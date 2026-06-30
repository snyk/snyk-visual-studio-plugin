// ABOUTME: Integration tests for user-override persistence covering INT-001, INT-002, ACC-003,
// ABOUTME: ACC-006 from the IDE-2152 test plan.
// Uses real SnykOptionsManager + real SnykSettingsLoader on a temp file + real UserOverrideTracker.
using System.Collections.Generic;
using System.IO;
using Moq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Settings
{
    public class UserOverridePersistenceTests
    {
        // Build a real SnykOptionsManager backed by a temp file. Returns the path so callers
        // can re-construct a second manager (simulating an IDE restart).
        private static (SnykOptionsManager manager, Mock<ISnykOptions> options, string path)
            BuildManager(string existingPath = null)
        {
            var path = existingPath ?? Path.GetTempFileName();
            var optMock = new Mock<ISnykOptions>();
            optMock.SetupAllProperties();

            // Seed reasonable defaults so Save() doesn't null-ref on Token or TrustedFolders.
            optMock.Object.OssEnabled = true;
            optMock.Object.SnykCodeSecurityEnabled = true;
            optMock.Object.IacEnabled = true;
            optMock.Object.SecretsEnabled = false;
            optMock.Object.AutoScan = true;
            optMock.Object.EnableDeltaFindings = false;
            optMock.Object.FilterCritical = true;
            optMock.Object.FilterHigh = true;
            optMock.Object.FilterMedium = true;
            optMock.Object.FilterLow = true;
            optMock.Object.OpenIssuesEnabled = true;
            optMock.Object.IgnoredIssuesEnabled = false;
            optMock.Object.IgnoreUnknownCA = false;
            optMock.Object.BinariesAutoUpdate = true;
            optMock.Object.CliCustomPath = string.Empty;
            optMock.Object.CliReleaseChannel = SnykCliDownloader.DefaultReleaseChannel;
            optMock.Object.CliBaseDownloadURL = SnykCliDownloader.DefaultBaseDownloadUrl;
            optMock.Object.AdditionalEnv = string.Empty;
            optMock.Object.AdditionalParameters = new List<string>();
            optMock.Object.TrustedFolders = new System.Collections.Generic.HashSet<string>();
            optMock.Object.DeviceId = "test-device";
            // Fresh-install default: OAuth is the zero value of AuthenticationType (pinned by R3-4 test).
            // Using Token here would mask auth-seeding regressions because SeedFrom would see a
            // non-default auth method and spuriously mark AuthenticationMethod as changed.
            optMock.Object.ApiToken = new AuthenticationToken(
                AuthenticationType.OAuth, string.Empty);

            var spMock = new Mock<ISnykServiceProvider>();
            spMock.Setup(x => x.Options).Returns(optMock.Object);

            var manager = new SnykOptionsManager(path, spMock.Object);
            return (manager, optMock, path);
        }

        // INT-001 / ACC-003: Save then Load round-trips the override set through settings.json.
        // After saving options with OssEnabled=false (non-default) — passing it as an editedKey so
        // ApplyUserEdits records it — then loading in a fresh manager, the tracker must report
        // OssEnabled's key as changed and it must round-trip through the persisted-keys branch of Load.
        [Fact]
        public void Manager_SaveThenLoad_PersistsChangedConfigKeys()
        {
            var (manager, optMock, path) = BuildManager();
            try
            {
                manager.Load(); // seed tracker first
                // Mark OssEnabled as user-overridden: set to non-default and declare as edited key.
                optMock.Object.OssEnabled = false;
                manager.Save(optMock.Object, triggerSettingsChangedEvent: false,
                    editedKeys: new List<string> { PflagKeys.SnykOssEnabled });

                // Simulate IDE restart: construct a fresh manager from the same file.
                var (manager2, _, _) = BuildManager(path);
                var loaded = manager2.Load();

                // The persisted ChangedConfigKeys should include snyk_oss_enabled.
                Assert.NotNull(loaded.ChangedConfigKeys);
                Assert.Contains(PflagKeys.SnykOssEnabled, loaded.ChangedConfigKeys);
            }
            finally
            {
                File.Delete(path);
            }
        }

        // INT-002: Save applies the edit-delta to the tracker.
        // Passing OssEnabled in editedKeys with a non-default value marks it;
        // passing it again with the default value unmarks it and enqueues a reset.
        [Fact]
        public void Manager_Save_SyncsTrackerFromDeltas()
        {
            var (manager, optMock, path) = BuildManager();
            try
            {
                manager.Load(); // seed tracker first

                // First save: OssEnabled = false (non-default), declared as edited key → marked.
                optMock.Object.OssEnabled = false;
                manager.Save(optMock.Object, triggerSettingsChangedEvent: false,
                    editedKeys: new List<string> { PflagKeys.SnykOssEnabled });

                var afterFirstSave = manager.Load();
                Assert.Contains(PflagKeys.SnykOssEnabled, afterFirstSave.ChangedConfigKeys);

                // Second save: OssEnabled back to true (default), declared as edited key → unmarked.
                optMock.Object.OssEnabled = true;
                manager.Save(optMock.Object, triggerSettingsChangedEvent: false,
                    editedKeys: new List<string> { PflagKeys.SnykOssEnabled });

                var afterSecondSave = manager.Load();
                Assert.DoesNotContain(PflagKeys.SnykOssEnabled, afterSecondSave.ChangedConfigKeys);
            }
            finally
            {
                File.Delete(path);
            }
        }

        // INT-004 (finding 4): Calling Load() twice on the same manager must not union marks.
        // The second Load must reflect only what is on disk, not a union of both loads.
        [Fact]
        public void Load_CalledTwice_DoesNotUnionStaleMarks()
        {
            // Write a settings.json with OssEnabled=false (non-default) -> snyk_oss_enabled marked.
            var path = Path.GetTempFileName();
            try
            {
                var rawJson = @"{
  ""ossEnabled"": false,
  ""snykCodeSecurityEnabled"": true,
  ""iacEnabled"": true,
  ""binariesAutoUpdateEnabled"": true,
  ""trustedFolders"": [],
  ""openIssuesEnabled"": true,
  ""filterCritical"": true,
  ""filterHigh"": true,
  ""filterMedium"": true,
  ""filterLow"": true,
  ""autoScan"": true,
  ""deviceId"": ""load-twice-device"",
  ""token"": """"
}";
                File.WriteAllText(path, rawJson);

                var (manager, optMock, _) = BuildManager(path);

                // First load: OssEnabled=false -> marked.
                var first = manager.Load();
                Assert.Contains(PflagKeys.SnykOssEnabled, first.ChangedConfigKeys);

                // Now update the file so OssEnabled=true (default) — simulates an external reset.
                var rawJson2 = rawJson.Replace(@"""ossEnabled"": false", @"""ossEnabled"": true");
                File.WriteAllText(path, rawJson2);
                // Reload the settings from disk (mimic what the manager would do on a second Load).
                manager.LoadSettingsFromFile();

                // Second load: should NOT carry over the stale snyk_oss_enabled mark.
                var second = manager.Load();
                Assert.DoesNotContain(PflagKeys.SnykOssEnabled, second.ChangedConfigKeys);
            }
            finally
            {
                File.Delete(path);
            }
        }

        // R7-1a: Save with updateOverrideTracker:false does NOT mark LS-pushed non-default values.
        // Simulates OnSnykConfiguration receiving e.g. organization="acme" from the LS and calling
        // Save(options, triggerSettingsChangedEvent:false, updateOverrideTracker:false).
        // The tracker must stay empty — the LS push must never create phantom user overrides.
        [Fact]
        public void Save_UpdateOverrideTrackerFalse_DoesNotMarkNonDefaultValues()
        {
            var (manager, optMock, path) = BuildManager();
            try
            {
                // Load first so the tracker is seeded (reflects a fresh install with no overrides).
                manager.Load();
                Assert.Empty(manager.OverrideTracker.Snapshot()); // precondition: no user overrides

                // Simulate LS pushing a non-default value (e.g. org set by LDX).
                optMock.Object.Organization = "acme-from-ls";

                // LS-originated save: updateOverrideTracker:false.
                manager.Save(optMock.Object, triggerSettingsChangedEvent: false, updateOverrideTracker: false);

                // Tracker must stay clean — no phantom user override.
                Assert.DoesNotContain(PflagKeys.Organization, manager.OverrideTracker.Snapshot()); // LS-pushed must not be marked
                Assert.False(manager.OverrideTracker.IsChanged(PflagKeys.Organization),
                    "IsChanged must be false for a key set only by the LS, not the user");

                // Reload and confirm the persisted ChangedConfigKeys did NOT grow.
                var loaded = manager.Load();
                if (loaded.ChangedConfigKeys != null)
                    Assert.DoesNotContain(PflagKeys.Organization, loaded.ChangedConfigKeys); // must not persist LS-pushed key
            }
            finally
            {
                File.Delete(path);
            }
        }

        // R7-1b: Save with updateOverrideTracker:true and a non-empty editedKeys DOES mark the key.
        // Proves the flag gates the behavior — both branches tested (contrast with R7-1a above).
        [Fact]
        public void Save_UpdateOverrideTrackerTrue_DoesMarkNonDefaultValues()
        {
            var (manager, optMock, path) = BuildManager();
            try
            {
                manager.Load();
                Assert.Empty(manager.OverrideTracker.Snapshot()); // precondition: no user overrides

                // User explicitly sets a non-default value and the call site declares it as edited.
                optMock.Object.OssEnabled = false;

                // User-initiated save: updateOverrideTracker:true, editedKeys declares what changed.
                manager.Save(optMock.Object, triggerSettingsChangedEvent: false, updateOverrideTracker: true,
                    editedKeys: new List<string> { PflagKeys.SnykOssEnabled });

                // Tracker must record the override.
                Assert.Contains(PflagKeys.SnykOssEnabled, manager.OverrideTracker.Snapshot());
                Assert.True(manager.OverrideTracker.IsChanged(PflagKeys.SnykOssEnabled),
                    "IsChanged must be true for a key the user explicitly changed");
            }
            finally
            {
                File.Delete(path);
            }
        }

        // RINT-001 (Integration): Manager.Save with editedKeys marks only the keys in the set.
        // A non-default value NOT in editedKeys (org-pushed) must never be marked.
        [Fact]
        public void Manager_Save_WithEditedKeys_MarksOnlyEditedDeviations()
        {
            var (manager, optMock, path) = BuildManager();
            try
            {
                manager.Load(); // seed tracker

                // Simulate: org pushed OssEnabled=false (non-default) AND user edited IacEnabled=false.
                optMock.Object.OssEnabled = false;   // org-pushed, NOT in editedKeys
                optMock.Object.IacEnabled = false;   // user-edited, in editedKeys

                // Save with only IacEnabled in the edited set.
                manager.Save(
                    optMock.Object,
                    triggerSettingsChangedEvent: false,
                    updateOverrideTracker: true,
                    editedKeys: new List<string> { PflagKeys.SnykIacEnabled });

                // IacEnabled was edited and its value (false) deviates from default (true) → marked.
                Assert.Contains(PflagKeys.SnykIacEnabled, manager.OverrideTracker.Snapshot());

                // OssEnabled was NOT in editedKeys (org-pushed) → must NOT be marked.
                Assert.DoesNotContain(PflagKeys.SnykOssEnabled, manager.OverrideTracker.Snapshot());
            }
            finally
            {
                File.Delete(path);
            }
        }

        // F4: Focused edit-delta path test. Starting from a seeded tracker with NO marks,
        // Save with a non-empty editedKeys causes ApplyUserEdits to mark the key, and that
        // mark persists through Load via the persisted-keys branch (not SeedFrom).
        [Fact]
        public void Save_WithEditedKey_MarksKeyAndPersistsViaPersistedKeysBranch()
        {
            var (manager, optMock, path) = BuildManager();
            try
            {
                // Load to seed tracker from all-default options — snapshot must be empty.
                manager.Load();
                Assert.Empty(manager.OverrideTracker.Snapshot());

                // User changes IacEnabled to false (non-default) and the call site declares it edited.
                optMock.Object.IacEnabled = false;
                manager.Save(optMock.Object, triggerSettingsChangedEvent: false,
                    editedKeys: new List<string> { PflagKeys.SnykIacEnabled });

                // Immediately: tracker has the key marked.
                Assert.Contains(PflagKeys.SnykIacEnabled, manager.OverrideTracker.Snapshot());

                // After restart (fresh manager from same file): the key must come back via
                // the persisted-keys branch of Load (ChangedConfigKeys is non-null on disk).
                var (manager2, _, _) = BuildManager(path);
                var loaded = manager2.Load();

                Assert.NotNull(loaded.ChangedConfigKeys);
                Assert.Contains(PflagKeys.SnykIacEnabled, loaded.ChangedConfigKeys);
                // OssEnabled was never edited — must not appear.
                Assert.DoesNotContain(PflagKeys.SnykOssEnabled, loaded.ChangedConfigKeys);
            }
            finally
            {
                File.Delete(path);
            }
        }

        // ACC-006: SeedOnFirstLoad — pre-existing non-default values are recognized as overrides
        // on upgrade (when the persisted ChangedConfigKeys set is absent / empty).
        [Fact]
        public void SeedOnFirstLoad_NonDefaultPersistedValues_AreRecognizedAsOverrides()
        {
            // Write a settings.json WITHOUT ChangedConfigKeys (simulates a pre-upgrade file).
            var path = Path.GetTempFileName();
            try
            {
                var rawJson = @"{
  ""ossEnabled"": false,
  ""snykCodeSecurityEnabled"": true,
  ""iacEnabled"": true,
  ""binariesAutoUpdateEnabled"": true,
  ""trustedFolders"": [],
  ""openIssuesEnabled"": true,
  ""filterCritical"": true,
  ""filterHigh"": true,
  ""filterMedium"": true,
  ""filterLow"": true,
  ""autoScan"": true,
  ""deviceId"": ""upgrade-test-device"",
  ""token"": """"
}";
                File.WriteAllText(path, rawJson);

                // Load: since ChangedConfigKeys is absent, manager should seed from values.
                var (manager, _, _) = BuildManager(path);
                var loaded = manager.Load();

                // OssEnabled = false is non-default, so it must be in ChangedConfigKeys.
                Assert.Contains(PflagKeys.SnykOssEnabled, loaded.ChangedConfigKeys);
                // SnykCodeEnabled = true is the default, so it must NOT be in ChangedConfigKeys.
                Assert.DoesNotContain(PflagKeys.SnykCodeEnabled, loaded.ChangedConfigKeys);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
