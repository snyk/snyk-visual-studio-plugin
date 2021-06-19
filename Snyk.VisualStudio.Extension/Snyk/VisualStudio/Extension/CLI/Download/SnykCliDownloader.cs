namespace Snyk.VisualStudio.Extension.CLI
{
    using System;
    using System.IO;
    using System.Net;
    using Service;
    using Snyk.Common;

    /// <summary>
    /// Donwnload last Snyk CLI version.
    /// </summary>
    public class SnykCliDownloader
    {
        private const string LatestReleasesUrl = "https://api.github.com/repos/snyk/snyk/releases/latest";

        private const string LatestReleaseDownloadUrl = "https://github.com/snyk/snyk/releases/download/{0}/{1}";

        private readonly SnykActivityLogger logger = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCliDownloader"/> class.
        /// </summary>
        /// <param name="logger">ActivityLogger parameter.</param>
        public SnykCliDownloader(SnykActivityLogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Callback on download finished event.
        /// </summary>
        public delegate void CliDownloadFinishedCallback();

        /// <summary>
        /// Request last cli information.
        /// </summary>
        /// <returns>Latest CLI relaese information.</returns>
        public LatestReleaseInfo GetLatestReleaseInfo()
        {
            this.logger?.LogError("Enter GetLatestReleaseInfo method");

            using (var webClient = new SnykWebClient())
            {
                this.logger?.LogError("Downloading latest CLI release info");

                string latestReleasesInfoJson = webClient.DownloadString(LatestReleasesUrl);

                this.logger?.LogError("Deserialize latest CLI release info");

                return Json.Deserialize<LatestReleaseInfo>(latestReleasesInfoJson);
            }
        }

        /// <summary>
        /// Download last CLI instance.
        /// </summary>
        /// <param name="cliFileDestinationPath">Path to destination cli file.</param>
        /// <param name="progressWorker">Progress worker for update get download progress.</param>
        /// <param name="downloadFinishedCallback">Callback for download finished event.</param>
        public void Download(
            string cliFileDestinationPath = null,
            ISnykProgressWorker progressWorker = null,
            CliDownloadFinishedCallback downloadFinishedCallback = null)
        {
            this.logger?.LogInformation("Enter Download method");

            if (cliFileDestinationPath == null)
            {
                cliFileDestinationPath = SnykCli.GetSnykCliPath();
            }

            this.logger?.LogInformation($"CLI File Destination Path: {cliFileDestinationPath}");

            if (!File.Exists(cliFileDestinationPath))
            {
                this.logger?.LogInformation("CLI file not exists. Starting download");

                progressWorker?.DownloadStarted();

                progressWorker?.CancelIfCancellationRequested();

                LatestReleaseInfo latestReleaseInfo = this.GetLatestReleaseInfo();

                this.logger?.LogInformation("Got latest relase information");

                using (var webClient = new SnykWebClient())
                {
                    string cliVersion = latestReleaseInfo.TagName;

                    this.logger?.LogInformation($"Latest relase information CLI version: {cliVersion}");

                    string cliDownloadUrl = string.Format(LatestReleaseDownloadUrl, cliVersion, SnykCli.CliFileName);

                    this.logger?.LogInformation($"CLI download url: {cliDownloadUrl}");

                    string snykDirectoryPath = SnykCli.GetSnykDirectoryPath();

                    progressWorker?.CancelIfCancellationRequested();

                    Directory.CreateDirectory(snykDirectoryPath);

                    if (progressWorker != null)
                    {
                        this.AsynchronousDownload(webClient, progressWorker, cliFileDestinationPath, cliDownloadUrl, downloadFinishedCallback);   
                    }
                    else
                    {
                        this.SynchronousDownload(webClient, cliFileDestinationPath, cliDownloadUrl);
                    }
                }
            }
        }

        private void SynchronousDownload(
            WebClient webClient,
            string cliFileDestinationPath,
            string cliDownloadUrl,
            CliDownloadFinishedCallback downloadFinishedCallback = null)
        {
            this.logger?.LogInformation("Enter SynchronousDownload method");

            webClient.DownloadFile(cliDownloadUrl, cliFileDestinationPath);

            if (downloadFinishedCallback != null)
            {
                downloadFinishedCallback();
            }
        }

        private void AsynchronousDownload(
            WebClient webClient,
            ISnykProgressWorker progressWorker,
            string cliFileDestinationPath,
            string cliDownloadUrl,
            CliDownloadFinishedCallback downloadFinishedCallback = null)
        {
            this.logger?.LogInformation("Enter AsynchronousDownload method");

            webClient.DownloadProgressChanged += (source, progressChangedEvent) =>
            {
                try
                {
                    progressWorker.UpdateProgress(progressChangedEvent.ProgressPercentage);

                    progressWorker.CancelIfCancellationRequested();
                }
                catch (Exception exception)
                {
                    this.logger?.LogError(exception.Message);

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
                        this.logger?.LogError($"Error: Can't delete temp CLI file. Message: {ex.Message}");
                    }
                }
            };

            webClient.DownloadFileCompleted += (sender, completedEventArgs) =>
            {
                this.logger?.LogInformation("Fire DownloadFinished event");

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
