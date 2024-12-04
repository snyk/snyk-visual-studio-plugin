using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace Snyk.VisualStudio.Extension
{
    /// <summary>
    /// Common Snyk extension message.
    /// </summary>
    public class SnykExtension
    {
        /// <summary>
        /// Integration name.
        /// </summary>
        public const string IntegrationName = "VISUAL_STUDIO";

        private static string extensionDirectoryPath;
        private static readonly Lazy<string> versionLazy = new(GetIntegrationVersion);

        /// <summary>
        /// Gets extension version.
        /// </summary>
        public static string Version => versionLazy.Value;

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