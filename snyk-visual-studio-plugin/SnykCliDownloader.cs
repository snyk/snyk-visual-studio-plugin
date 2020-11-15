using System;
using System.Text;

using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace snyk_visual_studio_plugin
{
    class SnykCliDownloader
    {
        private const string CliFileName = "snyk-win.exe";
        private const string LatestReleasesUrl = "https://api.github.com/repos/snyk/snyk/releases/latest";
        private const string LatestReleaseDownloadUrl = "https://github.com/snyk/snyk/releases/download/{0}/{1}";

        [DataContract]
        internal class LatestReleaseInfo
        {
            [DataMember]
            internal string Url;

            [DataMember]
            internal int Id;

            [DataMember(Name = "tag_name")]
            internal string TagName;

            [DataMember]
            internal string Name;
        }

        private LatestReleaseInfo getLatestReleaseInfo(WebClient webClient)
        {
            string json = webClient.DownloadString(LatestReleasesUrl);

            var latestReleaseInfo = new LatestReleaseInfo();
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var jsonSerializer = new DataContractJsonSerializer(latestReleaseInfo.GetType());

            latestReleaseInfo = jsonSerializer.ReadObject(memoryStream) as LatestReleaseInfo;

            memoryStream.Close();

            return latestReleaseInfo;
        }

        public void Download()
        {
            if (!File.Exists(GetSnykCliPath()))
            {
                using (var webClient = new WebClient())
                {
                    webClient.Headers.Add("user-agent", "VisualStudioSnykExtension");

                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    LatestReleaseInfo latestReleaseInfo = getLatestReleaseInfo(webClient);

                    string cliVersion = latestReleaseInfo.TagName;

                    string cliDownloadUrl = String.Format(LatestReleaseDownloadUrl, cliVersion, CliFileName);

                    string snykDirectoryPath = GetSnykDirectoryPath();

                    Directory.CreateDirectory(snykDirectoryPath);

                    string cliFileDestinationPath = GetSnykCliPath();

                    webClient.DownloadFile(cliDownloadUrl, cliFileDestinationPath);
                }
            }
        }

        string GetSnykDirectoryPath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return appDataPath + Path.DirectorySeparatorChar + "Snyk";
        }

        string GetSnykCliPath()
        {
            return GetSnykDirectoryPath() + Path.DirectorySeparatorChar + CliFileName;
        }
    }
}
