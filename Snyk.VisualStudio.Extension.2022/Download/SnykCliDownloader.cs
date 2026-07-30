using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Extension;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.UI.Notifications;

namespace Snyk.VisualStudio.Extension.Download
{
    /// <summary>
    /// Donwnload last Snyk CLI version.
    /// </summary>
    public class SnykCliDownloader
    {
        public const string DefaultBaseDownloadUrl = "https://downloads.snyk.io";
        public const string DefaultReleaseChannel = "stable";

        private const string LatestReleaseVersionUrlScheme = "{0}/cli/{1}/ls-protocol-version-" + LsConstants.ProtocolVersion;
        private const string LatestReleaseDownloadUrlScheme = "{0}/cli/{1}/" + SnykCli.CliFileName;
        private const string Sha256DownloadUrl = "{0}.sha256";

        private static readonly ILogger Logger = LogManager.ForContext<SnykCliDownloader>();

        private readonly ISnykOptions SnykOptions;
        private string expectedSha;

        public SnykCliDownloader(ISnykOptions snykOptions)
        {
            this.SnykOptions = snykOptions;
        }

        /// <summary>
        /// Callback on download finished event.
        /// </summary>
        public delegate void CliDownloadFinishedCallback();

        /// <summary>
        /// Resolve the CLI download base URL, falling back to the default unless the configured value
        /// is an absolute http/https URL. Anything else — empty (the user clearing the field, or the
        /// Language Server echoing back binary_base_url, which it registers with an empty default) or a
        /// scheme-less host like "downloads.snyk.io" — composes into a RELATIVE url, which
        /// <see cref="System.Net.WebClient"/> resolves through Path.GetFullPath into a nonexistent
        /// local file path and reports as a DirectoryNotFoundException.
        /// </summary>
        public static string ResolveBaseDownloadUrl(string configuredBaseDownloadUrl) =>
            UriExtensions.IsValidWebUrl(configuredBaseDownloadUrl)
                ? configuredBaseDownloadUrl
                : DefaultBaseDownloadUrl;

        /// <summary>
        /// Resolve the CLI release channel, treating an unset or cleared value as the default.
        /// Same reasoning as <see cref="ResolveBaseDownloadUrl"/>.
        /// </summary>
        public static string ResolveReleaseChannel(string configuredReleaseChannel) =>
            string.IsNullOrWhiteSpace(configuredReleaseChannel)
                ? DefaultReleaseChannel
                : configuredReleaseChannel;

        // internal for testability (InternalsVisibleTo test project): lets the URL-construction tests
        // pin the resolved URLs without hitting the network.
        internal string BuildLatestReleaseVersionUrl() => string.Format(
            LatestReleaseVersionUrlScheme,
            ResolveBaseDownloadUrl(SnykOptions.CliBaseDownloadURL),
            ResolveReleaseChannel(SnykOptions.CliReleaseChannel));

        internal string BuildCliDownloadUrl(string version) => string.Format(
            LatestReleaseDownloadUrlScheme,
            ResolveBaseDownloadUrl(SnykOptions.CliBaseDownloadURL),
            version);

