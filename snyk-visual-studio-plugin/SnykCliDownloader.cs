using System;
using System.Text;

using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;

namespace Snyk.VisualStudio.Extension.CLI
{   
    public class SnykCliDownloader
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

        public void Download(string cliFileDestinationPath = null)
        {
            if (cliFileDestinationPath == null)
            {
                cliFileDestinationPath = SnykCli.GetSnykCliPath();
            }
        
            if (!File.Exists(cliFileDestinationPath))
            {
                using (var webClient = BuildWebClient())
                {                    
                    LatestReleaseInfo latestReleaseInfo = GetLatestReleaseInfo(webClient);

                    string cliVersion = latestReleaseInfo.TagName;

                    string cliDownloadUrl = String.Format(LatestReleaseDownloadUrl, cliVersion, SnykCli.CliFileName);

                    string snykDirectoryPath = SnykCli.GetSnykDirectoryPath();

                    Directory.CreateDirectory(snykDirectoryPath);

                    webClient.DownloadFile(cliDownloadUrl, cliFileDestinationPath);
                }
            }
        }    
        
        public WebClient BuildWebClient()
        {
            var webClient = new WebClient();

            webClient.Headers.Add("user-agent", "SnykVisualStudioExtension");

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            return webClient;
        }    
    }
}
