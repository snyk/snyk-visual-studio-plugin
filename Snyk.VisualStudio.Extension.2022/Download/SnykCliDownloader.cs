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

        // Where a URL's authority ends: the start of the path, query or fragment.
        private static readonly char[] AuthorityDelimiters = { '/', '\\', '?', '#' };

        private readonly ISnykOptions SnykOptions;
        private string expectedSha;
        private LatestReleaseInfo cachedLatestReleaseInfo;

        public SnykCliDownloader(ISnykOptions snykOptions)
        {
            this.SnykOptions = snykOptions;
        }

        /// <summary>
        /// Callback on download finished event.
        /// </summary>
        public delegate void CliDownloadFinishedCallback();

        /// <summary>
        /// The configured base URL, or the default when unset. Empty must not pass through: it composes
        /// into a relative URL, which WebClient resolves to a local file path instead of a request.
        /// </summary>
        public static string ResolveBaseDownloadUrl(string configuredBaseDownloadUrl) =>
            string.IsNullOrWhiteSpace(configuredBaseDownloadUrl)
                ? DefaultBaseDownloadUrl
                : configuredBaseDownloadUrl.Trim();

        // Blanks credentials in a URL before it is logged.
        //
        // Scans the raw string rather than going through Uri: Uri.UserInfo returns the value
        // *unescaped*, so searching the original for it misses "us%65r:token@host" and returns
        // the URL untouched — a silent leak. Uri also parses a Windows path as scheme "file",
        // so anything keyed off the scheme mangles "C:\tools\snyk@2\cli.exe".
        internal static string Redact(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            // The authority follows the scheme separator, then any number of slashes. Backslashes
            // and a scheme-less "//host" count: this is a value typed into a settings box, and a
            // credential must not survive on the strength of a typo.
            var authorityStart = 0;
            var schemeEnd = value.IndexOf(':');

            if (schemeEnd >= 0 && schemeEnd + 1 < value.Length && IsSlash(value[schemeEnd + 1]))
            {
                // A one-character scheme is a Windows drive letter, and "C:\" introduces a path,
                // not an authority: otherwise the first path segment is read as the authority and
                // "C:\snyk@2\cli.exe" comes out as "C:\<credentials>@2\cli.exe". A second slash
                // means it really is a scheme, since no local path starts "C://".
                var isDriveLetter = schemeEnd < 2
                    && !(schemeEnd + 2 < value.Length && IsSlash(value[schemeEnd + 2]));

                if (!isDriveLetter)
                {
                    authorityStart = schemeEnd + 1;
                }
            }

            while (authorityStart < value.Length && IsSlash(value[authorityStart]))
            {
                authorityStart++;
            }

            if (authorityStart >= value.Length)
            {
                return value;
            }

            // Only the authority can hold credentials. An '@' past its end belongs to the path
            // or query ("host/path@v2"), or to a local path ("C:\tools\snyk@2").
            var authorityEnd = value.IndexOfAny(AuthorityDelimiters, authorityStart);

            if (authorityEnd < 0)
            {
                authorityEnd = value.Length;
            }

            if (authorityEnd <= authorityStart)
            {
                return value;
            }

            // Last '@', so a credential that itself contains one is covered. A scheme-less
            // "user:pass@host" is caught too: authorityStart is 0 and the whole value is authority.
            var credentialsEnd = value.LastIndexOf('@', authorityEnd - 1, authorityEnd - authorityStart);

            return credentialsEnd < 0
                ? value
                : value.Substring(0, authorityStart) + "<credentials>" + value.Substring(credentialsEnd);
        }

        private static bool IsSlash(char c) => c == '/' || c == '\\';

        /// <summary>
        /// The configured release channel, or the default when unset.
        /// </summary>
        public static string ResolveReleaseChannel(string configuredReleaseChannel) =>
            string.IsNullOrWhiteSpace(configuredReleaseChannel)
                ? DefaultReleaseChannel
                : configuredReleaseChannel.Trim();

        // internal for testability.
        internal string BuildLatestReleaseVersionUrl() => string.Format(
            LatestReleaseVersionUrlScheme,
            ResolveBaseDownloadUrl(SnykOptions.CliBaseDownloadURL),
            ResolveReleaseChannel(SnykOptions.CliReleaseChannel));

        internal string BuildCliDownloadUrl(string version) => string.Format(
            LatestReleaseDownloadUrlScheme,
            ResolveBaseDownloadUrl(SnykOptions.CliBaseDownloadURL),
            version);

        /// <summary>
        /// The latest release, fetched once per downloader instance. A single update otherwise asks
        /// three times — the version probe, the log line in DownloadAsync, and the finished callback
        /// that persists CurrentCliVersion — and those answers need not agree: a release published
        /// mid-update meant installing one version and recording the next, after which the version
        /// comparison reports "current" and the install never moves off the older binary.
        /// </summary>
        // internal for testability (the tests assert the fetch happens once).
        internal LatestReleaseInfo GetLatestReleaseInfoOnce() =>
            this.cachedLatestReleaseInfo ?? (this.cachedLatestReleaseInfo = this.GetLatestReleaseInfo());

        /// <summary>
        /// Request last cli information.
        /// </summary>
        /// <returns>Latest CLI relaese information.</returns>
        // virtual for testability: a test double replaces the network call. Prefer
        // GetLatestReleaseInfoOnce internally — this always issues a request.
        public virtual LatestReleaseInfo GetLatestReleaseInfo()
        {
            Logger.Information("Enter GetLatestReleaseInfo method");

            using (var webClient = new SnykWebClient())
            {
                var latestReleaseVersionUrl = this.BuildLatestReleaseVersionUrl();

                // The composed URL and its inputs: a misconfigured value otherwise surfaces only as an
                // exception naming a path that appears nowhere in the settings.
                Logger.Information(
                    "Get latest CLI release info from {Url} (configured base url: '{BaseDownloadUrl}', release channel: '{ReleaseChannel}')",
                    Redact(latestReleaseVersionUrl),
                    Redact(this.SnykOptions.CliBaseDownloadURL),
                    // A no-op for every legitimate channel ("stable", "rc", "v1.1292.0" hold no '@'),
                    // but it is free text that composes into the URL above, so treat it like its
                    // siblings rather than making this the one argument that can carry a secret.
                    Redact(this.SnykOptions.CliReleaseChannel));

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
        // virtual for testability: a test double replaces the network call.
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
                if (!this.IsCliFileExists(cliFileDestinationPath) || this.IsNewVersionAvailable(this.SnykOptions.CurrentCliVersion, this.GetLatestReleaseInfoOnce().Name))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                // A failed check is not "up to date": with no CLI on disk the language server would be
                // started with nothing to run. Fall back to whether one exists at all.
                var cliFileExists = this.IsCliFileExists(cliFileDestinationPath);

                Logger.Error(ex,
                    "Could not fetch latest CLI release info, so whether a newer version exists is unknown. Falling back to whether a CLI is present at {Path}: {CliFileExists}",
                    cliFileDestinationPath,
                    cliFileExists);

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

            LatestReleaseInfo latestReleaseInfo = this.GetLatestReleaseInfoOnce();

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
        /// Create the folder of the configured destination, which may not exist: a first install has
        /// no app-data directory yet, and a user-typed custom path may name a folder that is not there.
        /// </summary>
        // internal for testability.
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
        /// Put the downloaded binary at its destination, creating the folder if needed. Replaces an
        /// existing CLI in one step, so an interrupted install cannot leave a truncated binary behind.
        /// </summary>
        // internal for testability.
        internal static void InstallCliFile(string sourceFilePath, string cliFileDestinationPath)
        {
            PrepareCliDirectory(cliFileDestinationPath);

            if (!File.Exists(cliFileDestinationPath))
            {
                // Nothing to preserve, and File.Replace requires the destination to exist.
                File.Copy(sourceFilePath, cliFileDestinationPath, overwrite: true);
                return;
            }

            // Stage beside the destination so the swap stays on one volume, then replace. File.Copy
            // straight onto the destination truncates it and streams in place, so a crash, a full
            // disk or an AV lock mid-write leaves neither the old nor the new binary — and callers
            // only check File.Exists, so a corrupt file reads as a usable CLI.
            var stagingPath = cliFileDestinationPath + "." + Guid.NewGuid().ToString("N").Substring(0, 8) + ".new";

            try
            {
                File.Copy(sourceFilePath, stagingPath, overwrite: true);

                try
                {
                    File.Replace(stagingPath, cliFileDestinationPath, destinationBackupFileName: null);
                }
                catch (Exception e) when (e is IOException || e is PlatformNotSupportedException || e is UnauthorizedAccessException)
                {
                    // File.Replace needs a volume that supports it; a custom CLI path can name an SMB
                    // share or a FAT volume. Failing the install outright would be worse than a
                    // non-atomic overwrite, so fall back — but say so, because the guarantee is gone.
                    Logger.Warning(e, "Atomic replace unavailable at {Path}; overwriting in place", cliFileDestinationPath);
                    File.Copy(sourceFilePath, cliFileDestinationPath, overwrite: true);
                }
            }
            finally
            {
                TryDeleteFile(stagingPath);
            }
        }

        // Cleanup must never mask the failure that triggered it, so this swallows and logs.
        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Logger.Warning(e, "Could not remove the temporary file {Path}", path);
            }
        }

        /// <summary>
        /// Install the binary and fire the finished callbacks. Must rethrow on failure: the callbacks
        /// are what start the language server, and the caller turns the exception into DownloadFailed.
        /// </summary>
        // internal for testability.
        internal void InstallAndFinish(
            ISnykProgressWorker progressWorker,
            string sourceFilePath,
            string cliFileDestinationPath,
            List<CliDownloadFinishedCallback> downloadFinishedCallbacks)
        {
            try
            {
                InstallCliFile(sourceFilePath, cliFileDestinationPath);
            }
            catch (Exception e)
            {
                this.ReportInstallFailure(e, cliFileDestinationPath);

                throw;
            }

            // Outside the catch above: it diagnoses the file copy, and the callbacks are what start
            // the language server. A callback failing is not the CLI failing to install, and must not
            // be reported as one. It still propagates; the caller has a single failure channel, so it
            // surfaces DownloadFailed either way — but the install-specific diagnosis stays accurate.
            this.FinishDownload(progressWorker, downloadFinishedCallbacks);
        }

        // virtual for testability: the notification sink is a static singleton, so a test cannot
        // otherwise observe whether a failure was diagnosed as an install failure.
        internal virtual void ReportInstallFailure(Exception e, string cliFileDestinationPath)
        {
            // An update failure leaves a working CLI behind; an install failure does not.
            var existingCliRemains = File.Exists(cliFileDestinationPath);
            var message = existingCliRemains
                ? $"Snyk CLI could not be updated at {cliFileDestinationPath}: {e.Message} The existing CLI will continue to be used."
                : $"Snyk CLI could not be installed at {cliFileDestinationPath}: {e.Message}";

            // Null-conditional: the download can run before the package initialises this.
            NotificationService.Instance?.ShowErrorInfoBar(message);
            Logger.Error(e, "Error on CLI copy from temp file to {Path}", cliFileDestinationPath);
        }

        /// <summary>
        /// True when the binary already there is the one we would install. The language server downloads
        /// the same CLI to the same path and runs it, so overwriting it risks a sharing violation.
        /// </summary>
        // internal for testability.
        internal static bool IsCliUpToDate(string cliFileDestinationPath, string expectedSha) =>
            IsCliUpToDate(cliFileDestinationPath, expectedSha, Sha256.Checksum);

        // computeChecksum is injected so a test can reach the catch below.
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
                // Any failure means "not verifiable", so fall through to the download. Catching only
                // IOException would miss UnauthorizedAccessException and, under a FIPS policy,
                // InvalidOperationException from SHA256Managed.
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

            // The work below is synchronous HTTP plus a SHA-256 of a ~150 MB file; keep it off the UI
            // thread regardless of caller. Idempotent if the caller already switched.
            await TaskScheduler.Default;

            try
            {
                this.SaveLatestCliSha(cliDownloadUrl);

                if (IsCliUpToDate(cliFileDestinationPath, this.expectedSha))
                {
                    Logger.Information(
                        "CLI at {Path} already matches the expected checksum — skipping download",
                        cliFileDestinationPath);

                    // Without this the progress bar jumps from 0 straight to finished.
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

                // Covers the download and the checksum as well as the install. VerifyCliFile throwing
                // is an expected path — DownloadAsync catches ChecksumVerificationException and
                // retries — so leaving it outside stranded a ~175MB temp file on every attempt, as did
                // a cancelled or failed download.
                try
                {
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

                    this.InstallAndFinish(progressWorker, tempCliFile, cliFileDestinationPath, downloadFinishedCallbacks);
                }
                finally
                {
                    TryDeleteFile(tempCliFile);
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
