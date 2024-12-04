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
        /// Get Snyk AppData directory path. By default it's $UserDirectory\.AppData\Snyk.
        /// </summary>
        /// <returns>AppData directory path.</returns>
        public static string GetSnykAppDataDirectoryPath()
        {
            string appDataDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return Path.Combine(appDataDirectoryPath, SnykConfigurationDirectoryName);
        }
    }
}
