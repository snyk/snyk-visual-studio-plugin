using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace Snyk.VisualStudio.Extension
{
    public class SnykExtension
    {
        public const string IntegrationName = "VISUAL_STUDIO";

        private static string version;

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

        public static string GetExtensionDirectoryPath()
        {
            string codebase = typeof(SnykVSPackage).Assembly.CodeBase;

            var uri = new Uri(codebase, UriKind.Absolute);
                        
            return Directory.GetParent(uri.LocalPath).FullName;
        }

        public class Guids
        {
            public static readonly Guid SnykVSPackageCommandSet = new Guid("{31b6f1bd-8317-4d93-b023-b60f667b9e76}");
            
            public const int SnykToolbarId = 0x501;
            public const int RunScanCommandId = 0x503;
            public const int StopCommandId = 0x504;
            public const int CleanCommandId = 0x505;
            public const int OptionsCommandId = 0x506;
        }
    }
}
