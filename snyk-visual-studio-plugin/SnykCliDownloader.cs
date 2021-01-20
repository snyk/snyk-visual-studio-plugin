using System;
using System.IO;
using System.Net;
using Snyk.VisualStudio.Extension.Util;
using Snyk.VisualStudio.Extension.UI;

namespace Snyk.VisualStudio.Extension.CLI
{   
    public class SnykCliDownloader
    {        
        private const string LatestReleasesUrl = "https://api.github.com/repos/snyk/snyk/releases/latest";

        private const string LatestReleaseDownloadUrl = "https://github.com/snyk/snyk/releases/download/{0}/{1}";

        private SnykActivityLogger logger = null;

        public SnykCliDownloader(SnykActivityLogger logger)
        {
            this.logger = logger;
        }

        public LatestReleaseInfo GetLatestReleaseInfo()
        {
            logger?.LogError("Enter GetLatestReleaseInfo method");

            using (var webClient = new SnykWebClient())
            {
                logger?.LogError("Downloading latest CLI release info");

                string latestReleasesInfoJson = webClient.DownloadString(LatestReleasesUrl);

                logger?.LogError("Deserialize latest CLI release info");

                return (LatestReleaseInfo)Json.Deserialize(latestReleasesInfoJson, typeof(LatestReleaseInfo));
            }            
        }

        public void Download(string cliFileDestinationPath = null, ISnykProgressWorker progressWorker = null)
        {
            logger?.LogInformation("Enter Download method");

            if (cliFileDestinationPath == null)
            {
                cliFileDestinationPath = SnykCli.GetSnykCliPath();
            }

            logger?.LogInformation($"CLI File Destination Path: {cliFileDestinationPath}");

            if (!File.Exists(cliFileDestinationPath))
            {
                logger?.LogInformation("CLI file not exists. Starting download");

                progressWorker?.DownloadStarted();

                progressWorker?.CancelIfCancellationRequested();

                LatestReleaseInfo latestReleaseInfo = GetLatestReleaseInfo();

                logger?.LogInformation("Got latest relase information");

                using (var webClient = new SnykWebClient())
                {                   
                    string cliVersion = latestReleaseInfo.TagName;

                    logger?.LogInformation($"Latest relase information CLI version: {cliVersion}");

                    string cliDownloadUrl = String.Format(LatestReleaseDownloadUrl, cliVersion, SnykCli.CliFileName);

                    logger?.LogInformation($"CLI download url: {cliDownloadUrl}");

                    string snykDirectoryPath = SnykCli.GetSnykDirectoryPath();

                    progressWorker?.CancelIfCancellationRequested();

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

        private void SynchronousDownload(WebClient webClient, string cliFileDestinationPath, string cliDownloadUrl)
        {
            logger?.LogInformation("Enter SynchronousDownload method");

            webClient.DownloadFile(cliDownloadUrl, cliFileDestinationPath);
        } 
            
        private void AsynchronousDownload(WebClient webClient, 
            ISnykProgressWorker progressWorker, 
            string cliFileDestinationPath, 
            string cliDownloadUrl)
        {
            logger?.LogInformation("Enter AsynchronousDownload method");

            webClient.DownloadProgressChanged += (source, progressChangedEvent) =>
            {
                try
                {
                    progressWorker.UpdateProgress(progressChangedEvent.ProgressPercentage);

                    progressWorker.CancelIfCancellationRequested();

                    if (progressChangedEvent.ProgressPercentage == 100)
                    {
                        logger?.LogInformation("Fire DownloadFinished event");

                        progressWorker.DownloadFinished();
                    }
                }
                catch (Exception exception)
                {
                    logger?.LogError(exception.Message);

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
                        logger?.LogError($"Error: Can't delete temp CLI file. Message: {ex.Message}");
                    }
                }
            };            

            webClient.DownloadFileAsync(new Uri(cliDownloadUrl), cliFileDestinationPath);

            progressWorker.CancelIfCancellationRequested();
        }                     
    }
}
