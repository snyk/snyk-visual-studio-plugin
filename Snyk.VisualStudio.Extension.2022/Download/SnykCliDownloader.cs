using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Serilog;
using Snyk.VisualStudio.Extension.CLI;
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
        /// The base URL to download the CLI from: the configured value, or the default when it is unset.
        /// <para>
        /// An empty value composed into a RELATIVE url, which <see cref="System.Net.WebClient"/> resolves
        /// through Path.GetFullPath into a nonexistent local file path and reports as a
        /// DirectoryNotFoundException — the defect this fixes. The Language Server registers
        /// binary_base_url with an empty default and echoes every machine-scope setting back in
        /// $/snyk.configuration, so empty reaches the options routinely.
        /// </para>
        /// <para>
        /// Trim-then-default-if-empty, and otherwise use the value verbatim, is what every other Snyk
        /// IDE does: snyk-ls (applyCliBaseDownloadURL), Eclipse (LsBinaries.resolveBaseUrl) and VS Code
        /// (Configuration.getCliBaseDownloadUrl). A scheme-less host is not supported anywhere in the
        /// family — the settings field's placeholder shows a full URL — so it is not accepted here
        /// either; doing so would make the same input behave differently depending on the IDE.
        /// </para>
        /// <para>
        /// Two rough edges are deliberately NOT handled here, because they are shared with every other
        /// IDE and fixing them in one of them only would recreate that divergence:
        /// </para>
        /// <list type="bullet">
        /// <item>A trailing slash composes into "host//cli/...", which CDN-backed origins serve as a
        /// different key. This has a non-user trigger: snyk-ls ships "https://downloads.snyk.io/" as the
        /// section default in its settings form's reset handler.</item>
        /// <item>A query or fragment swallows the composed path, leaving the request pointed at the host
        /// root.</item>
        /// </list>
        /// <para>
        /// Both belong upstream in snyk-ls. Note also that Visual Studio is the only family member whose
        /// failure mode here is a filesystem read — WebClient resolves a relative URL through
        /// Path.GetFullPath, whereas Go, VS Code and Eclipse all fail loudly on a missing scheme. That
        /// asymmetry is why the empty case is additionally guarded on the inbound, outbound and load
        /// paths rather than at the point of use alone.
        /// </para>
        /// </summary>
        public static string ResolveBaseDownloadUrl(string configuredBaseDownloadUrl) =>
            string.IsNullOrWhiteSpace(configuredBaseDownloadUrl)
                ? DefaultBaseDownloadUrl
                : configuredBaseDownloadUrl.Trim();

        // Blanks the credentials in a URL before it reaches the log. A mirror configured as
        // https://user:token@host would otherwise write the token to snyk-extension.log. Applied to
        // every logged URL, not just the configured value: the composed download URL carries the same
        // credentials.
        internal static string Redact(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf('@') < 0)
            {
                return value;
            }

            var parsed = Uri.TryCreate(value, UriKind.Absolute, out var uri);

            if (parsed && !string.IsNullOrEmpty(uri.UserInfo))
            {
                return value.Replace(uri.UserInfo + "@", "<credentials>@");
            }

            // An '@' inside a well-formed web URL that has no userinfo belongs to the path or query
            // ("https://host/path@v2"), so there is nothing to blank.
            if (parsed && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return value;
            }

            // Everything else carrying '@' is treated as credentials. Uri.UserInfo cannot be relied on
            // here: "user:pass@host" parses as an absolute URI with scheme "user" and NO userinfo, so a
            // check for a populated UserInfo silently passes the secret straight through to the log.
            return "<credentials>@" + value.Substring(value.LastIndexOf('@') + 1);
        }

        /// <summary>
        /// Resolve the CLI release channel, treating an unset or cleared value as the default.
        /// Same reasoning as <see cref="ResolveBaseDownloadUrl"/>.
        /// </summary>
        public static string ResolveReleaseChannel(string configuredReleaseChannel) =>
            string.IsNullOrWhiteSpace(configuredReleaseChannel)
                ? DefaultReleaseChannel
                : configuredReleaseChannel.Trim();

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
        // virtual for testability (InternalsVisibleTo test project): a test double stands in for the
        // release-info request so the download-decision paths can be exercised without network access.
        public virtual LatestReleaseInfo GetLatestReleaseInfo()
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
                    Redact(latestReleaseVersionUrl),
                    Redact(this.SnykOptions.CliBaseDownloadURL ?? string.Empty),
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
        // virtual for testability (InternalsVisibleTo test project): a test double returns a known sha so
        // the up-to-date short-circuit in DownloadAsync can be exercised without network access.
        public virtual string GetLatestCliSha(string cliDownloadUrl)
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
                // Do NOT report "up to date" when the check itself failed: with no CLI on disk that
                // silently skips the download and starts the language server with nothing to run.
                // Fall back to the one thing still knowable locally — whether a CLI exists at all.
                var cliFileExists = this.IsCliFileExists(cliFileDestinationPath);

                Logger.Error(ex,
                    "Could not fetch latest CLI release info. CLI present at {Path}: {CliFileExists}; download needed: {DownloadNeeded}",
                    cliFileDestinationPath,
                    cliFileExists,
                    !cliFileExists);

                return !cliFileExists;
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

            Logger.Information("Latest relase information: version {Version} and url {Url}", latestReleaseInfo.Version, Redact(latestReleaseInfo.Url));

            progressWorker.CancelIfCancellationRequested();

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
        /// Put the downloaded binary at its destination, creating the destination folder if needed.
        /// Throws on failure — the caller reports it and rethrows.
        /// </summary>
        // internal static for testability (InternalsVisibleTo test project): lets the install step be
        // exercised without downloading a CLI first.
        internal static void InstallCliFile(string sourceFilePath, string cliFileDestinationPath)
        {
            // The destination folder is the configured one, which may not exist yet (snyk-ls reports its
            // own CLI location as cli_path). Create it before copying, or the copy fails with
            // DirectoryNotFoundException and reads as a file-in-use error.
            PrepareCliDirectory(cliFileDestinationPath);

            // Overwrite in place rather than Delete-then-Copy: a failed copy (e.g. the language server is
            // running the binary) then leaves the existing, working CLI intact instead of having already
            // deleted it.
            File.Copy(sourceFilePath, cliFileDestinationPath, overwrite: true);
        }

        /// <summary>
        /// Install the downloaded binary and fire the finished callbacks, reporting and rethrowing on
        /// failure. Rethrow is the contract that matters: swallowing it left the extension dead, because
        /// FinishDownload never ran and SnykTasksService then fired neither DownloadFinished (which is
        /// what starts the language server) nor DownloadFailed, so the tool window stayed in its
        /// initializing state with no error and no recovery. Propagating reaches
        /// SnykTasksService.DownloadAsync's handler, which raises DownloadFailed; that handler starts the
        /// server anyway when a usable CLI is already present, and otherwise tells the user the download
        /// failed and they can set a CLI path in settings.
        /// </summary>
        // internal for testability (InternalsVisibleTo test project): lets the rethrow contract be
        // exercised without first downloading a CLI.
        internal void InstallAndFinish(
            ISnykProgressWorker progressWorker,
            string sourceFilePath,
            string cliFileDestinationPath,
            List<CliDownloadFinishedCallback> downloadFinishedCallbacks)
        {
            try
            {
                InstallCliFile(sourceFilePath, cliFileDestinationPath);

                this.FinishDownload(progressWorker, downloadFinishedCallbacks);
            }
            catch (Exception e)
            {
                // Message the two outcomes differently. SnykToolWindowControl.OnDownloadFailed restarts
                // the language server when a CLI is already present at the destination, so telling the
                // user to close their scans while we start one would contradict itself.
                var existingCliRemains = File.Exists(cliFileDestinationPath);
                var message = existingCliRemains
                    ? $"Snyk CLI could not be updated at {cliFileDestinationPath}: {e.Message} The existing CLI will continue to be used."
                    : $"Snyk CLI could not be installed at {cliFileDestinationPath}: {e.Message}";

                // Null-conditional: the download runs early in startup and NotificationService is
                // initialised by the package, so a null here would throw an NRE from inside this catch
                // and mask the real failure.
                NotificationService.Instance?.ShowErrorInfoBar(message);
                Logger.Error(e, "Error on CLI copy from temp file to {Path}", cliFileDestinationPath);

                throw;
            }
        }

        /// <summary>
        /// True when the binary already at <paramref name="cliFileDestinationPath"/> is the one we are
        /// about to install. snyk-ls downloads the same CLI to the same path and runs it, so overwriting
        /// an identical binary achieves nothing and can fail with a sharing violation.
        /// </summary>
        // internal static for testability (InternalsVisibleTo test project).
        internal static bool IsCliUpToDate(string cliFileDestinationPath, string expectedSha) =>
            IsCliUpToDate(cliFileDestinationPath, expectedSha, Sha256.Checksum);

        // computeChecksum is injected so a test can reach the catch below deterministically: the
        // failures it exists for (an ACL-denied destination, SHA256Managed under a FIPS policy) cannot
        // be provoked portably from a test that only has a file path.
        internal static bool IsCliUpToDate(string cliFileDestinationPath, string expectedSha, Func<string, string> computeChecksum)
        {
            if (string.IsNullOrEmpty(expectedSha) || !File.Exists(cliFileDestinationPath))
            {
                return false;
            }

            try
            {
                return string.Equals(computeChecksum(cliFileDestinationPath), expectedSha, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception e)
            {
                // Every failure here means the same thing — "not verifiable" — so the answer is always
                // false and the normal download path runs, reporting a proper error if it also cannot
                // get at the file. Catching only IOException would let UnauthorizedAccessException (an
                // ACL-denied destination) and InvalidOperationException (SHA256Managed under a
                // FIPS-enforcing policy) escape and abort the download before it is even attempted.
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

            // Own the thread guarantee rather than relying on the caller: this method is public, and the
            // work below (two synchronous release-info requests, a SHA-256 of a ~150 MB binary) must
            // never run on the UI thread. Idempotent when the caller has already switched.
            await TaskScheduler.Default;

            try
            {
                this.SaveLatestCliSha(cliDownloadUrl);

                // SnykTasksService.DownloadAsync switches to the thread pool before any of this runs, so
                // the hash, the two synchronous release-info requests before it, and the settings write
                // in the finished callback are all off the UI thread.
                if (IsCliUpToDate(cliFileDestinationPath, this.expectedSha))
                {
                    Logger.Information(
                        "CLI at {Path} already matches the expected checksum — skipping download",
                        cliFileDestinationPath);

                    // The skip is the common path once the language server has already fetched the
                    // binary; without this the progress bar jumps from 0 straight to finished.
                    progressWorker.UpdateProgress(100);

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
                    this.InstallAndFinish(progressWorker, tempCliFile, cliFileDestinationPath, downloadFinishedCallbacks);
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
