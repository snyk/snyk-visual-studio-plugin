namespace Snyk.VisualStudio.Extension.Shared.CLI.Download
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.Service;

    /// <summary>
    /// Donwnload last Snyk CLI version.
    /// </summary>
    public class SnykCliDownloader
    {
        private const string BaseUrl = "https://static.snyk.io";
        private const string LatestReleaseVersionUrl = BaseUrl + "/cli/latest/version";
        private const string LatestReleaseDownloadUrl = BaseUrl + "/cli/latest/{0}";
        private const string Sha256DownloadUrl = BaseUrl + "/cli/latest/snyk-win.exe.sha256";

        private const int FourDays = 4;

        private static readonly ILogger Logger = LogManager.ForContext<SnykCliDownloader>();

        private string currentCliVersion;

        private string expectedSha;

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
        /// <summary>
        public LatestReleaseInfo GetLatestReleaseInfo()
        {
            Logger.Information("Enter GetLatestReleaseInfo method");

            using (var webClient = new SnykWebClient())
            {
                Logger.Information("Get latest CLI release info");

                string latestVersion = webClient.DownloadString(LatestReleaseVersionUrl).Replace("\n", string.Empty);

                return new LatestReleaseInfo
                {
                    Version = latestVersion,
                    Url = string.Format(LatestReleaseDownloadUrl, SnykCli.CliFileName),
                    Name = "v" + latestVersion,
                };
            }
        }

        /// <summary>
        /// Request last cli sha.
        /// </summary>
        /// <returns>CLI sha string.</returns>
        /// <summary>
        public string GetLatestCliSha()
        {
            Logger.Information("Enter GetLatestCliSha method");

            using (var webClient = new SnykWebClient())
            {
                Logger.Information("Get latest CLI sha");

                string result = webClient.DownloadString(Sha256DownloadUrl)
                    .Replace(SnykCli.CliFileName, string.Empty)
                    .Replace("\n", string.Empty)
                    .Trim();

                return result;
            }
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
        public bool IsCliDownloadNeeded(DateTime lastCheckDate, string cliFileDestinationPath = null)
        {
            if (!this.IsCliFileExists(cliFileDestinationPath) || this.IsCliUpdateExists(lastCheckDate))
            {
                return true;
            }

            return false;
        }

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
                    && this.IsNewVersionAvailable(this.currentCliVersion, this.GetLatestReleaseInfo().Name);

        /// <summary>
        /// Check is there a new version on the server and if there is, download it.
        /// </summary>
        /// <param name="progressWorker">Progress worker for update get download progress.</param>
        /// <param name="lastCheckDate">Last date when it check for CLI updates.</param>
        /// <param name="filePath">CLI file destination path or null.</param>
        /// <param name="downloadFinishedCallbacks">List of callback for download finished event.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AutoUpdateCliAsync(
            ISnykProgressWorker progressWorker,
            DateTime lastCheckDate,
            string filePath = null,
            List<CliDownloadFinishedCallback> downloadFinishedCallbacks = null)
        {
            string fileDestinationPath = this.GetCliFilePath(filePath);

            if (this.IsCliDownloadNeeded(lastCheckDate, filePath))
            {
                await this.DownloadAsync(
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
        /// <param name="progressWorker">Progress worker for update get download progress.</param>
        /// <param name="fileDestinationPath">Path to destination cli file.</param>
        /// <param name="downloadFinishedCallbacks">List of Callbacks for download finished event.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task DownloadAsync(
            ISnykProgressWorker progressWorker,
            string fileDestinationPath = null,
            List<CliDownloadFinishedCallback> downloadFinishedCallbacks = null)
        {
            Logger.Information("Enter Download method");

            string cliFileDestinationPath = this.GetCliFilePath(fileDestinationPath);

            Logger.Information("CLI File Destination Path: {Path}", cliFileDestinationPath);

            progressWorker.DownloadStarted();

            progressWorker.CancelIfCancellationRequested();

            Logger.Information("Got latest relase information");

            LatestReleaseInfo latestReleaseInfo = this.GetLatestReleaseInfo();

                    string cliVersion = latestReleaseInfo.TagName;

                    Logger.Information("Latest relase information CLI version: {CliVersion}", cliVersion);

                    string cliDownloadUrl = string.Format(LatestReleaseDownloadUrl, cliVersion, SnykCli.CliFileName);
            Logger.Information("Latest relase information: version {Version} and url {Url}", latestReleaseInfo.Version, latestReleaseInfo.Url);

            progressWorker.CancelIfCancellationRequested();

            this.PrepareSnykCliDirectory();

            await this.DownloadAsync(
                progressWorker,
                cliFileDestinationPath,
                latestReleaseInfo.Url,
                downloadFinishedCallbacks);
        }

        /// <summary>
        /// Verify cli file sha. If it's not correct method will from <see cref="ChecksumVerificationException"/> exception.
        /// </summary>
        /// <param name="cliPath">CLI file full path.</param>
        /// <exception cref="ChecksumVerificationException">Exception if cli sha not correct.</exception>
        public void VerifyCliFile(string cliPath)
        {
            if (!this.IsCliFileExists(cliPath))
            {
                throw new ChecksumVerificationException("Cli file not exists, can't verify checksum");
            }

            string currentSha = Sha256.Checksum(cliPath);

            if (this.expectedSha.ToLower() != currentSha.ToLower())
            {
                throw new ChecksumVerificationException($"Expected {this.expectedSha}, but downloaded file has {currentSha}");
            }
        }

        /// <summary>
        /// Initialize extectedSha property with latest value from server.
        /// </summary>
        public void SaveLatestCliSha() => this.expectedSha = this.GetLatestCliSha();

        private void PrepareSnykCliDirectory()
        {
            string snykDirectoryPath = SnykDirectory.GetSnykAppDataDirectoryPath();

            if (!Directory.Exists(snykDirectoryPath))
            {
                Directory.CreateDirectory(snykDirectoryPath);
            }
        }

        private async Task DownloadAsync(
            ISnykProgressWorker progressWorker,
            string cliFileDestinationPath,
            string cliDownloadUrl,
            List<CliDownloadFinishedCallback> downloadFinishedCallbacks = null)
        {
            Logger.Information("Enter AsynchronousDownload method");

            try
            {
                this.SaveLatestCliSha();

                await this.DownloadFileAsync(progressWorker, cliDownloadUrl, cliFileDestinationPath);
            }
            catch (ChecksumVerificationException e)
            {
                Logger.Error(e, "Error on cli file download");

                await this.DownloadFileAsync(progressWorker, cliDownloadUrl, cliFileDestinationPath);
            }

            this.FinishDownload(progressWorker, downloadFinishedCallbacks);
        }

        private async Task DownloadFileAsync(ISnykProgressWorker progressWorker, string cliDownloadUrl, string cliFileDestinationPath)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                var response = await client.GetAsync(cliDownloadUrl, HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode();

                string tempCliFile = Path.GetTempFileName();

                using (var fileStream = new FileStream(tempCliFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 8192, true))
                {
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
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
                    }
                }

                this.VerifyCliFile(tempCliFile);

                File.Copy(tempCliFile, cliFileDestinationPath);

                File.Delete(tempCliFile);
            }
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
            catch (FormatException e)
            {
                return -1;
            }
        }

        private string GetCliFilePath(string filePath) => string.IsNullOrEmpty(filePath) ? SnykCli.GetSnykCliPath() : filePath;
    }
}
