using System;
using System.IO;
using System.Net;
using Snyk.VisualStudio.Extension.Util;
using Snyk.VisualStudio.Extension.UI;
using System.Windows;

namespace Snyk.VisualStudio.Extension.CLI
{   
    public class SnykCliDownloader
    {        
        private const string LatestReleasesUrl = "https://api.github.com/repos/snyk/snyk/releases/latest";

        private const string LatestReleaseDownloadUrl = "https://github.com/snyk/snyk/releases/download/{0}/{1}";

        public static SnykCliDownloader NewInstance() => new SnykCliDownloader();

        public LatestReleaseInfo GetLatestReleaseInfo()
        {
            using (var webClient = new SnykWebClient())
            {
                string latestReleasesInfoJson = webClient.DownloadString(LatestReleasesUrl);

                return (LatestReleaseInfo)Json.Deserialize(latestReleasesInfoJson, typeof(LatestReleaseInfo));
            }            
        }

        public void Download(string cliFileDestinationPath = null, IProgressWorker progressWorker = null)
        {
            if (cliFileDestinationPath == null)
            {
                cliFileDestinationPath = SnykCli.GetSnykCliPath();
            }
        
            if (!File.Exists(cliFileDestinationPath))
            {
                progressWorker.DownloadStarted();

                progressWorker.CancelIfCancellationRequested();

                LatestReleaseInfo latestReleaseInfo = GetLatestReleaseInfo();

                using (var webClient = new SnykWebClient())
                {                   
                    string cliVersion = latestReleaseInfo.TagName;

                    string cliDownloadUrl = String.Format(LatestReleaseDownloadUrl, cliVersion, SnykCli.CliFileName);

                    string snykDirectoryPath = SnykCli.GetSnykDirectoryPath();

                    progressWorker.CancelIfCancellationRequested();

                    Directory.CreateDirectory(snykDirectoryPath);

                    if (progressWorker != null)
                    {
                        AsynchronousDownload(webClient, progressWorker, cliFileDestinationPath, cliDownloadUrl);   
                    }
                    else
                    {
                        SynchronousDownload(webClient, cliFileDestinationPath, cliDownloadUrl);
                    }                    
                }
            }
        }

        private void SynchronousDownload(WebClient webClient, string cliFileDestinationPath, string cliDownloadUrl) => 
            webClient.DownloadFile(cliDownloadUrl, cliFileDestinationPath);

        private void AsynchronousDownload(WebClient webClient, 
            IProgressWorker progressWorker, 
            string cliFileDestinationPath, 
            string cliDownloadUrl)
        {
            /*webClient.DownloadFileCompleted += (source, downloadCompletedEvent) =>
            {
                if (downloadCompletedEvent.Cancelled)
                {
                    File.Delete(cliFileDestinationPath);
                }

                progressWorker.DownloadFinished();
            };*/
            
            webClient.DownloadProgressChanged += (source, progressChangedEvent) =>
            {
                try
                {
                    progressWorker.UpdateProgress(progressChangedEvent.ProgressPercentage);

                    progressWorker.CancelIfCancellationRequested();

                    if (progressChangedEvent.ProgressPercentage == 100)
                    {
                        progressWorker.DownloadFinished();
                    }
                }
                catch (Exception exception)
                {
                    webClient.CancelAsync();

                    progressWorker.DownloadCancelled(exception.Message);

                    try
                    {
                        if (File.Exists(cliFileDestinationPath))
                        {
                            File.Delete(cliFileDestinationPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        // FIXME: Move this to proper class.
                        MessageBox.Show("Error: Can't delete temp CLI file. Message: " + ex.Message);
                    }
                }
            };            

            webClient.DownloadFileAsync(new Uri(cliDownloadUrl), cliFileDestinationPath);

            progressWorker.CancelIfCancellationRequested();
        }                     
    }
}
