﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

namespace Snyk.VisualStudio.Extension.CLI
{   
    public class SnykCliDownloader
    {        
        private const string LatestReleasesUrl = "https://api.github.com/repos/snyk/snyk/releases/latest";

        private const string LatestReleaseDownloadUrl = "https://github.com/snyk/snyk/releases/download/{0}/{1}";

        public delegate void CliDownloadFinishedCallback();

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

                return JsonConvert.DeserializeObject<LatestReleaseInfo>(latestReleasesInfoJson);
            }            
        }

        public void Download(string cliFileDestinationPath = null, 
            ISnykProgressWorker progressWorker = null, 
            CliDownloadFinishedCallback downloadFinishedCallback = null)
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
                        AsynchronousDownload(webClient, progressWorker, cliFileDestinationPath, cliDownloadUrl, downloadFinishedCallback);   
                    }
                    else
                    {
                        SynchronousDownload(webClient, cliFileDestinationPath, cliDownloadUrl);
                    }                    
                }
            }
        }

        private void SynchronousDownload(WebClient webClient, 
            string cliFileDestinationPath, 
            string cliDownloadUrl, 
            CliDownloadFinishedCallback downloadFinishedCallback = null)
        {
            logger?.LogInformation("Enter SynchronousDownload method");

            webClient.DownloadFile(cliDownloadUrl, cliFileDestinationPath);

            if (downloadFinishedCallback != null)
            {
                downloadFinishedCallback();
            }
        } 
            
        private void AsynchronousDownload(WebClient webClient, 
            ISnykProgressWorker progressWorker, 
            string cliFileDestinationPath, 
            string cliDownloadUrl,
            CliDownloadFinishedCallback downloadFinishedCallback = null)
        {
            logger?.LogInformation("Enter AsynchronousDownload method");

            webClient.DownloadProgressChanged += (source, progressChangedEvent) =>
            {
                try
                {
                    progressWorker.UpdateProgress(progressChangedEvent.ProgressPercentage);

                    progressWorker.CancelIfCancellationRequested();
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

            webClient.DownloadFileCompleted += (sender, completedEventArgs) =>
            {
                logger?.LogInformation("Fire DownloadFinished event");

                progressWorker.DownloadFinished();

                if (downloadFinishedCallback != null)
                {
                    downloadFinishedCallback();
                }
            };

            webClient.DownloadFileAsync(new Uri(cliDownloadUrl), cliFileDestinationPath);

            progressWorker.CancelIfCancellationRequested();
        }                     
    }
}
