// ABOUTME: Integration tests for SnykDirectory.GetSettingsFilePath() (IDE-1483).
// Verifies the settings file path is under the stable AppData location,
// not the extension install directory.
using System;
using System.IO;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Utils
{
    /// <summary>
    /// INT-002: Integration tests for <see cref="SnykDirectory.GetSettingsFilePath"/>.
    /// </summary>
    public class SnykDirectoryTests
    {
        /// <summary>
        /// INT-002: The settings file path must be under the stable AppData directory
        /// (<c>%LocalAppData%\Snyk</c>) and end with <c>settings.json</c>.
        /// It must NOT equal the install-directory path.
        /// </summary>
        [Fact]
        public void SettingsFilePath_IsUnderAppData_NotInstallDir()
        {
            var settingsPath = SnykDirectory.GetSettingsFilePath();
            var appDataPath = SnykDirectory.GetSnykAppDataDirectoryPath();

            // Must be rooted under the stable AppData directory
            Assert.StartsWith(appDataPath, settingsPath, System.StringComparison.OrdinalIgnoreCase);

            // Must end with settings.json
            Assert.EndsWith("settings.json", settingsPath, System.StringComparison.OrdinalIgnoreCase);

            // Must NOT be under the extension install directory
            var installDirPath = SnykExtension.GetExtensionDirectoryPath();
            var installDirSettings = Path.Combine(installDirPath, "settings.json");
            Assert.NotEqual(installDirSettings, settingsPath, System.StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// GetSettingsFilePath must not throw even when the target directory cannot be created
        /// (e.g. because a file with the same name already exists at that path — simulates a
        /// locked-down or UNC-redirected %LocalAppData% scenario).
        /// The method must return the expected path regardless.
        ///
        /// Before the fix, Directory.CreateDirectory was called without a try/catch, so this test
        /// is RED before the production fix and GREEN after.
        ///
        /// Note: logging of the warning cannot be asserted here — SnykDirectory uses
        /// LogManager.ForContext&lt;T&gt;() which writes to a file-backed Lazy&lt;Logger&gt; that is not
        /// interceptable via Serilog.Log.Logger (see brain note codebase/log-manager-not-mockable).
        /// </summary>
        [Fact]
        public void GetSettingsFilePath_DoesNotThrow_WhenDirectoryCreationFails()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                // Create a FILE at the path where a directory would normally be created.
                // Directory.CreateDirectory on this path throws IOException ("File exists") on Linux
                // and UnauthorizedAccessException on Windows — either way, an exception is thrown.
                var fakeAppDataDir = Path.Combine(tempDir, "FakeSnyk");
                File.WriteAllText(fakeAppDataDir, "not a directory");

                string result = null;
                var ex = Record.Exception(() => result = SnykDirectory.GetSettingsFilePath(fakeAppDataDir));

                Assert.Null(ex);
                Assert.NotNull(result);
                Assert.EndsWith("settings.json", result, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
