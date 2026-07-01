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

        // INT-002 (corrected semantics, PR #515): Save applies the edit-delta to the tracker.
        // Any key present in editedKeys is an explicit user choice → marked, whether its value is
        // non-default OR equal to the plugin default. Reset-to-default is no longer inferred from
        // value==default (that inference caused "enabling Snyk Code doesn't persist").
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

                // Second save: OssEnabled at true (its default), declared as edited key → still an
                // explicit user choice → stays marked (no inferred reset from value==default).
                optMock.Object.OssEnabled = true;
                manager.Save(optMock.Object, triggerSettingsChangedEvent: false,
                    editedKeys: new List<string> { PflagKeys.SnykOssEnabled });

                var afterSecondSave = manager.Load();
                Assert.Contains(PflagKeys.SnykOssEnabled, afterSecondSave.ChangedConfigKeys);
            }
            finally
            {
                File.Delete(path);
            }
        }

        // INT-004: Calling Load() twice on the same manager must not union/accumulate marks.
        // The second Load must yield exactly the same set as the first — not a superset.
        //
        // Under the seeded-marker design: the first Load (marker absent, OssEnabled=false non-default)
        // seeds and writes the marker + {snyk_oss_enabled} to disk. The second Load (marker now present
        // in snykSettings in-memory) takes Branch C: clears changed marks via ClearChanged(), then
        // hydrates from the persisted set ({snyk_oss_enabled}) verbatim. The result must be identical
        // to the first load — exactly {snyk_oss_enabled}, not duplicated or grown.
        //
        // Note: SnykSettingsLoader.Load() caches the SnykSettings object after the first file read,
        // so the manager's in-memory snykSettings is the source of truth between loads on the same
        // manager. A simulated IDE restart requires a FRESH manager (see SACC-001/002).
        [Fact]
        public void Load_CalledTwice_DoesNotUnionStaleMarks()
        {
            // Write a settings.json with OssEnabled=false (non-default) — no seeded marker yet.
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

                var (manager, _, _) = BuildManager(path);

                // First load: marker absent + OssEnabled=false non-default → Branch A seeds →
                // {snyk_oss_enabled} marked, marker written to disk.
                var first = manager.Load();
                Assert.Contains(PflagKeys.SnykOssEnabled, first.ChangedConfigKeys);
                var firstCount = first.ChangedConfigKeys.Count;

                // Second load on the SAME manager: marker now present in the in-memory snykSettings
                // (Branch C) → ClearChanged() then hydrate verbatim from the persisted set.
                // Result must equal the first load — no stale-mark unioning, no growth.
                var second = manager.Load();
                Assert.Contains(PflagKeys.SnykOssEnabled, second.ChangedConfigKeys);
                Assert.Equal(firstCount, second.ChangedConfigKeys.Count);
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
                Assert.NotNull(loaded.ChangedConfigKeys);
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

                // Load: since ChangedConfigKeys is absent (and marker is absent), manager should seed from values.
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

        // ─────────────────────────────────────────────────────────────────────
        // REFINEMENT-S tests — LOAD-TIME SEEDING LIFECYCLE (CP-S.1)
        // ─────────────────────────────────────────────────────────────────────

        // SACC-001: Customer-outcome acceptance test.
        // Org pushes a non-default value via LS (updateOverrideTracker:false).
        // After the LS save and a simulated IDE restart, the loaded ChangedConfigKeys
        // must NOT contain the org-pushed key — it is still an org value, not a user override.
        // Reproduces the org-value-freezing-across-restart bug:
        //   Load() re-seeds (value-vs-default) → marks the org-pushed org value → frozen.
        // Requires the persisted seeded-marker so the second Load trusts the persisted set (incl. empty).
        [Fact]
        public void OrgPushedValueSurvivesRestart_StillReachesUserWhenOrgChangesItAgain()
        {
            var (manager, optMock, path) = BuildManager();
            try
            {
                // Step 1: Initial load on a fresh-install (all-default) file.
                // Seeding finds no deviations → empty override set → marker written.
                manager.Load();
                Assert.Empty(manager.OverrideTracker.Snapshot()); // precondition: no overrides

                // Step 2: LS pushes a non-default org value (e.g. org set by central config).
                optMock.Object.Organization = "acme-from-org";
                // LS-originated save — updateOverrideTracker:false.
                // Must NOT touch ChangedConfigKeys or the seeded marker.
                manager.Save(optMock.Object, triggerSettingsChangedEvent: false, updateOverrideTracker: false);

                // Step 3: Simulate IDE restart — construct a FRESH manager from the same file.
                var (manager2, _, _) = BuildManager(path);
                var loaded = manager2.Load();

                // The seeded marker must be present → persisted-keys branch used → empty set returned.
                // The org-pushed organization key must NOT appear in ChangedConfigKeys.
                Assert.NotNull(loaded.ChangedConfigKeys);
                Assert.DoesNotContain(PflagKeys.Organization, loaded.ChangedConfigKeys);

                Assert.False(manager2.OverrideTracker.IsChanged(PflagKeys.Organization),
                    "IsChanged must be false: organization was set by the org/LS, not by the user");
            }
            finally
            {
                File.Delete(path);
            }
        }

        // SACC-002 (corrected semantics, PR #515): A key the user edits through the form is an
        // explicit override, even when the value they land on equals the plugin default. That
        // explicit override must survive a restart (persisted verbatim, not dropped). Reset-to-default
        // is no longer inferred from value==default inside the Save/ApplyUserEdits path.
        [Fact]
        public void EditedKeyAtDefaultValue_StaysMarkedAsOverrideAcrossRestart()
        {
            var (manager, optMock, path) = BuildManager();
            try
            {
                // Step 1: User marks IacEnabled as an override (non-default: false).
                manager.Load();
                optMock.Object.IacEnabled = false;
                manager.Save(optMock.Object, triggerSettingsChangedEvent: false,
                    editedKeys: new List<string> { PflagKeys.SnykIacEnabled });

                Assert.Contains(PflagKeys.SnykIacEnabled, manager.OverrideTracker.Snapshot());

                // Step 2: User edits IacEnabled through the form again, landing on the default (true).
                // Under the corrected semantics this is still an explicit user choice → stays marked.
                optMock.Object.IacEnabled = true;
                manager.Save(optMock.Object, triggerSettingsChangedEvent: false,
                    editedKeys: new List<string> { PflagKeys.SnykIacEnabled });

                Assert.Contains(PflagKeys.SnykIacEnabled, manager.OverrideTracker.Snapshot());

                // Step 3: Simulate IDE restart — FRESH manager from the same file. The explicit
                // override must survive verbatim (persisted-keys branch), not be dropped.
                var (manager2, _, _) = BuildManager(path);
                var loaded = manager2.Load();

                Assert.NotNull(loaded.ChangedConfigKeys);
                Assert.Contains(PflagKeys.SnykIacEnabled, loaded.ChangedConfigKeys);

                Assert.True(manager2.OverrideTracker.IsChanged(PflagKeys.SnykIacEnabled),
                    "An explicitly edited key must remain an override after restart");
            }
            finally
            {
                File.Delete(path);
            }
        }

        // SINT-001: Integration test.
        // A settings file with the seeded-marker present and ChangedConfigKeys absent/empty must
        // hydrate to an EMPTY override set — Load must NOT call SeedFrom.
        // Verified by observable state: a current-non-default value in options is NOT marked after load.
        [Fact]
        public void Load_SeededEmptySet_DoesNotReSeedFromValues()
        {
            // Write a settings.json with the seeded marker present but ChangedConfigKeys absent.
            // This models the steady state after a fresh install: user has zero overrides, marker is set.
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
  ""deviceId"": ""sint001-device"",
  ""token"": """",
  ""changedConfigKeysSeeded"": true
}";
                File.WriteAllText(path, rawJson);

                var (manager, _, _) = BuildManager(path);
                var loaded = manager.Load();

                // The marker is present → Branch C taken → empty set hydrated verbatim.
                // OssEnabled=false is non-default, but it MUST NOT be marked (no re-seed).
                Assert.Empty(loaded.ChangedConfigKeys);

                Assert.False(manager.OverrideTracker.IsChanged(PflagKeys.SnykOssEnabled),
                    "With seeded-marker present and empty ChangedConfigKeys, no re-seed must occur — " +
                    "a non-default value in the file must NOT be marked as a user override");
            }
            finally
            {
                File.Delete(path);
            }
        }

        // SINT-002: Integration test — seeding is idempotent across a restart on an all-default file:
        // a fresh manager over the same file yields the SAME (empty) override set and does not spuriously
        // mark a still-default value.
        //
        // Scenario: first manager loads an all-default file (no marker) → Branch A seeds an empty set
        // (in memory; the marker write is DEFERRED to the next real Save — IDE-1483 × IDE-2152 merge
        // fix — so a valid file is left byte-unchanged by Load()). No real Save runs here, so the file
        // still has no marker. A FRESH manager (simulating restart) re-seeds via Branch A; because the
        // on-disk value is still the default, the re-seed is idempotent (empty set, nothing marked).
        //
        // Note: SnykSettingsLoader caches the SnykSettings object, so rewriting the file and calling
        // LoadSettingsFromFile() on the SAME manager does not re-read from disk. A fresh manager is
        // required to genuinely simulate a restart.
        [Fact]
        public void Load_AfterSeed_DoesNotReSeedOnNextLoad()
        {
            var (manager, _, path) = BuildManager();
            try
            {
                // First load on an all-default file (no marker): Branch A seeds an empty set in memory
                // (marker write deferred to the next real Save).
                var first = manager.Load();
                Assert.Empty(first.ChangedConfigKeys);

                // Simulate IDE restart with a FRESH manager over the same file. No real Save ran, so the
                // marker is still absent → Branch A re-seeds. The on-disk value is the default, so the
                // re-seed is idempotent: still empty, nothing marked.
                var (manager2, _, _) = BuildManager(path);
                var second = manager2.Load();

                // OssEnabled is at its default on disk — confirmed not marked in first load.
                Assert.Empty(second.ChangedConfigKeys);

                Assert.False(manager2.OverrideTracker.IsChanged(PflagKeys.SnykOssEnabled),
                    "Re-seeding a still-default file is idempotent — a default value must not be marked");
            }
            finally
            {
                File.Delete(path);
            }
        }

        // PENDING-RESET-PERSIST-001 (fix #2): A reset applied but not yet confirmed-delivered persists
        // to settings.json and is rehydrated into the tracker's pending-reset queue by a fresh manager
        // over the same file (simulated restart). Uses Save(resetKeys:...) — the same channel the bridge
        // uses — so the tracker un-marks + enqueues, and Save persists PendingResetConfigKeys.
        [Fact]
        public void PendingReset_PersistsAndRehydratesAcrossRestart()
        {
            var (manager, optMock, path) = BuildManager();
            try
            {
                manager.Load(); // seed tracker

                // First: user overrides OssEnabled (so there is a mark to un-mark on reset).
                optMock.Object.OssEnabled = false;
                manager.Save(optMock.Object, triggerSettingsChangedEvent: false,
                    editedKeys: new List<string> { PflagKeys.SnykOssEnabled });
                Assert.Contains(PflagKeys.SnykOssEnabled, manager.OverrideTracker.Snapshot());

                // Then: user resets it. Not committed (LS not ready) → stays pending + persisted.
                manager.Save(optMock.Object, triggerSettingsChangedEvent: false,
                    resetKeys: new List<string> { PflagKeys.SnykOssEnabled });
                Assert.Contains(PflagKeys.SnykOssEnabled, manager.OverrideTracker.PeekPendingResets());
                // Un-marked → no longer in the persisted override set.
                Assert.DoesNotContain(PflagKeys.SnykOssEnabled, manager.OverrideTracker.Snapshot());

                // Restart: fresh manager over the same file must rehydrate the pending reset.
                var (manager2, _, _) = BuildManager(path);
                manager2.Load();

                Assert.Contains(PflagKeys.SnykOssEnabled, manager2.OverrideTracker.PeekPendingResets());
            }
            finally
            {
                File.Delete(path);
            }
        }

        // INIT-DEFERRED-COMMIT-001 (fix #3 invariant): An init-only delivery folds the pending reset
        // into the handshake wire map (proving it is SENT at init) but must NOT commit it — after init
        // the reset stays QUEUED in the tracker AND PERSISTED in settings.json (so a crash before the
        // first config-update does not lose it). A subsequent SUCCESSFUL config-update (peek→send→
        // commit, simulated via CommitPendingResets of the peeked snapshot) then drains it from both
        // the queue and persistence. This pins the deferred-commit invariant documented on
        // SnykLanguageClient.GetInitializationOptions.
        [Fact]
        public void InitOnlyDelivery_LeavesResetQueuedAndPersisted_SubsequentConfigUpdateCommits()
        {
            var (manager, optMock, path) = BuildManager();
            try
            {
                manager.Load(); // seed tracker

                // User overrides then resets OssEnabled → reset queued + persisted (LS not ready yet).
                optMock.Object.OssEnabled = false;
                manager.Save(optMock.Object, triggerSettingsChangedEvent: false,
                    editedKeys: new List<string> { PflagKeys.SnykOssEnabled });
                manager.Save(optMock.Object, triggerSettingsChangedEvent: false,
                    resetKeys: new List<string> { PflagKeys.SnykOssEnabled });
                Assert.Contains(PflagKeys.SnykOssEnabled, manager.OverrideTracker.PeekPendingResets());

                // Wire a real LsSettingsV25 to the same real options + real manager (so it reads the
                // manager's real tracker). This is the exact init path SnykLanguageClient uses.
                var spMock = new Mock<ISnykServiceProvider>();
                spMock.Setup(x => x.Options).Returns(optMock.Object);
                spMock.Setup(x => x.SnykOptionsManager).Returns(manager);
                optMock.Object.FolderConfigs = new List<FolderConfig>();
                var lsSettings = new LsSettingsV25(spMock.Object);

                // Init-only delivery: build the handshake options. This folds the reset into the wire
                // map (SENT) but must NOT commit it.
                var init = lsSettings.GetInitializationOptions();

                // The reset was folded into the init settings map as {value:null, changed:true}.
                Assert.True(init.Settings.ContainsKey(PflagKeys.SnykOssEnabled));
                Assert.Null(init.Settings[PflagKeys.SnykOssEnabled].Value);
                Assert.True(init.Settings[PflagKeys.SnykOssEnabled].Changed);

                // Invariant part 1: after init the reset is STILL queued in the tracker (not committed).
                Assert.Contains(PflagKeys.SnykOssEnabled, manager.OverrideTracker.PeekPendingResets());

                // Invariant part 2: it is STILL persisted — a fresh manager over the same file (a crash
                // right after a successful init) rehydrates it, so nothing is lost.
                var (managerAfterCrash, _, _) = BuildManager(path);
                managerAfterCrash.Load();
                Assert.Contains(PflagKeys.SnykOssEnabled,
                    managerAfterCrash.OverrideTracker.PeekPendingResets());

                // Subsequent successful config-update: peek→send→commit. Commit the exact peeked set
                // through the manager (what DidChangeConfigurationAsync does on RPC success).
                var peeked = manager.OverrideTracker.PeekPendingResets();
                manager.CommitPendingResets(peeked);

                // Now drained from the queue...
                Assert.DoesNotContain(PflagKeys.SnykOssEnabled, manager.OverrideTracker.PeekPendingResets());
                // ...and from persistence: a fresh manager over the same file does NOT rehydrate it.
                var (managerAfterCommit, _, _) = BuildManager(path);
                managerAfterCommit.Load();
                Assert.DoesNotContain(PflagKeys.SnykOssEnabled,
                    managerAfterCommit.OverrideTracker.PeekPendingResets());
            }
            finally
            {
                File.Delete(path);
            }
        }

        // PENDING-RESET-PERSIST-002 (fix #2): A committed (confirmed-delivered) reset is removed from
        // persistence too, so a fresh manager over the same file does NOT rehydrate it.
        [Fact]
        public void CommittedPendingReset_RemovedFromPersistence_NotRehydrated()
        {
            var (manager, optMock, path) = BuildManager();
            try
            {
                manager.Load();

                manager.Save(optMock.Object, triggerSettingsChangedEvent: false,
                    resetKeys: new List<string> { PflagKeys.Organization });
                Assert.Contains(PflagKeys.Organization, manager.OverrideTracker.PeekPendingResets());

                // Confirmed-delivered → commit through the manager (updates persistence too).
                manager.CommitPendingResets(new List<string> { PflagKeys.Organization });
                Assert.DoesNotContain(PflagKeys.Organization, manager.OverrideTracker.PeekPendingResets());

                // Restart: the committed reset must NOT come back.
                var (manager2, _, _) = BuildManager(path);
                manager2.Load();

                Assert.DoesNotContain(PflagKeys.Organization, manager2.OverrideTracker.PeekPendingResets());
            }
            finally
            {
                File.Delete(path);
            }
        }

        // R2 migration-guard test: a settings.json written by a prior version (no seeded marker)
        // that already has a non-empty ChangedConfigKeys set must NOT be discarded by re-seeding.
        // Branch B in Load(): marker absent + keys non-empty → hydrate verbatim + persist marker.
        [Fact]
        public void Load_PriorVersionPersistedKeys_HydratesVerbatimWithoutReseeding()
        {
            // Write a settings.json as a prior version would have: ChangedConfigKeys present,
            // no changedConfigKeysSeeded marker. OssEnabled=true (default), but snyk_oss_enabled
            // is in ChangedConfigKeys — simulating a user override that was written before the
            // marker was introduced. A re-seed would DISCARD this because OssEnabled==default.
            var path = Path.GetTempFileName();
            try
            {
                var rawJson = @"{
  ""ossEnabled"": true,
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
  ""deviceId"": ""migration-guard-device"",
  ""token"": """",
  ""changedConfigKeys"": [""snyk_oss_enabled""]
}";
                File.WriteAllText(path, rawJson);

                var (manager, _, _) = BuildManager(path);
                var loaded = manager.Load();

                // Branch B must hydrate snyk_oss_enabled from the persisted set verbatim.
                // Re-seeding would discard it (ossEnabled=true == default → not marked).
                Assert.NotNull(loaded.ChangedConfigKeys);
                Assert.Contains(PflagKeys.SnykOssEnabled, loaded.ChangedConfigKeys);
                Assert.True(manager.OverrideTracker.IsChanged(PflagKeys.SnykOssEnabled),
                    "A key persisted by a prior version must survive the migration load — " +
                    "Branch B must hydrate verbatim, not re-seed and discard the stored override");
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
