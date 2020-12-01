using System;
using System.Text;

using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Snyk.VisualStudio.Extension.CLI
{
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

    class SnykCliDownloader
    {        
        private const string LatestReleasesUrl = "https://api.github.com/repos/snyk/snyk/releases/latest";
        private const string LatestReleaseDownloadUrl = "https://github.com/snyk/snyk/releases/download/{0}/{1}";
        
        public LatestReleaseInfo GetLatestReleaseInfo(WebClient webClient)
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
            if (!File.Exists(SnykCli.GetSnykCliPath()))
            {
                using (var webClient = new WebClient())
                {
                    webClient.Headers.Add("user-agent", "VisualStudioSnykExtension");

                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    LatestReleaseInfo latestReleaseInfo = GetLatestReleaseInfo(webClient);

                    string cliVersion = latestReleaseInfo.TagName;

                    string cliDownloadUrl = String.Format(LatestReleaseDownloadUrl, cliVersion, SnykCli.CliFileName);

                    string snykDirectoryPath = SnykCli.GetSnykDirectoryPath();

                    Directory.CreateDirectory(snykDirectoryPath);

                    string cliFileDestinationPath = SnykCli.GetSnykCliPath();

                    webClient.DownloadFile(cliDownloadUrl, cliFileDestinationPath);
                }
            }
        }        
    }
}
