namespace Snyk.Common
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Common Snyk extension message.
    /// </summary>
    public class SnykExtension
    {
        /// <summary>
        /// Integration name.
        /// </summary>
        public const string IntegrationName = "VISUAL_STUDIO";
        private const string AppSettingsDevelopmentFileName = "appsettings.development.json";
        private const string AppSettingsFileName = "appsettings.json";

        private static string extensionDirectoryPath;
        private static SnykAppSettings appSettings;
        private static readonly Lazy<string> versionLazy = new(GetIntegrationVersion);

        /// <summary>
        /// Gets extension version.
        /// </summary>
        public static string Version => versionLazy.Value;

        /// <summary>
        /// Gets <see cref="SnykAppSettings"/> from the appsettings.json file.
        /// </summary>
        public static SnykAppSettings AppSettings
        {
            get
            {
                if (appSettings == null)
                {
                    string extensionPath = GetExtensionDirectoryPath();
                    string appSettingsPath = Path.Combine(extensionPath, AppSettingsFileName);

#if DEBUG
                    // In Debug mode, attempt to use the appsettings.development.json file if present
                    var developmentAppSettingsPath = Path.Combine(extensionPath, AppSettingsDevelopmentFileName);
                    if (File.Exists(developmentAppSettingsPath))
                    {
                        appSettingsPath = developmentAppSettingsPath;
                    }
#endif

                    appSettings = Json.Deserialize<SnykAppSettings>(File.ReadAllText(appSettingsPath, Encoding.UTF8));
                }

                return appSettings;
            }
        }

        /// <summary>
        /// Get extension directory path.
        /// </summary>
        /// <returns>Extension directory path.</returns>
        public static string GetExtensionDirectoryPath()
        {
            if (string.IsNullOrEmpty(extensionDirectoryPath))
            {
                string codebase = typeof(SnykExtension).Assembly.CodeBase;

                var uri = new Uri(codebase, UriKind.Absolute);

                extensionDirectoryPath = Directory.GetParent(uri.LocalPath).FullName;
            }

            return extensionDirectoryPath;
        }

        /// <summary>
        /// Get integration version.
        /// </summary>
        /// <returns>String.</returns>
        private static string GetIntegrationVersion()
        {
            try
            {
                var extensionPath = GetExtensionDirectoryPath();

                var manifestPath = Path.Combine(extensionPath, "extension.vsixmanifest");

                var xmlDocument = new XmlDocument();
                xmlDocument.Load(manifestPath);

                if (xmlDocument.DocumentElement?.Name != "PackageManifest")
                {
                    return "UNKNOWN";
                }

                var metaData = xmlDocument.DocumentElement.ChildNodes.Cast<XmlElement>().First(x => x.Name == "Metadata");
                var identity = metaData.ChildNodes.Cast<XmlElement>().First(x => x.Name == "Identity");

                return identity.GetAttribute("Version");
            }
            catch (Exception e)
            {
                // The Exception.ToString() containing the exception type, message,
                // stack trace, and all of these things again for nested/inner exceptions.
                Console.Error.WriteLine(e.ToString());
                
                return string.Empty;
            }
        }
    }
}