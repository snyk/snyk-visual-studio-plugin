// ABOUTME: Acceptance and integration tests for settings persistence (IDE-1483).
// These tests prove that the real SnykOptionsManager + SnykSettingsLoader persist data
// to the stable AppData location and that auth/config survive a simulated restart.
// They use real temp files — no mocked I/O — and constitute the acceptance layer.
using System;
using System.IO;
using Moq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Settings
{
    /// <summary>
    /// Acceptance/integration tests for IDE-1483: settings must survive a simulated restart
    /// (two SnykOptionsManager instances, same stable file path, real file I/O).
    /// </summary>
    public class SettingsPersistenceAcceptanceTests
    {
        // ----------------------------------------------------------------
        // ACC-001: auth token, org, and endpoint survive a simulated restart
        // ----------------------------------------------------------------

        /// <summary>
        /// ACC-001: A user authenticates and sets org/endpoint; after a simulated
        /// restart (new manager over the same stable path) those values are still present.
        /// </summary>
        [Fact]
        public void SettingsSurviveRestart()
        {
            var settingsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
            try
            {
                // --- First "session": save settings ---
                var manager1 = BuildManager(settingsPath);
                var options1 = manager1.Load();
                options1.ApiToken = new AuthenticationToken(
                    AuthenticationType.Token, "test-token-abc");
                options1.Organization = "my-org";
                options1.CustomEndpoint = "https://api.snykgov.io";
                manager1.Save(options1, triggerSettingsChangedEvent: false);

                // --- Second "session": new manager instance, same path ---
                var manager2 = BuildManager(settingsPath);
                var loaded = manager2.Load();

                Assert.Equal("test-token-abc", loaded.ApiToken.ToString());
                Assert.Equal("my-org", loaded.Organization);
                Assert.Equal("https://api.snykgov.io", loaded.CustomEndpoint);
            }
            finally
            {
                if (File.Exists(settingsPath)) File.Delete(settingsPath);
            }
        }

        // ----------------------------------------------------------------
        // ACC-002: recorded CLI version survives restart → downloader won't re-download
        // ----------------------------------------------------------------

        /// <summary>
        /// ACC-002: Recorded CurrentCliVersion survives a simulated restart so the
        /// downloader can see it and avoids re-downloading a current CLI.
        /// </summary>
        [Fact]
        public void RecordedCliVersionSurvivesRestart()
        {
            var settingsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var manager1 = BuildManager(settingsPath);
                var options1 = manager1.Load();
                options1.CurrentCliVersion = "v1.2.3";
                manager1.Save(options1, triggerSettingsChangedEvent: false);

                var manager2 = BuildManager(settingsPath);
                var loaded = manager2.Load();

                Assert.Equal("v1.2.3", loaded.CurrentCliVersion);
            }
            finally
            {
                if (File.Exists(settingsPath)) File.Delete(settingsPath);
            }
        }

        // ----------------------------------------------------------------
        // ACC-003: upgrading user keeps auth after migration from old install-dir location
        // ----------------------------------------------------------------

        /// <summary>
        /// ACC-003: A user upgrading from the old build has a settings file at the
        /// old install-dir location. After the extension starts with the new code the
        /// settings are migrated to the stable path and the user is not logged out.
        /// </summary>
        [Fact]
        public void UpgradingUserKeepsAuthAfterMigration()
        {
            // Use a unique per-run directory so a leftover file from a crashed run
            // cannot make this test non-deterministic.
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var oldPath = Path.Combine(tempDir, "old_settings.json");
            var newPath = Path.Combine(tempDir, "new_settings.json");
            try
            {
                // Create old settings with auth token
                var oldManager = BuildManager(oldPath);
                var opts = oldManager.Load();
                opts.ApiToken = new AuthenticationToken(
                    AuthenticationType.Token, "upgrade-token");
                opts.Organization = "upgrade-org";
                oldManager.Save(opts, triggerSettingsChangedEvent: false);

                // Run migration: old -> new
                SettingsLocationMigrator.MigrateIfNeeded(oldPath, newPath);

                // New manager reads from new location
                var newManager = BuildManager(newPath);
                var loaded = newManager.Load();

                Assert.Equal("upgrade-token", loaded.ApiToken.ToString());
                Assert.Equal("upgrade-org", loaded.Organization);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        // ----------------------------------------------------------------
        // INT-001: SaveThenLoad round-trip via real manager + loader
        // ----------------------------------------------------------------

        /// <summary>
        /// INT-001: Save then load through the real manager and loader at the new
        /// stable path. Loaded values equal what was saved.
        /// </summary>
        [Fact]
        public void SaveThenLoad_RoundTrips()
        {
            var settingsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var manager = BuildManager(settingsPath);
                var options = manager.Load();
                options.ApiToken = new AuthenticationToken(
                    AuthenticationType.Token, "roundtrip-token");
                options.Organization = "roundtrip-org";
                options.CustomEndpoint = "https://api.eu.snyk.io";
                manager.Save(options, triggerSettingsChangedEvent: false);

                // Reload via a second manager over the same file
                var reloaded = BuildManager(settingsPath).Load();

                Assert.Equal("roundtrip-token", reloaded.ApiToken.ToString());
                Assert.Equal("roundtrip-org", reloaded.Organization);
                Assert.Equal("https://api.eu.snyk.io", reloaded.CustomEndpoint);
            }
            finally
            {
                if (File.Exists(settingsPath)) File.Delete(settingsPath);
            }
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
