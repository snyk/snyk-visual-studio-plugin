// ABOUTME: Unit tests for IDE-1483 FIX-D1 — SnykOptionsManager must NOT overwrite a
// corrupt/partially-written settings.json with blank defaults (token-loss bug).
// Three scenarios: corrupt file, absent file, valid file.
// Real temp files, GUID paths, IDisposable cleanup — matching the existing test conventions
// in SettingsLocationMigratorTests and SettingsPersistenceAcceptanceTests.
//
// NOTE: These tests must be verified on Windows/CI — no .NET toolchain on the Linux
// build host used to author them.
using System;
using System.IO;
using System.Text;
using Moq;
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

            // Act — construct the manager (which calls LoadSettingsFromFile internally)
            var manager = BuildManager(settingsPath);

            // Assert — the on-disk file must still contain the corrupt content unchanged.
            // If LoadSettingsFromFile overwrote it with defaults the token is permanently lost.
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
        // helpers
        // ----------------------------------------------------------------

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
