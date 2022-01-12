namespace Snyk.VisualStudio.Extension.CLI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Donwnload last Snyk CLI version.
    /// </summary>
    public class SnykCliDownloader
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykCliDownloader>();

        private const string LatestReleasesUrl = "https://api.github.com/repos/snyk/snyk/releases/latest";

        private const string LatestReleaseDownloadUrl = "https://github.com/snyk/snyk/releases/download/{0}/{1}";

        private const int FourDays = 4;

        private readonly LatestReleaseInfo latestReleaseInfo;

        private string currentCliVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCliDownloader"/> class.
        /// </summary>
        /// <param name="logger">ActivityLogger parameter.</param>
        public SnykCliDownloader()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCliDownloader"/> class.
        /// </summary>
        /// <param name="currentCliVersion">Initial CLI version parameter.</param>
        /// <param name="logger">ActivityLogger parameter.</param>
        public SnykCliDownloader(string currentCliVersion)
            : this() => this.currentCliVersion = currentCliVersion;

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
            Logger.Information("Enter GetLatestReleaseInfo method");

            if (this.latestReleaseInfo == null)
            {
                using (var webClient = new SnykWebClient())
                {
                    Logger.Information("Downloading latest CLI release info");

                    string latestReleasesInfoJson = webClient.DownloadString(LatestReleasesUrl);

                    Logger.Information("Deserialize latest CLI release info");

                    return Json.Deserialize<LatestReleaseInfo>(latestReleasesInfoJson);
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
            else
            {
                progressWorker.IsWorkFinished = true;
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
            Logger.Information("Enter Download method");

            string cliFileDestinationPath = this.GetCliFilePath(fileDestinationPath);

            Logger.Information($"CLI File Destination Path: {cliFileDestinationPath}");

            if (!File.Exists(cliFileDestinationPath))
            {
                Logger.Information("CLI file not exists. Starting download");

                progressWorker?.DownloadStarted();

                progressWorker?.CancelIfCancellationRequested();

                Logger.Information("Got latest relase information");

                using (var webClient = new SnykWebClient())
                {
                    LatestReleaseInfo latestReleaseInfo = this.GetLatestReleaseInfo();

                    string cliVersion = latestReleaseInfo.TagName;

                    Logger.Information($"Latest relase information CLI version: {cliVersion}");

                    string cliDownloadUrl = string.Format(LatestReleaseDownloadUrl, cliVersion, SnykCli.CliFileName);

                    Logger.Information($"CLI download url: {cliDownloadUrl}");

                    string snykDirectoryPath = SnykDirectory.GetSnykAppDataDirectoryPath();

                    progressWorker?.CancelIfCancellationRequested();

                    Directory.CreateDirectory(snykDirectoryPath);

                    if (progressWorker != null)
                    {
                        this.AsynchronousDownloadAsync(progressWorker, cliFileDestinationPath, cliDownloadUrl, downloadFinishedCallbacks);
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
            Logger.Information("Enter SynchronousDownload method");

            webClient.DownloadFile(cliDownloadUrl, cliFileDestinationPath);

            if (downloadFinishedCallbacks != null)
            {
                downloadFinishedCallbacks.ForEach(downloadFinishedCallback =>
                {
                    downloadFinishedCallback();
                });
            }
        }

        private async Task AsynchronousDownloadAsync(
            ISnykProgressWorker progressWorker,
            string cliFileDestinationPath,
            string cliDownloadUrl,
            List<CliDownloadFinishedCallback> downloadFinishedCallbacks = null)
        {
            Logger.Information("Enter AsynchronousDownload method");

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                var response = await client.GetAsync(cliDownloadUrl, HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode();

                using (Stream contentStream = await response.Content.ReadAsStreamAsync(), fileStream = new FileStream(cliFileDestinationPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 8192, true))
                {
                    var totalBytes = response.Content.Headers.ContentLength;
                    var totalRead = 0L;
                    var buffer = new byte[8192];
                    var isMoreToRead = true;

                    do
                    {
                        var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);

                        if (read == 0)
                        {
                            isMoreToRead = false;
                        }
                        else
                        {
                            await fileStream.WriteAsync(buffer, 0, read);

                            totalRead += read;

                            int percentage = (int)(totalRead * 100 / totalBytes);

                            progressWorker.UpdateProgress(percentage);

                            progressWorker.CancelIfCancellationRequested();
                        }
                    }
                    while (isMoreToRead);

                    this.FinishDownload(progressWorker, downloadFinishedCallbacks);
                }
            }

            progressWorker.CancelIfCancellationRequested();
        }

        private void FinishDownload(ISnykProgressWorker progressWorker, List<CliDownloadFinishedCallback> downloadFinishedCallbacks)
        {
            Logger.Information("Fire DownloadFinished event");

            if (downloadFinishedCallbacks != null)
            {
                downloadFinishedCallbacks.ForEach(downloadFinishedCallback => downloadFinishedCallback());
            }

            progressWorker.DownloadFinished();
        }

        /// <summary>
        /// Convert String cli version to int value.
        /// </summary>
        /// <param name="cliVersion">Source CLI version</param>
        /// <returns>Int value, if CLI version string is incorrect it will return -1.</returns>
        private int CliVersionAsInt(string cliVersion)
        {
            if (string.IsNullOrEmpty(cliVersion))
            {
                return -1;
            }

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
