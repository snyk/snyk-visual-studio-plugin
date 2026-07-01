// ABOUTME: Unit tests for IDE-1483 FIX-D1 — SnykOptionsManager must NOT overwrite a
// corrupt/partially-written settings.json with blank defaults (token-loss bug).
// Three scenarios: corrupt file, absent file, valid file.
// Real temp files, GUID paths, IDisposable cleanup — matching the existing test conventions
// in SettingsLocationMigratorTests and SettingsPersistenceAcceptanceTests.
//
// NOTE: These tests must be verified on Windows/CI — no .NET toolchain on the Linux
// build host used to author them.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Moq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.Utils;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Settings
{
    /// <summary>
    /// UNIT tests for IDE-1483 FIX-D1: SnykOptionsManager.LoadSettingsFromFile must distinguish
    /// "file absent" from "file exists but unreadable/corrupt":
    ///   - Absent file  → write defaults to disk (safe: fresh install, no data loss).
    ///   - Corrupt file → keep defaults in memory only; do NOT overwrite the on-disk file
    ///                    (the file might be mid-write from a concurrent migration; token must survive).
    ///   - Valid file   → load correctly; do NOT rewrite the file.
    /// </summary>
    public class SnykOptionsManagerCorruptFileTests : IDisposable
    {
        private readonly string tempDir;

        public SnykOptionsManagerCorruptFileTests()
        {
            this.tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(this.tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(this.tempDir, recursive: true); } catch { }
        }

        // ----------------------------------------------------------------
        // D1-UNIT-001: corrupt/unparseable existing settings.json is NOT
        //              overwritten with defaults on SnykOptionsManager construction
        // ----------------------------------------------------------------

        /// <summary>
        /// D1-UNIT-001: When settings.json exists but contains invalid JSON (simulating
        /// a partially-written file mid-migration), constructing SnykOptionsManager must
        /// NOT overwrite the file.  The corrupt content must be preserved on disk so the
        /// user's auth token can be recovered once the concurrent write completes.
        ///
        /// This test is RED before FIX-D1 and GREEN after.
        /// </summary>
        [Fact]
        public void CorruptSettingsFile_IsNotOverwrittenOnConstruction()
        {
            // Arrange — write a deliberately corrupt (truncated) JSON file,
            // simulating what Window B reads while Window A is mid-File.Copy.
            var settingsPath = Path.Combine(this.tempDir, Guid.NewGuid().ToString("N") + ".json");
            const string corruptContent = "{\"Token\":\"my-precious-token\","; // truncated — not valid JSON; Token uses correct SnykSettings field casing
            File.WriteAllText(settingsPath, corruptContent, Encoding.UTF8);

            // Act — construct the manager (which calls LoadSettingsFromFile internally) AND then
            // call Load(). Load() is invoked right after construction in SnykVSPackage init, so the
            // real construct→Load sequence must be exercised: Load()'s seed lifecycle (IDE-2152)
            // must NOT overwrite a corrupt-but-recoverable file either.
            var manager = BuildManager(settingsPath);
            manager.Load();

            // Assert — the on-disk file must still contain the corrupt content unchanged.
            // If LoadSettingsFromFile or Load() overwrote it with defaults the token is permanently lost.
            var onDisk = File.ReadAllText(settingsPath, Encoding.UTF8);
            Assert.Equal(corruptContent, onDisk);
        }

        // ----------------------------------------------------------------
        // D1-UNIT-002: absent settings.json → in-memory defaults written to disk,
        //              construction does not throw
        // ----------------------------------------------------------------

        /// <summary>
        /// D1-UNIT-002: When settings.json does not exist (fresh install), construction
        /// must not throw.  Defaults are held in memory (and written to disk, which is
        /// safe — there is no pre-existing data to lose).
        ///
        /// This test must remain GREEN before and after FIX-D1 — it is a regression guard.
        /// </summary>
        [Fact]
        public void AbsentSettingsFile_ConstructionDoesNotThrow_DefaultsLoadable()
        {
            // Arrange — path that definitely does not exist
            var settingsPath = Path.Combine(this.tempDir, Guid.NewGuid().ToString("N") + ".json");
            Assert.False(File.Exists(settingsPath));

            // Act — construction must not throw
            SnykOptionsManager manager = null;
            var ex = Record.Exception(() => manager = BuildManager(settingsPath));

            Assert.Null(ex);
            Assert.NotNull(manager);

            // Load() must return an ISnykOptions with defaults (not throw)
            ISnykOptions opts = null;
            var exLoad = Record.Exception(() => opts = manager.Load());
            Assert.Null(exLoad);
            Assert.NotNull(opts);
        }

        // ----------------------------------------------------------------
        // D1-UNIT-003: valid settings.json → loaded correctly, file unchanged
        // ----------------------------------------------------------------

        /// <summary>
        /// D1-UNIT-003: When settings.json contains valid JSON, construction loads the
        /// settings correctly and does NOT rewrite the file (no silent data mutation on
        /// a good-state load).
        ///
        /// This test must remain GREEN before and after FIX-D1.
        /// </summary>
        [Fact]
        public void ValidSettingsFile_LoadedCorrectly_FileUnchanged()
        {
            // Arrange — write a well-formed settings file with a known token
            var settingsPath = Path.Combine(this.tempDir, Guid.NewGuid().ToString("N") + ".json");
            var seed = new SnykSettings { Token = "valid-token-xyz", Organization = "my-org" };
            var originalContent = Json.Serialize(seed);
            File.WriteAllText(settingsPath, originalContent, Encoding.UTF8);
            var originalBytes = File.ReadAllBytes(settingsPath);

            // Act — construct and load
            var manager = BuildManager(settingsPath);
            var opts = manager.Load();

            // Assert — token and org round-trip correctly
            Assert.Equal("valid-token-xyz", opts.ApiToken.ToString());
            Assert.Equal("my-org", opts.Organization);

            // The file must not have been rewritten — byte-for-byte equality is reliable
            // (no timing dependency, no Thread.Sleep, not susceptible to filesystem-clock
            // granularity differences on FAT32/APFS/Docker volumes).
            var afterBytes = File.ReadAllBytes(settingsPath);
            Assert.Equal(originalBytes, afterBytes);
        }

        // ----------------------------------------------------------------
        // MERGE-REGRESSION-001 (IDE-1483 × IDE-2152): the token-loss repro.
        //   Construct the manager over a PRESENT-BUT-CORRUPT settings.json that carries a
        //   recoverable token, then call Load() (the exact construct→Load sequence used at
        //   SnykVSPackage init). The IDE-2152 seed lifecycle in Load() must NOT overwrite the
        //   corrupt-but-recoverable file: the on-disk bytes must be byte-for-byte unchanged so
        //   the token survives once the concurrent write that produced the corruption completes.
        //   RED before the fix (Load()'s Branch A seed-write clobbers the file with blank defaults).
        // ----------------------------------------------------------------

        [Fact]
        public void CorruptSettingsFile_LoadDoesNotOverwrite_TokenPreservedOnDisk()
        {
            // Arrange — a corrupt (truncated) settings.json holding a recoverable token.
            var settingsPath = Path.Combine(this.tempDir, Guid.NewGuid().ToString("N") + ".json");
            const string corruptContent = "{\"Token\":\"recoverable-token-123\",\"Organization\":\"acme";
            File.WriteAllText(settingsPath, corruptContent, Encoding.UTF8);
            var originalBytes = File.ReadAllBytes(settingsPath);

            // Act — the real init sequence: construct (LoadSettingsFromFile) then Load().
            var manager = BuildManager(settingsPath);
            var opts = manager.Load();

            // Assert — Load() must not throw and must return defaults in memory...
            Assert.NotNull(opts);
            // ...and, critically, the corrupt file must be UNTOUCHED on disk so the token is recoverable.
            var afterBytes = File.ReadAllBytes(settingsPath);
            Assert.Equal(originalBytes, afterBytes);
            var afterText = File.ReadAllText(settingsPath, Encoding.UTF8);
            Assert.Equal(corruptContent, afterText);
            Assert.Contains("recoverable-token-123", afterText);
        }

        // ----------------------------------------------------------------
        // MERGE-RECONCILE-001 (IDE-1483 D1-UNIT-003 × IDE-2152 seeding): a VALID but unmarked
        //   (upgrading) settings.json must be left byte-for-byte unchanged by Load() — the seed
        //   marker write is DEFERRED to the next real Save — AND the override tracker must still
        //   be seeded in memory, AND a subsequent real Save must persist the seeded marker.
        //   RED before the fix (Load()'s Branch A/B seed-write rewrites the valid file).
        // ----------------------------------------------------------------

        [Fact]
        public void ValidUnmarkedFile_LoadLeavesBytesUnchanged_SeedsInMemory_SaveThenPersistsMarker()
        {
            // Arrange — a valid file with a non-default value (ossEnabled=false) and NO seeded marker,
            // i.e. an upgrading user's settings.json. Seeding must recognise the override in memory.
            var settingsPath = Path.Combine(this.tempDir, Guid.NewGuid().ToString("N") + ".json");
            var seed = new SnykSettings { OssEnabled = false, Organization = "my-org" };
            var originalContent = Json.Serialize(seed);
            File.WriteAllText(settingsPath, originalContent, Encoding.UTF8);
            var originalBytes = File.ReadAllBytes(settingsPath);

            // Act — construct + Load().
            var manager = BuildManager(settingsPath);
            var loaded = manager.Load();

            // Assert 1 — the valid file is byte-for-byte unchanged: Load() defers the marker write.
            var afterLoadBytes = File.ReadAllBytes(settingsPath);
            Assert.Equal(originalBytes, afterLoadBytes);

            // Assert 2 — the tracker was seeded in memory: the non-default override is recognised.
            Assert.Contains(PflagKeys.SnykOssEnabled, loaded.ChangedConfigKeys);
            Assert.True(manager.OverrideTracker.IsChanged(PflagKeys.SnykOssEnabled));

            // Assert 3 — a subsequent REAL Save persists the seeded marker + override set to disk,
            // so the deferral does not lose the seed. A fresh manager over the same file must then
            // take the steady-state branch and preserve the override verbatim.
            manager.Save(BuildOptions(), triggerSettingsChangedEvent: false);
            var afterSave = Json.Deserialize<SnykSettings>(File.ReadAllText(settingsPath, Encoding.UTF8));
            Assert.True(afterSave.ChangedConfigKeysSeeded);
            Assert.NotNull(afterSave.ChangedConfigKeys);
            Assert.Contains(PflagKeys.SnykOssEnabled, afterSave.ChangedConfigKeys);
        }

        // ----------------------------------------------------------------
        // MERGE-SAVE-GUARD-001 (IDE-1483 × IDE-2152): the FIRST post-corrupt Save must not
        //   permanently destroy a recoverable token. After a corrupt read, snykSettings is a
        //   blank-defaults object (Token=""). The Load-time fix keeps Load() from writing, but the
        //   first REAL Save (here the LS-push Save(updateOverrideTracker:false), also SaveOrganizationAsync
        //   / CommitPendingResets / MigrateLegacySolutionSettings) serializes the blank object and
        //   File.WriteAllText clobbers the recoverable bytes → Token="" on disk = permanent loss.
        //   The choke-point guard in SaveSettingsToFile must back the recoverable file up to a
        //   settings.json.corrupt-<UTC>.bak sidecar BEFORE the first overwrite, so the token bytes
        //   survive even though settings.json is (legitimately) rewritten with this session's state.
        //   RED before the fix (no backup is created; the recoverable token bytes are gone forever).
        // ----------------------------------------------------------------

        [Fact]
        public void CorruptSettingsFile_FirstSave_BacksUpRecoverableTokenToSidecar()
        {
            // Arrange — a corrupt settings.json holding a recoverable token.
            var settingsPath = Path.Combine(this.tempDir, Guid.NewGuid().ToString("N") + ".json");
            const string corruptContent = "{\"Token\":\"recoverable-token-999\",\"Organization\":\"acme";
            File.WriteAllText(settingsPath, corruptContent, Encoding.UTF8);

            // Act — the real init sequence (construct → Load) leaves the corrupt file intact, then the
            // first LS-push Save writes this session's (blank-token) state to settings.json.
            var manager = BuildManager(settingsPath);
            manager.Load();
            manager.Save(BuildBlankOptions(), triggerSettingsChangedEvent: false, updateOverrideTracker: false);

            // Assert — the recoverable token bytes MUST survive in a sidecar backup, even though
            // settings.json itself has been rewritten. Without the guard there is no backup and the
            // original truncated bytes (with the token) are permanently gone.
            var backups = Directory.GetFiles(this.tempDir, Path.GetFileName(settingsPath) + ".corrupt-*.bak");
            Assert.Single(backups);
            var backupText = File.ReadAllText(backups[0], Encoding.UTF8);
            Assert.Equal(corruptContent, backupText);
            Assert.Contains("recoverable-token-999", backupText);
        }

        // ----------------------------------------------------------------
        // MERGE-SAVE-GUARD-002: a NORMAL (non-corrupt) load path must Save settings.json exactly as
        //   before — no spurious .bak sidecar when settingsFileWasUnreadable is false.
        //   Guards against the guard firing on the common (valid-file) path.
        // ----------------------------------------------------------------

        [Fact]
        public void ValidSettingsFile_Save_WritesNormally_NoBackupCreated()
        {
            // Arrange — a valid file; the load path sets settingsFileWasUnreadable = false.
            var settingsPath = Path.Combine(this.tempDir, Guid.NewGuid().ToString("N") + ".json");
            var seed = new SnykSettings { Token = "valid-token-abc", Organization = "my-org" };
            File.WriteAllText(settingsPath, Json.Serialize(seed), Encoding.UTF8);

            // Act — construct, load, then a normal Save.
            var manager = BuildManager(settingsPath);
            manager.Load();
            manager.Save(BuildOptions(), triggerSettingsChangedEvent: false);

            // Assert — settings.json still holds a valid, deserialisable object and NO backup exists.
            var afterText = File.ReadAllText(settingsPath, Encoding.UTF8);
            var afterSettings = Json.Deserialize<SnykSettings>(afterText);
            Assert.NotNull(afterSettings);
            var backups = Directory.GetFiles(this.tempDir, Path.GetFileName(settingsPath) + ".corrupt-*.bak");
            Assert.Empty(backups);
        }

        // ----------------------------------------------------------------
        // MERGE-SAVE-GUARD-003: after the FIRST post-corrupt Save, settingsFileWasUnreadable is
        //   cleared, so a SECOND Save writes normally and creates NO additional backup (exactly one
        //   sidecar per corrupt-load session).
        //   RED before the fix (the flag is never cleared / no backup logic exists).
        // ----------------------------------------------------------------

        [Fact]
        public void CorruptSettingsFile_SecondSave_WritesNormally_NoRepeatedBackup()
        {
            // Arrange — corrupt file with a recoverable token.
            var settingsPath = Path.Combine(this.tempDir, Guid.NewGuid().ToString("N") + ".json");
            const string corruptContent = "{\"Token\":\"recoverable-token-777\",\"Organization\":\"acme";
            File.WriteAllText(settingsPath, corruptContent, Encoding.UTF8);

            var manager = BuildManager(settingsPath);
            manager.Load();

            // Act — two Saves in the same session.
            manager.Save(BuildBlankOptions(), triggerSettingsChangedEvent: false, updateOverrideTracker: false);
            manager.Save(BuildBlankOptions(), triggerSettingsChangedEvent: false, updateOverrideTracker: false);

            // Assert — exactly ONE backup (from the first Save); the second Save wrote normally with
            // the flag cleared and produced no further sidecar.
            var backups = Directory.GetFiles(this.tempDir, Path.GetFileName(settingsPath) + ".corrupt-*.bak");
            Assert.Single(backups);
        }

        // ----------------------------------------------------------------
        // MERGE-SAVE-GUARD-004 (Should-Fix): a FAILED backup must BLOCK the destructive overwrite.
        //   When settingsFileWasUnreadable == true and File.Copy of the recoverable file throws
        //   (e.g. sidecar ACL denial, transient sharing violation, MAX_PATH on the .bak name), the
        //   current code swallows the copy error, clears the flag, and proceeds to overwrite the
        //   recoverable settings.json with this session's blank state → permanent token loss with NO
        //   backup. The guard must instead: NOT clear settingsFileWasUnreadable, NOT overwrite, and
        //   return early so the recoverable file survives and the NEXT Save retries the backup.
        //   RED before the fix (the recoverable file is clobbered with blank defaults).
        // ----------------------------------------------------------------

        [Fact]
        public void CorruptSettingsFile_FirstSave_BackupFails_DoesNotOverwriteRecoverableFile()
        {
            // Arrange — a corrupt settings.json holding a recoverable token.
            var settingsPath = Path.Combine(this.tempDir, Guid.NewGuid().ToString("N") + ".json");
            const string corruptContent = "{\"Token\":\"recoverable-token-555\",\"Organization\":\"acme";
            File.WriteAllText(settingsPath, corruptContent, Encoding.UTF8);
            var originalBytes = File.ReadAllBytes(settingsPath);

            // A manager whose copy seam always throws, simulating a backup that cannot be created.
            var manager = new FailingCopyOptionsManager(settingsPath, BuildServiceProvider());
            manager.Load();

            // Act — the first LS-push Save. Backup will throw; the guard must block the overwrite.
            manager.Save(BuildBlankOptions(), triggerSettingsChangedEvent: false, updateOverrideTracker: false);

            // Assert 1 — the recoverable corrupt file is STILL intact on disk (not overwritten blank).
            var afterBytes = File.ReadAllBytes(settingsPath);
            Assert.Equal(originalBytes, afterBytes);
            Assert.Contains("recoverable-token-555", File.ReadAllText(settingsPath, Encoding.UTF8));

            // Assert 2 — no backup was created (the copy failed).
            var backups = Directory.GetFiles(this.tempDir, Path.GetFileName(settingsPath) + ".corrupt-*.bak");
            Assert.Empty(backups);

            // Assert 3 — the unreadable flag survives so a LATER successful Save retries the backup.
            Assert.True(manager.SettingsFileWasUnreadableForTest);
        }

        // ----------------------------------------------------------------
        // MERGE-SAVE-GUARD-005: a failed backup blocks the overwrite, but once the copy CAN succeed
        //   the very next Save backs up the recoverable token and then writes normally. Proves the
        //   flag was preserved (not cleared) across the failed attempt.
        //   RED before the fix (the first Save would have cleared the flag).
        // ----------------------------------------------------------------

        [Fact]
        public void CorruptSettingsFile_BackupFailsThenSucceeds_NextSaveBacksUpAndWrites()
        {
            var settingsPath = Path.Combine(this.tempDir, Guid.NewGuid().ToString("N") + ".json");
            const string corruptContent = "{\"Token\":\"recoverable-token-556\",\"Organization\":\"acme";
            File.WriteAllText(settingsPath, corruptContent, Encoding.UTF8);

            var manager = new FailingCopyOptionsManager(settingsPath, BuildServiceProvider());
            manager.Load();

            // First Save — copy throws → overwrite blocked, flag preserved.
            manager.Save(BuildBlankOptions(), triggerSettingsChangedEvent: false, updateOverrideTracker: false);
            Assert.True(manager.SettingsFileWasUnreadableForTest);

            // Flip the seam so the copy now succeeds, then Save again.
            manager.FailCopy = false;
            manager.Save(BuildBlankOptions(), triggerSettingsChangedEvent: false, updateOverrideTracker: false);

            // Now exactly one backup carrying the recoverable token, and settings.json was written.
            var backups = Directory.GetFiles(this.tempDir, Path.GetFileName(settingsPath) + ".corrupt-*.bak");
            Assert.Single(backups);
            Assert.Contains("recoverable-token-556", File.ReadAllText(backups[0], Encoding.UTF8));
            var afterSettings = Json.Deserialize<SnykSettings>(File.ReadAllText(settingsPath, Encoding.UTF8));
            Assert.NotNull(afterSettings);
            Assert.False(manager.SettingsFileWasUnreadableForTest);
        }

        // ----------------------------------------------------------------
        // MERGE-EMPTY-HEAL-001 (Fix 2): an empty/whitespace settings.json is NOT corrupt — it must be
        //   treated as ABSENT and self-heal by writing defaults, with NO useless .corrupt .bak.
        //   RED before the fix (empty file → deserialize null → misclassified present-but-unreadable →
        //   never repaired; and a later Save would emit an empty .bak).
        // ----------------------------------------------------------------

        [Fact]
        public void EmptySettingsFile_TreatedAsAbsent_SelfHeals_NoBackup()
        {
            var settingsPath = Path.Combine(this.tempDir, Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(settingsPath, "   \r\n\t ", Encoding.UTF8); // whitespace-only

            // Act — construct (LoadSettingsFromFile writes defaults for an absent file) + Load().
            var manager = BuildManager(settingsPath);
            manager.Load();

            // Assert 1 — defaults were written to disk (self-heal): the file is now valid JSON.
            var afterSettings = Json.Deserialize<SnykSettings>(File.ReadAllText(settingsPath, Encoding.UTF8));
            Assert.NotNull(afterSettings);

            // Assert 2 — no .corrupt backup sidecar was produced (empty file is not "unreadable").
            var backups = Directory.GetFiles(this.tempDir, Path.GetFileName(settingsPath) + ".corrupt-*.bak");
            Assert.Empty(backups);
        }

        // ----------------------------------------------------------------
        // helpers
        // ----------------------------------------------------------------

        // A SnykOptionsManager whose file-copy seam can be forced to throw, so the "backup failed"
        // branch is exercised deterministically on any platform (CI runs on Windows). Production
        // behaviour is identical when FailCopy == false (delegates to the base File.Copy seam).
        private sealed class FailingCopyOptionsManager : SnykOptionsManager
        {
            public bool FailCopy = true;

            public FailingCopyOptionsManager(string settingsFilePath, ISnykServiceProvider serviceProvider)
                : base(settingsFilePath, serviceProvider)
            {
            }

            protected override void CopyFile(string source, string destination)
            {
                if (FailCopy)
                    throw new UnauthorizedAccessException("simulated backup copy failure");
                base.CopyFile(source, destination);
            }
        }

        private static ISnykServiceProvider BuildServiceProvider()
        {
            var optionsMock = new Mock<ISnykOptions>();
            optionsMock.SetupAllProperties();
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            serviceProviderMock.Setup(x => x.Options).Returns(optionsMock.Object);
            return serviceProviderMock.Object;
        }

        // A blank-defaults options object mirroring the in-memory state after a corrupt read
        // (empty token). Used to drive the first post-corrupt Save that would otherwise clobber
        // the recoverable file.
        private static ISnykOptions BuildBlankOptions()
        {
            var optMock = new Mock<ISnykOptions>();
            optMock.SetupAllProperties();
            optMock.Object.AdditionalParameters = new List<string>();
            optMock.Object.TrustedFolders = new HashSet<string>();
            optMock.Object.ApiToken = new AuthenticationToken(AuthenticationType.OAuth, string.Empty);
            return optMock.Object;
        }

        // A fully-defaulted options object so Save() does not null-ref, and so a real Save with no
        // editedKeys preserves the already-seeded override set (Snapshot of the seeded tracker).
        private static ISnykOptions BuildOptions()
        {
            var optMock = new Mock<ISnykOptions>();
            optMock.SetupAllProperties();
            optMock.Object.OssEnabled = false; // matches the seeded non-default value on disk
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
            optMock.Object.AdditionalEnv = string.Empty;
            optMock.Object.AdditionalParameters = new List<string>();
            optMock.Object.TrustedFolders = new HashSet<string>();
            optMock.Object.DeviceId = "test-device";
            optMock.Object.Organization = "my-org";
            optMock.Object.ApiToken = new AuthenticationToken(AuthenticationType.OAuth, string.Empty);
            return optMock.Object;
        }

        private static SnykOptionsManager BuildManager(string settingsFilePath)
        {
            var optionsMock = new Mock<ISnykOptions>();
            optionsMock.SetupAllProperties();
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            serviceProviderMock.Setup(x => x.Options).Returns(optionsMock.Object);
            return new SnykOptionsManager(settingsFilePath, serviceProviderMock.Object);
        }
    }
}