        /// <summary>
        /// Request last cli information.
        /// </summary>
        /// <returns>Latest CLI relaese information.</returns>
        public LatestReleaseInfo GetLatestReleaseInfo()
        {
            Logger.Information("Enter GetLatestReleaseInfo method");

            using (var webClient = new SnykWebClient())
            {
                var latestReleaseVersionUrl = this.BuildLatestReleaseVersionUrl();

                // Log the composed URL and the raw options it came from: without this the only symptom
                // of an unusable base url / release channel is a DirectoryNotFoundException from deep
                // inside WebClient, naming a local path that appears nowhere in the settings.
                Logger.Information(
                    "Get latest CLI release info from {Url} (configured base url: '{BaseDownloadUrl}', release channel: '{ReleaseChannel}')",
                    latestReleaseVersionUrl,
                    this.SnykOptions.CliBaseDownloadURL,
                    this.SnykOptions.CliReleaseChannel);

                var latestVersion = webClient.DownloadString(latestReleaseVersionUrl).Replace("\n", string.Empty);

                return new LatestReleaseInfo
                {
                    Version = "v" + latestVersion,
                    Url = this.BuildCliDownloadUrl("v" + latestVersion),
                    Name = "v" + latestVersion,
                };
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
            return currentVersionStr != newVersionStr;
        }

        /// <summary>
        /// Request last cli sha.
        /// </summary>
        /// <returns>CLI sha string.</returns>
        public string GetLatestCliSha(string cliDownloadUrl)
        {
            Logger.Information("Enter GetLatestCliSha method");

            using (var webClient = new SnykWebClient())
            {
                Logger.Information("Get latest CLI sha");
                var shaDownloadUrl = string.Format(Sha256DownloadUrl, cliDownloadUrl);
                var result = webClient.DownloadString(shaDownloadUrl)
                    .Replace(SnykCli.CliFileName, string.Empty)
                    .Replace("\n", string.Empty)
                    .Trim();

                return result;
            }
        }

        /// <summary>
        /// Check is CLI download needed.
        /// 1. If CLI file not exists.
        /// 2. If new CLI release exists.
        /// </summary>
        /// <param name="cliFileDestinationPath">Path to CLI file.</param>
        /// <returns>True if CLI file not exists or new release exists.</returns>
        public bool IsCliDownloadNeeded(string cliFileDestinationPath = null)
        {
            try
            {
                if (!this.IsCliFileExists(cliFileDestinationPath) || this.IsNewVersionAvailable(this.SnykOptions.CurrentCliVersion, this.GetLatestReleaseInfo().Name))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Could not fetch latest CLI release info for provided version {Ex}", ex);
                return false;
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
        /// Check is there a new version on the server and if there is, download it.
        /// </summary>
        /// <param name="progressWorker">Progress worker for update get download progress.</param>
        /// <param name="filePath">CLI file destination path or null.</param>
        /// <param name="downloadFinishedCallbacks">List of callback for download finished event.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AutoUpdateCliAsync(ISnykProgressWorker progressWorker,
            string filePath = null,
            List<CliDownloadFinishedCallback> downloadFinishedCallbacks = null)
        {
            var fileDestinationPath = SnykCli.GetCliFilePath(filePath);

            var isCliDownloadNeeded = this.IsCliDownloadNeeded(fileDestinationPath);

            if (isCliDownloadNeeded)
            {
                await this.DownloadAsync(
                    progressWorker,
                    fileDestinationPath,
                    downloadFinishedCallbacks);
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

            var cliFileDestinationPath = SnykCli.GetCliFilePath(fileDestinationPath);

            Logger.Information("CLI File Destination Path: {Path}", cliFileDestinationPath);

            progressWorker.DownloadStarted();

            progressWorker.CancelIfCancellationRequested();

            Logger.Information("Got latest relase information");

            LatestReleaseInfo latestReleaseInfo = this.GetLatestReleaseInfo();

            Logger.Information("Latest relase information: version {Version} and url {Url}", latestReleaseInfo.Version, latestReleaseInfo.Url);

            progressWorker.CancelIfCancellationRequested();

            PrepareCliDirectory(cliFileDestinationPath);

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
                throw new FileNotFoundException($"Cli file not found in {cliPath}");
            }

            var currentSha = Sha256.Checksum(cliPath);

            if (this.expectedSha.ToLower() != currentSha.ToLower())
            {
                throw new ChecksumVerificationException(this.expectedSha, currentSha);
            }
        }

        /// <summary>
        /// Initialize extectedSha property with latest value from server.
        /// </summary>
        public void SaveLatestCliSha(string cliDownloadUrl) => this.expectedSha = this.GetLatestCliSha(cliDownloadUrl);

        /// <summary>
        /// Create the folder the CLI is about to be written into. This is the folder of the CONFIGURED
        /// destination, not the plugin's own AppData folder: snyk-ls reports its own CLI location
        /// (<c>%LocalAppData%\snyk-ls\snyk-win.exe</c>) as <c>cli_path</c>, which lands in CliCustomPath,
        /// so the destination folder may not exist yet.
        /// </summary>
        // internal static for testability (InternalsVisibleTo test project).
        internal static void PrepareCliDirectory(string cliFileDestinationPath)
        {
            if (string.IsNullOrEmpty(cliFileDestinationPath))
            {
                return;
            }

            var directoryPath = Path.GetDirectoryName(cliFileDestinationPath);

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// True when the binary already at <paramref name="cliFileDestinationPath"/> is the one we are
        /// about to install. snyk-ls downloads the same CLI to the same path and runs it, so overwriting
        /// an identical binary achieves nothing and can fail with a sharing violation.
        /// </summary>
        // internal static for testability (InternalsVisibleTo test project).
        internal static bool IsCliUpToDate(string cliFileDestinationPath, string expectedSha)
        {
            if (string.IsNullOrEmpty(expectedSha) || !File.Exists(cliFileDestinationPath))
            {
                return false;
            }

            try
            {
                return string.Equals(Sha256.Checksum(cliFileDestinationPath), expectedSha, StringComparison.OrdinalIgnoreCase);
            }
            catch (IOException e)
            {
                // Locked by the LS mid-scan: treat as "not verifiable" and let the normal download path
                // run, which reports a proper error if it also cannot get at the file.
                Logger.Warning(e, "Could not checksum the CLI at {Path}", cliFileDestinationPath);
                return false;
            }
        }

        public async Task DownloadAsync(
            ISnykProgressWorker progressWorker,
            string cliFileDestinationPath,
            string cliDownloadUrl,
            List<CliDownloadFinishedCallback> downloadFinishedCallbacks = null)
        {
            Logger.Information("Enter AsynchronousDownload method");

            try
            {
                this.SaveLatestCliSha(cliDownloadUrl);

                if (IsCliUpToDate(cliFileDestinationPath, this.expectedSha))
                {
                    Logger.Information(
                        "CLI at {Path} already matches the expected checksum — skipping download",
                        cliFileDestinationPath);

                    this.FinishDownload(progressWorker, downloadFinishedCallbacks);

                    return;
                }

                await this.DownloadFileAsync(progressWorker, cliDownloadUrl, cliFileDestinationPath, downloadFinishedCallbacks);
            }
            catch (ChecksumVerificationException e)
            {
                Logger.Error(e, "Error on cli file download");

                await this.DownloadFileAsync(progressWorker, cliDownloadUrl, cliFileDestinationPath, downloadFinishedCallbacks);
            }
        }

        private async Task DownloadFileAsync(
            ISnykProgressWorker progressWorker,
            string cliDownloadUrl,
            string cliFileDestinationPath,
            List<CliDownloadFinishedCallback> downloadFinishedCallbacks = null)
        {
            const int bufferSize = 81920;

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                var response = await client.GetAsync(cliDownloadUrl, HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode();

                var tempCliFile = Path.GetTempFileName();

                using (var fileStream = new FileStream(tempCliFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, bufferSize, true))
                {
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        var totalBytes = response.Content.Headers.ContentLength ?? long.MaxValue; // Avoid dividing by null when calculating progress
                        var totalRead = 0L;
                        var buffer = new byte[bufferSize];
                        var isMoreToRead = true;
                        var lastProgressPercentage = 0;

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

                                if (percentage > lastProgressPercentage)
                                {
                                    progressWorker.UpdateProgress(percentage);
                                    lastProgressPercentage = percentage;
                                }

                                progressWorker.CancelIfCancellationRequested();
                            }
                        }
                        while (isMoreToRead);
                    }
                }

                this.VerifyCliFile(tempCliFile);

                try
                {
                    // The destination folder is the configured one, which may not exist yet (snyk-ls
                    // reports its own CLI location as cli_path). Create it before copying, or the copy
                    // fails with DirectoryNotFoundException and reads as a file-in-use error.
                    PrepareCliDirectory(cliFileDestinationPath);

                    // Overwrite in place rather than Delete-then-Copy: a failed copy (e.g. the LS is
                    // running the binary) then leaves the existing, working CLI intact instead of
                    // having already deleted it.
                    File.Copy(tempCliFile, cliFileDestinationPath, overwrite: true);

                    this.FinishDownload(progressWorker, downloadFinishedCallbacks);
                }
                catch (Exception e)
                {
                    // Null-conditional: the download runs early in startup and NotificationService is
                    // initialised by the package, so a null here would throw an NRE from inside this
                    // catch and mask the real failure.
                    NotificationService.Instance?.ShowErrorInfoBar(
                        $"CLI could not be updated at {cliFileDestinationPath}: {e.Message} " +
                        "If the file is in use, close any running Snyk scans and try again.");
                    Logger.Error(e, "Error on CLI copy from temp file to {Path}", cliFileDestinationPath);

                    // Rethrow: swallowing this left the extension in a dead state. FinishDownload never
                    // runs, so SnykTasksService fires neither DownloadFinished (which is what starts the
                    // language server) nor DownloadFailed — the tool window stays in its initializing
                    // state forever. Propagating reaches SnykTasksService.DownloadAsync's handler, which
                    // raises DownloadFailed; that handler starts the server anyway when a usable CLI is
                    // already present at the configured path, and otherwise tells the user the download
                    // failed and they can set a CLI path in settings.
                    throw;
                }
                finally
                {
                    File.Delete(tempCliFile);
                }
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
    }
}
