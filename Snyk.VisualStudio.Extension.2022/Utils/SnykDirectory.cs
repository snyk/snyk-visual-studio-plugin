using System;
using System.IO;

namespace Snyk.VisualStudio.Extension
{
    public class SnykDirectory
    {
        /// <summary>
        /// Directory name for store Snyk CLI.
        /// </summary>
        public const string SnykConfigurationDirectoryName = "Snyk";

        /// <summary>
        /// Settings file name.
        /// </summary>
        private const string SettingsFileName = "settings.json";

        /// <summary>
        /// Get Snyk AppData directory path. By default it's $UserDirectory\.AppData\Snyk.
        /// </summary>
        /// <returns>AppData directory path.</returns>
        public static string GetSnykAppDataDirectoryPath()
        {
            string appDataDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return Path.Combine(appDataDirectoryPath, SnykConfigurationDirectoryName);
        }

        /// <summary>
        /// Get the stable per-user settings file path under <c>%LocalAppData%\Snyk\settings.json</c>.
        /// Best-effort creates the parent directory before returning; if creation fails (e.g.
        /// locked-down or UNC-redirected %LocalAppData%) a Warning is logged and the computed
        /// path is still returned — SnykSettingsLoader.Save already swallows write failures,
        /// so a missing dir degrades gracefully instead of aborting startup.
        /// </summary>
        /// <returns>Full path to the settings file in the stable AppData location.</returns>
        public static string GetSettingsFilePath()
        {
            return GetSettingsFilePath(GetSnykAppDataDirectoryPath());
        }

        /// <summary>
        /// Overload that accepts an explicit <paramref name="appDataDir"/> for testability.
        /// See <see cref="GetSettingsFilePath()"/> for full semantics.
        /// </summary>
        /// <param name="appDataDir">Base directory in which <c>settings.json</c> resides.</param>
        /// <returns>Full path: <paramref name="appDataDir"/>\settings.json.</returns>
        internal static string GetSettingsFilePath(string appDataDir)
        {
            try
            {
                Directory.CreateDirectory(appDataDir);
            }
            catch (Exception ex)
            {
                LogManager.ForContext(typeof(SnykDirectory)).Warning(
                    ex,
                    "Could not create settings directory '{DirectoryPath}' — settings writes may fail.",
                    appDataDir);
            }

            return Path.Combine(appDataDir, SettingsFileName);
        }
    }
}
