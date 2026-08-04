// ABOUTME: Tests for the one-shot Snyk Code enablement upgrade recovery in SnykOptionsManager.
// ABOUTME: Covers the upgrade/fresh-install/one-shot/reset/corrupt paths and the seed-then-migrate ordering.
//
// Snyk Code is the only product the Language Server does not default-enable: it needs an explicit
// changed:true to turn on, whereas OSS and IaC come up enabled from the LS's own defaults. So an
// upgrading user whose stored preference was "Code on" loses Code scanning entirely unless that
// preference reaches the LS as an explicit override. Two mechanisms produce that outcome and these
// tests pin both:
//   1. The plugin default for Code is false (matching the LS), so SeedFrom recognises a stored
//      true as a genuine override on any install that has never been seeded.
//   2. MigrateCodeEnablement covers installs whose override set was ALREADY seeded without
//      snyk_code_enabled — the state produced by builds that shipped the true default.
//
// NOTE: authored on macOS, where this net48 + VSSDK suite cannot be built or run. Verified on
// Windows/CI (see SnykOptionsManagerCorruptFileTests for the same caveat).
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Moq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.Utils;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Settings
{
    public class CodeEnablementMigrationTests : IDisposable
    {
        private readonly string tempDir;

        public CodeEnablementMigrationTests()
        {
            this.tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(this.tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(this.tempDir, recursive: true); } catch { }
        }

        // A path inside the temp dir that does NOT exist — the genuine fresh-install signal
        // (SnykSettingsLoader classifies both a missing file and an empty one as absent).
        private string NewSettingsPath() => Path.Combine(this.tempDir, "settings.json");

        private static SnykOptionsManager BuildManager(string path)
        {
            var spMock = new Mock<ISnykServiceProvider>();
            spMock.Setup(x => x.Options).Returns(BuildOptions());
            return new SnykOptionsManager(path, spMock.Object);
        }

        // A fully-populated options object so Save() cannot null-ref. Product values mirror the
        // plugin defaults; individual tests override what they care about.
        private static ISnykOptions BuildOptions()
        {
            var optMock = new Mock<ISnykOptions>();
            optMock.SetupAllProperties();
            optMock.Object.OssEnabled = true;
            optMock.Object.SnykCodeSecurityEnabled = SnykSettings.DefaultSnykCodeSecurityEnabled;
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
            optMock.Object.TrustedFolders = new HashSet<string>();
            optMock.Object.DeviceId = "test-device";
            optMock.Object.ApiToken = new AuthenticationToken(AuthenticationType.OAuth, string.Empty);
            return optMock.Object;
        }

        private static SnykSettings ReadSettings(string path) =>
            Json.Deserialize<SnykSettings>(File.ReadAllText(path, Encoding.UTF8));

        // A settings.json as written by a version that predates this migration: no
        // codeEnablementMigrated marker. `seeded` / `changedConfigKeys` model how far along the V25
        // override-tracking lifecycle the file is.
        private string WriteLegacySettings(bool codeEnabled, bool seeded = false,
            string changedConfigKeys = null, string pendingResets = null)
        {
            var path = NewSettingsPath();
            var json = "{" +
                "\"ossEnabled\": true," +
                "\"iacEnabled\": true," +
                "\"secretsEnabled\": false," +
                "\"snykCodeSecurityEnabled\": " + (codeEnabled ? "true" : "false") + "," +
                "\"autoScan\": true," +
                "\"openIssuesEnabled\": true," +
                "\"filterCritical\": true," +
                "\"filterHigh\": true," +
                "\"filterMedium\": true," +
                "\"filterLow\": true," +
                "\"binariesAutoUpdateEnabled\": true," +
                "\"trustedFolders\": []," +
                "\"deviceId\": \"upgrade-test-device\"," +
                "\"token\": \"\"" +
                (seeded ? ",\"changedConfigKeysSeeded\": true" : string.Empty) +
                (changedConfigKeys != null ? ",\"changedConfigKeys\": " + changedConfigKeys : string.Empty) +
                (pendingResets != null ? ",\"pendingResetConfigKeys\": " + pendingResets : string.Empty) +
                "}";
            File.WriteAllText(path, json, Encoding.UTF8);
            return path;
        }

        // ── Upgrade from a pre-V25 file (the reported regression) ─────────────────────────────

        // The headline case: released 2.9.0 stored "Code on" and no override set. After upgrading,
        // Code must reach the LS as an explicit override or the Code scanner is skipped entirely.
        // Carried by the default flip (stored true != default false → SeedFrom marks it).
        [Fact]
        public void Upgrade_CodeEnabled_IsRecoveredAsExplicitOverride()
        {
            var path = WriteLegacySettings(codeEnabled: true);

            var manager = BuildManager(path);
            var loaded = manager.Load();

            Assert.True(manager.OverrideTracker.IsChanged(PflagKeys.SnykCodeEnabled),
                "an upgrading user's enabled Snyk Code must be carried across as an explicit " +
                "override, otherwise it is sent changed:false and the LS leaves Code disabled");
            Assert.Contains(PflagKeys.SnykCodeEnabled, loaded.ChangedConfigKeys);
        }

        // The mirror case: Code was off. Nothing to rescue — the stored value equals the plugin
        // default, and the LS also defaults Code off, so changed:false resolves correctly.
        [Fact]
        public void Upgrade_CodeDisabled_IsNotMarked()
        {
            var path = WriteLegacySettings(codeEnabled: false);

            var manager = BuildManager(path);
            var loaded = manager.Load();

            Assert.False(manager.OverrideTracker.IsChanged(PflagKeys.SnykCodeEnabled),
                "a stored Code=false equals the plugin default and must not become an override");
            Assert.DoesNotContain(PflagKeys.SnykCodeEnabled,
                loaded.ChangedConfigKeys ?? new HashSet<string>());
        }

        // The case only the migration can fix: an install whose override set was already seeded by a
        // build that shipped the true default, so snyk_code_enabled was never recorded. SeedFrom does
        // not re-run for these files (Branch C hydrates verbatim), so without MigrateCodeEnablement
        // the enabled state stays lost forever.
        [Fact]
        public void Upgrade_AlreadySeededSetOmittingCode_IsRecoveredByTheMigration()
        {
            var path = WriteLegacySettings(codeEnabled: true, seeded: true,
                changedConfigKeys: "[\"snyk_oss_enabled\"]");

            var manager = BuildManager(path);
            var loaded = manager.Load();

            Assert.True(manager.OverrideTracker.IsChanged(PflagKeys.SnykCodeEnabled),
                "a seeded override set that predates the migration must still recover Code");
            // The pre-existing override must survive alongside it.
            Assert.Contains(PflagKeys.SnykOssEnabled, loaded.ChangedConfigKeys);
        }

        // ── Fresh installs must never be seeded ───────────────────────────────────────────────

        // A fresh install defers to org governance / LDX-Sync. The marker is stamped when the file is
        // created so the migration can never mistake a later launch for an upgrade.
        [Fact]
        public void FreshInstall_StampsMarkerAndLeavesCodeToTheServer()
        {
            var path = NewSettingsPath();
            Assert.False(File.Exists(path));

            var manager = BuildManager(path);
            var loaded = manager.Load();

            var onDisk = ReadSettings(path);
            Assert.True(onDisk.CodeEnablementMigrated,
                "a fresh install must record the migration as evaluated at file-creation time");
            Assert.False(onDisk.SnykCodeSecurityEnabled);
            Assert.False(manager.OverrideTracker.IsChanged(PflagKeys.SnykCodeEnabled),
                "a fresh install must not force Snyk Code on — org/LDX-Sync decides");
            Assert.DoesNotContain(PflagKeys.SnykCodeEnabled,
                loaded.ChangedConfigKeys ?? new HashSet<string>());
        }

        // The failure mode the file-creation stamp exists to prevent: if the Language Server later
        // enables Code and echoes it back (an LS-originated save, which does NOT record user
        // overrides), the next launch sees a stored Code=true on a file with no override set. Without
        // the marker that is indistinguishable from an upgrade, and the org's value would be frozen
        // into a permanent user override.
        [Fact]
        public void FreshInstall_ThenServerEnablesCode_DoesNotBecomeAUserOverride()
        {
            var path = NewSettingsPath();
            var manager = BuildManager(path);
            manager.Load();

            // The LS pushes Code=true via $/snyk.configuration; the plugin persists it without
            // recording a user override.
            var pushed = BuildOptions();
            pushed.SnykCodeSecurityEnabled = true;
            manager.Save(pushed, triggerSettingsChangedEvent: false, updateOverrideTracker: false);
            Assert.True(ReadSettings(path).SnykCodeSecurityEnabled);

            // Restart over the same file.
            var manager2 = BuildManager(path);
            var loaded = manager2.Load();

            Assert.False(manager2.OverrideTracker.IsChanged(PflagKeys.SnykCodeEnabled),
                "an org-pushed Code value must not be frozen into a user override on restart");
            Assert.DoesNotContain(PflagKeys.SnykCodeEnabled,
                loaded.ChangedConfigKeys ?? new HashSet<string>());
        }

        // ── One-shot semantics ───────────────────────────────────────────────────────────────

        // Once evaluated, the migration must never run again: the user may have deliberately reset
        // Code to the org default since, and re-marking would silently undo that.
        [Fact]
        public void Migration_IsOneShot_DoesNotResurrectAResetOverride()
        {
            var path = NewSettingsPath();
            // Post-migration steady state: marker set, Code stored on, but the user has since reset
            // the key so it is absent from the override set.
            var json = "{" +
                "\"snykCodeSecurityEnabled\": true," +
                "\"trustedFolders\": []," +
                "\"token\": \"\"," +
                "\"changedConfigKeysSeeded\": true," +
                "\"codeEnablementMigrated\": true" +
                "}";
            File.WriteAllText(path, json, Encoding.UTF8);

            var manager = BuildManager(path);
            var loaded = manager.Load();

            Assert.False(manager.OverrideTracker.IsChanged(PflagKeys.SnykCodeEnabled),
                "the migration must not re-run and resurrect a reset override");
            Assert.DoesNotContain(PflagKeys.SnykCodeEnabled,
                loaded.ChangedConfigKeys ?? new HashSet<string>());
        }

        // A reset queued but not yet confirmed-delivered to the LS is an explicit user action in
        // flight. Even on a file that predates the marker, the migration must not fight it.
        [Fact]
        public void Upgrade_WithQueuedResetForCode_DoesNotReMark()
        {
            var path = WriteLegacySettings(codeEnabled: true, seeded: true,
                changedConfigKeys: "[]", pendingResets: "[\"snyk_code_enabled\"]");

            var manager = BuildManager(path);
            manager.Load();

            Assert.False(manager.OverrideTracker.IsChanged(PflagKeys.SnykCodeEnabled),
                "a queued reset means the user explicitly returned Code to the org default; " +
                "the migration must not undo it");
            Assert.Contains(PflagKeys.SnykCodeEnabled, manager.OverrideTracker.PeekPendingResets());
        }

        // ── Corrupt file ─────────────────────────────────────────────────────────────────────

        // After a present-but-unreadable read the in-memory settings are blank defaults over a
        // recoverable file, so they say nothing about the user's real preference. The migration must
        // neither mark anything nor burn its one shot, and must not touch the file (IDE-1483).
        [Fact]
        public void CorruptFile_DoesNotMigrateAndLeavesTheFileIntact()
        {
            var path = NewSettingsPath();
            File.WriteAllText(path, "{ \"snykCodeSecurityEnabled\": true, ", Encoding.UTF8);
            var originalBytes = File.ReadAllBytes(path);

            var manager = BuildManager(path);
            var loaded = manager.Load();

            Assert.True(manager.SettingsFileWasUnreadableForTest,
                "the truncated file must be classified as present-but-unreadable, not absent");
            Assert.False(manager.OverrideTracker.IsChanged(PflagKeys.SnykCodeEnabled),
                "blank post-corrupt state must not produce a phantom Code override");
            Assert.DoesNotContain(PflagKeys.SnykCodeEnabled,
                loaded.ChangedConfigKeys ?? new HashSet<string>());
            Assert.Equal(originalBytes, File.ReadAllBytes(path));
        }

        // ── Persistence of the recovered mark ────────────────────────────────────────────────

        // Regression guard for a subtle loss path: Save(updateOverrideTracker:false) — the
        // LS-originated echo, often the only save in a session — serialises snykSettings verbatim
        // without re-reading the tracker. If the recovered mark lived only in the tracker it would be
        // dropped at the next restart, while codeEnablementMigrated (whole-object serialised) would
        // survive and stop the migration re-running. Code would silently switch off again.
        [Fact]
        public void RecoveredMark_SurvivesAnLsOriginatedSaveAndRestart()
        {
            var path = WriteLegacySettings(codeEnabled: true, seeded: true,
                changedConfigKeys: "[\"snyk_oss_enabled\"]");

            var manager = BuildManager(path);
            manager.Load();
            Assert.True(manager.OverrideTracker.IsChanged(PflagKeys.SnykCodeEnabled));

            // An LS-originated save: persists values, deliberately does not record user overrides.
            var pushed = BuildOptions();
            pushed.SnykCodeSecurityEnabled = true;
            manager.Save(pushed, triggerSettingsChangedEvent: false, updateOverrideTracker: false);

            var onDisk = ReadSettings(path);
            Assert.True(onDisk.CodeEnablementMigrated);
            Assert.NotNull(onDisk.ChangedConfigKeys);
            Assert.Contains(PflagKeys.SnykCodeEnabled, onDisk.ChangedConfigKeys);

            // Restart: the recovered override must still be there, even though the migration is done.
            var manager2 = BuildManager(path);
            var loaded = manager2.Load();
            Assert.True(manager2.OverrideTracker.IsChanged(PflagKeys.SnykCodeEnabled),
                "the recovered override must survive a restart after an LS-originated save");
            Assert.Contains(PflagKeys.SnykCodeEnabled, loaded.ChangedConfigKeys);
        }

        // ── Ordering guard ───────────────────────────────────────────────────────────────────

        // The migration MUST run after the seed branches. SeedFrom performs a full Clear() of the
        // changed set, so a mark applied first is silently discarded. This proves the ordering is
        // load-bearing rather than incidental — the same guard PR #783 added for the VS Code fix
        // (which runs its migration BEFORE its seed, because there the migration writes a setting
        // value the seed then reads).
        [Fact]
        public void SeedThenMark_IsLoadBearing_MarkThenSeedLosesTheOverride()
        {
            var atDefaults = BuildOptionsAtDefaultsForSeed();

            // Production order: seed, then carry the pre-existing enabled state across.
            var correct = new UserOverrideTracker();
            correct.SeedFrom(atDefaults);
            correct.Mark(PflagKeys.SnykCodeEnabled);
            Assert.True(correct.IsChanged(PflagKeys.SnykCodeEnabled),
                "marking after the seed must survive");

            // Inverted order: the seed wipes the mark and the recovery is lost.
            var inverted = new UserOverrideTracker();
            inverted.Mark(PflagKeys.SnykCodeEnabled);
            inverted.SeedFrom(atDefaults);
            Assert.False(inverted.IsChanged(PflagKeys.SnykCodeEnabled),
                "SeedFrom clears the changed set, so a mark applied before it is discarded — " +
                "this is why MigrateCodeEnablement runs after the seed branches in Load()");
        }

        // Every global key at its plugin default, so SeedFrom marks nothing by itself and the
        // ordering test isolates the effect of the explicit Mark.
        private static ISnykOptions BuildOptionsAtDefaultsForSeed()
        {
            var o = BuildOptions();
            o.CustomEndpoint = string.Empty;
            o.Organization = string.Empty;
            o.RiskScoreThreshold = null;
            o.AuthenticationMethod = default(AuthenticationType);
            return o;
        }
    }
}
