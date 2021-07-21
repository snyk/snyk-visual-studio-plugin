namespace Snyk.VisualStudio.Extension.CLI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Donwnload last Snyk CLI version.
    /// </summary>
    public class SnykCliDownloader
    {
        private const string LatestReleasesUrl = "https://api.github.com/repos/snyk/snyk/releases/latest";

        private const string LatestReleaseDownloadUrl = "https://github.com/snyk/snyk/releases/download/{0}/{1}";

        private const int FourDays = 4;

        private readonly SnykActivityLogger logger = null;

        private string currentCliVersion;

        private LatestReleaseInfo latestReleaseInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCliDownloader"/> class.
        /// </summary>
        /// <param name="logger">ActivityLogger parameter.</param>
        public SnykCliDownloader(SnykActivityLogger logger) => this.logger = logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCliDownloader"/> class.
        /// </summary>
        /// <param name="currentCliVersion">Initial CLI version parameter.</param>
        /// <param name="logger">ActivityLogger parameter.</param>
        public SnykCliDownloader(string currentCliVersion, SnykActivityLogger logger)
            : this(logger) => this.currentCliVersion = currentCliVersion;

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

            if (this.latestReleaseInfo == null)
            {
                using (var webClient = new SnykWebClient())
                {
                    this.logger?.LogError("Downloading latest CLI release info");

                    string latestReleasesInfoJson = webClient.DownloadString(LatestReleasesUrl);

                    this.logger?.LogError("Deserialize latest CLI release info");

                    this.latestReleaseInfo = JsonSerializer.Deserialize<LatestReleaseInfo>(latestReleasesInfoJson);
                }
            }

            return this.latestReleaseInfo;
        }

        /// <summary>
        /// Compare CLI versions and if new version string is more new to current version method will return true.
        /// </summary>
        /// <param name="currentVersionStr">Current CLI version.</param>
        /// <param name="newVersionStr">New CLI version.</param>
        /// <returns>True if there is more new version.</returns>
        public bool IsNewVersionAvailable(string currentVersionStr, string newVersionStr)
        {
            int newVersion = this.CliVersionAsInt(newVersionStr);

            if (newVersion == -1)
            {
                return false;
            }

            int currentVersion = this.CliVersionAsInt(currentVersionStr);

            if (currentVersion == -1)
            {
                return true;
            }

            return newVersion > currentVersion;
        }

        /// <summary>
        /// Check is four days passed after lact check.
        /// </summary>
        /// <param name="lastCheckDate">Last check date value.</param>
        /// <returns>True if four days passed after last check.</returns>
        public bool IsFourDaysPassedAfterLastCheck(DateTime lastCheckDate)
            => (DateTime.Now - lastCheckDate).TotalDays > FourDays;

        /// <summary>
        /// Check is CLI download needed.
        /// 1. If CLI file not exists.
        /// 2. If new CLI release exists.
        /// </summary>
        /// <param name="lastCheckDate">Last check date.</param>
        /// <param name="cliFileDestinationPath">Path to CLI file.</param>
        /// <returns>True if CLI file not exists or new release exists.</returns>
        public bool IsCliDownloadNeeded(DateTime lastCheckDate, string cliFileDestinationPath = null) =>
            !this.IsCliFileExists(cliFileDestinationPath) || this.IsCliUpdateExists(lastCheckDate);

        /// <summary>
        /// Check is CLI file not exists by provided location.
        /// </summary>
        /// <param name="cliFileDestinationPath">CLI location path.</param>
        /// <returns>True if CLI file not exists.</returns>
        public bool IsCliFileExists(string cliFileDestinationPath = null) => File.Exists(cliFileDestinationPath);

        /// <summary>
        /// Is CLI update exists.
        /// </summary>
        /// <param name="lastCheckDate">Last check date.</param>
        /// <returns>True if new version CLI exists</returns>
        public bool IsCliUpdateExists(DateTime lastCheckDate) => this.IsFourDaysPassedAfterLastCheck(lastCheckDate)
                    && this.IsNewVersionAvailable(this.currentCliVersion, this.GetLatestReleaseInfo().CliVersion);

        /// <summary>
        /// Check is there a new version on the server and if there is, download it.
        /// </summary>
        /// <param name="lastCheckDate">Last date when it check for CLI updates.</param>
        /// <param name="filePath">CLI file destination path or null.</param>
        /// <param name="progressWorker">Progress worker for update get download progress.</param>
        /// <param name="downloadFinishedCallbacks">List of callback for download finished event.</param>
        public void AutoUpdateCli(
            DateTime lastCheckDate,
            string filePath = null,
            ISnykProgressWorker progressWorker = null,
            List<CliDownloadFinishedCallback> downloadFinishedCallbacks = null)
        {
            string fileDestinationPath = this.GetCliFilePath(filePath);

            if (this.IsCliDownloadNeeded(lastCheckDate, filePath))
            {
                if (progressWorker != null)
                {
                    progressWorker.IsUpdateDownload =
                        this.IsCliFileExists(fileDestinationPath) && this.IsCliUpdateExists(lastCheckDate);

                    if (progressWorker.IsUpdateDownload)
                    {
                        File.Delete(fileDestinationPath);
                    }
                }

                this.Download(
                    fileDestinationPath: fileDestinationPath,
                    progressWorker: progressWorker,
                    downloadFinishedCallbacks: downloadFinishedCallbacks);
            }
        }

        /// <summary>
        /// Download last CLI instance.
        /// </summary>
        /// <param name="fileDestinationPath">Path to destination cli file.</param>
        /// <param name="progressWorker">Progress worker for update get download progress.</param>
        /// <param name="downloadFinishedCallbacks">List of Callbacks for download finished event.</param>
        public void Download(
            string fileDestinationPath = null,
            ISnykProgressWorker progressWorker = null,
            List<CliDownloadFinishedCallback> downloadFinishedCallbacks = null)
        {
            this.logger?.LogInformation("Enter Download method");

            string cliFileDestinationPath = this.GetCliFilePath(fileDestinationPath);

            this.logger?.LogInformation($"CLI File Destination Path: {cliFileDestinationPath}");

            if (!File.Exists(cliFileDestinationPath))
            {
                this.logger?.LogInformation("CLI file not exists. Starting download");

                progressWorker?.DownloadStarted();

                progressWorker?.CancelIfCancellationRequested();

                this.logger?.LogInformation("Got latest relase information");

                using (var webClient = new SnykWebClient())
                {
                    LatestReleaseInfo latestReleaseInfo = this.GetLatestReleaseInfo();

                    string cliVersion = latestReleaseInfo.TagName;

                    this.logger?.LogInformation($"Latest relase information CLI version: {cliVersion}");

                    string cliDownloadUrl = string.Format(LatestReleaseDownloadUrl, cliVersion, SnykCli.CliFileName);

                    this.logger?.LogInformation($"CLI download url: {cliDownloadUrl}");

                    string snykDirectoryPath = SnykDirectory.GetSnykAppDataDirectoryPath();

                    progressWorker?.CancelIfCancellationRequested();

                    Directory.CreateDirectory(snykDirectoryPath);

                    if (progressWorker != null)
                    {
                        this.AsynchronousDownload(webClient, progressWorker, cliFileDestinationPath, cliDownloadUrl, downloadFinishedCallbacks);
                    }
                    else
                    {
                        this.SynchronousDownload(webClient, cliFileDestinationPath, cliDownloadUrl, downloadFinishedCallbacks);
                    }
                }
            }
        }

        private void SynchronousDownload(
            WebClient webClient,
            string cliFileDestinationPath,
            string cliDownloadUrl,
            List<CliDownloadFinishedCallback> downloadFinishedCallbacks = null)
        {
            this.logger?.LogInformation("Enter SynchronousDownload method");

            webClient.DownloadFile(cliDownloadUrl, cliFileDestinationPath);

            if (downloadFinishedCallbacks != null)
            {
                downloadFinishedCallbacks.ForEach(downloadFinishedCallback =>
                {
                    downloadFinishedCallback();
                });
            }
        }

        private void AsynchronousDownload(
            WebClient webClient,
            ISnykProgressWorker progressWorker,
            string cliFileDestinationPath,
            string cliDownloadUrl,
            List<CliDownloadFinishedCallback> downloadFinishedCallbacks = null)
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

                if (downloadFinishedCallbacks != null)
                {
                    downloadFinishedCallbacks.ForEach(downloadFinishedCallback =>
                    {
                        downloadFinishedCallback();
                    });
                }
            };

            webClient.DownloadFileAsync(new Uri(cliDownloadUrl), cliFileDestinationPath);

            progressWorker.CancelIfCancellationRequested();
        }

        /// <summary>
        /// Convert String cli version to int value.
        /// </summary>
        /// <param name="cliVersion">Source CLI version</param>
        /// <returns>Int value, if CLI version string is incorrect it will return -1.</returns>
        private int CliVersionAsInt(string cliVersion)
        {
            try
            {
                return int.Parse(cliVersion.Replace(".", string.Empty));
            }
            catch (FormatException ignore)
            {
                return -1;
            }
        }

        private string GetCliFilePath(string filePath) => string.IsNullOrEmpty(filePath) ? SnykCli.GetSnykCliPath() : filePath;
    }
}
