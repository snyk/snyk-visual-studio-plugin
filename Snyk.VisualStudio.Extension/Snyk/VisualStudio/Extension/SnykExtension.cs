namespace Snyk.VisualStudio.Extension
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Settings;

    /// <summary>
    /// Common Snyk extension message.
    /// </summary>
    public class SnykExtension
    {
        /// <summary>
        /// Integration name.
        /// </summary>
        public const string IntegrationName = "VISUAL_STUDIO_PREVIEW";

        private const string AppSettingsFileName = "appsettings.json";

        private static string version;

        private static string extensionDirectoryPath;

        private static SnykAppSettings appSettings;

        /// <summary>
        /// Get <see cref="SnykAppSettings"/> from file.
        /// </summary>
        /// <returns><see cref="SnykAppSettings"/> object.</returns>
        public static SnykAppSettings GetAppSettings()
        {
            if (appSettings == null)
            {
                string extensionPath = SnykExtension.GetExtensionDirectoryPath();

                string appsettingsPath = Path.Combine(extensionPath, AppSettingsFileName);

                appSettings = Json
                    .Deserialize<SnykAppSettings>(File.ReadAllText(appsettingsPath, Encoding.UTF8));
            }

            return appSettings;
        }

        /// <summary>
        /// Get integration version.
        /// </summary>
        /// <returns>String.</returns>
        public static string GetIntegrationVersion()
        {
            if (version == null)
            {
                try
                {
                    string extensionPath = GetExtensionDirectoryPath();

                    string manifestPath = Path.Combine(extensionPath, "extension.vsixmanifest");

                    var xmlDocument = new XmlDocument();
                    xmlDocument.Load(manifestPath);

                    if (xmlDocument.DocumentElement == null || xmlDocument.DocumentElement.Name != "PackageManifest")
                    {
                        return "UNKNOWN";
                    }

                    var metaData = xmlDocument.DocumentElement.ChildNodes.Cast<XmlElement>().First(x => x.Name == "Metadata");
                    var identity = metaData.ChildNodes.Cast<XmlElement>().First(x => x.Name == "Identity");

                    version = identity.GetAttribute("Version");
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
            }

            return version;
        }

        /// <summary>
        /// Get extension directory path.
        /// </summary>
        /// <returns>Extension directory path.</returns>
        public static string GetExtensionDirectoryPath()
        {
            if (string.IsNullOrEmpty(extensionDirectoryPath))
            {
                string codebase = typeof(SnykVSPackage).Assembly.CodeBase;

                var uri = new Uri(codebase, UriKind.Absolute);

                extensionDirectoryPath = Directory.GetParent(uri.LocalPath).FullName;
            }

            return extensionDirectoryPath;
        }
    }
}