using System;
using System.IO;
using System.Net;
using Snyk.VisualStudio.Extension.Util;

namespace Snyk.VisualStudio.Extension.CLI
{   
    public class SnykCliDownloader
    {        
        private const string LatestReleasesUrl = "https://api.github.com/repos/snyk/snyk/releases/latest";

        private const string LatestReleaseDownloadUrl = "https://github.com/snyk/snyk/releases/download/{0}/{1}";

        public static SnykCliDownloader NewInstance() => new SnykCliDownloader();

        public LatestReleaseInfo GetLatestReleaseInfo(WebClient webClient)
        {
            string latestReleasesInfoJson = webClient.DownloadString(LatestReleasesUrl);
            
            return (LatestReleaseInfo) Json.Deserialize(latestReleasesInfoJson, typeof(LatestReleaseInfo));
        }

        public void Download(string cliFileDestinationPath = null)
        {
            if (cliFileDestinationPath == null)
            {
                cliFileDestinationPath = SnykCli.GetSnykCliPath();
            }
        
            if (!File.Exists(cliFileDestinationPath))
            {
                using (var webClient = new SnykWebClient())
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
    }
}
