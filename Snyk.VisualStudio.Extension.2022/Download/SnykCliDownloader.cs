using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
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

        // Stands in for any user-supplied download URL. See DescribeUrlForLog.
        private const string CustomUrlNotLogged = "<custom URL, not logged>";

        private const string LatestReleaseVersionUrlScheme = "{0}/cli/{1}/ls-protocol-version-" + LsConstants.ProtocolVersion;
        private const string LatestReleaseDownloadUrlScheme = "{0}/cli/{1}/" + SnykCli.CliFileName;
        private const string Sha256DownloadUrl = "{0}.sha256";

        // What a locally-built language server reports instead of a number.
        private const string DevelopmentProtocolVersion = "development";

        // The probe launches the CLI, which on Windows can be slowed by on-access AV scanning. Bounded
        // because an unbounded WaitForExit is the hang this check exists to detect.
        private const int ProtocolProbeTimeoutMs = 20000;

        private static readonly ILogger Logger = LogManager.ForContext<SnykCliDownloader>();

        private readonly ISnykOptions SnykOptions;

        // Guards the three memos below. One downloader is shared across a startup's concurrent callers
        // (package init, the language client's load, the update itself), and an unguarded
        // "field ?? (field = Fetch())" lets two threads both see null and both fetch — costing repeated
        // release-version requests, repeated checksum requests and repeated hashes of the binary.
        //
        // Held across the fetch deliberately: the second caller waits and then reads the memo, which is
        // the point.
        //
        // The wait is NOT fully bounded, so callers must be off the UI thread. SnykWebClient's timeout
        // bounds the two requests; nothing bounds the checksum of a ~175MB binary, which is the longer
        // leg and worse on a network CliCustomPath. Both callers of ShouldDownloadCli hop to the pool
        // first — SnykVSPackage.InitializeLanguageClient and SnykLanguageClient.OnLoadedAsync — so a
        // new call site needs the same hop rather than relying on the thread VS happens to supply.
        private readonly object memoLock = new object();

        private string expectedSha;
        private string expectedShaUrl;
        private LatestReleaseInfo cachedLatestReleaseInfo;
        private string upToDateMemoKey;
        private bool upToDateMemoVerdict;

        public SnykCliDownloader(ISnykOptions snykOptions)
        {
            this.SnykOptions = snykOptions;
        }

        /// <summary>
        /// Callback on download finished event.
        /// </summary>
        public delegate void CliDownloadFinishedCallback();

        /// <summary>
        /// The configured base URL, or the default when unset or not an http(s) origin. Empty must not
        /// pass through: it composes into a relative URL, which WebClient resolves to a local file path
        /// instead of a request. Trailing slashes are dropped because the URL schemes below add their
        /// own, and Uri does not collapse "//" inside a path — the LS-served settings page resets this
        /// field to a value with a trailing slash, which otherwise fetches ".../cli//stable/...".
        /// </summary>
        public static string ResolveBaseDownloadUrl(string configuredBaseDownloadUrl)
        {
            // Normalise BEFORE the blank check, not after: trimming can itself produce a blank ("/"),
            // and guarding first let that through to compose the relative URL this method exists to
            // prevent. Backslashes too, since a user may type either separator.
            var normalised = configuredBaseDownloadUrl?.Trim().TrimEnd('/', '\\');

            // A scheme with no authority ("https://" -> "https:") is unusable for the same reason.
            if (string.IsNullOrWhiteSpace(normalised) || normalised.EndsWith(":", StringComparison.Ordinal))
            {
                return DefaultBaseDownloadUrl;
            }

            // http(s) only, as CustomEndpoint already is. WebClient will happily take a UNC path or a
            // file: URL, and a UNC target makes the version and checksum fetches perform an implicit
            // SMB authentication against a host of the setter's choosing — which hands that host the
            // Windows user's NTLM credentials. Nothing needs a non-web origin here, and both writers
            // of this field (the settings page and an LS config push) apply only a blank guard, so the
            // check belongs at the point of use where every consumer passes through it.
            if (!UriExtensions.IsValidWebUrl(normalised))
            {
                // The value itself is deliberately absent: it is user-supplied and failed validation,
                // so it is exactly the case where it must not reach the log.
                Logger.Warning(
                    "Ignoring the configured CLI base download URL: only http and https origins are used. Falling back to {Default}",
                    DefaultBaseDownloadUrl);

                return DefaultBaseDownloadUrl;
            }

            return normalised;
        }

        /// <summary>
        /// A download URL in a form that is safe to log: in full on Snyk's own host, otherwise replaced
        /// by a constant. A user-supplied URL can carry a credential in the userinfo, a query string or
        /// a signed parameter, and withholding the value cannot leak it whereas locating the secret has
        /// to be correct over an open-ended set of shapes.
        /// </summary>
        internal static string DescribeUrlForLog(string url)
        {
            var trimmed = url?.TrimStart();

            // Equal to the default, or the default followed by a path separator. A bare StartsWith is
            // not enough: "https://downloads.snyk.io.attacker.test/cli" starts with our host string but
            // is somebody else's host, and would have been logged in full.
            var onDefaultHost = !string.IsNullOrWhiteSpace(trimmed)
                && (string.Equals(trimmed, DefaultBaseDownloadUrl, StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith(DefaultBaseDownloadUrl + "/", StringComparison.OrdinalIgnoreCase));

            return onDefaultHost ? url : CustomUrlNotLogged;
        }

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
        internal LatestReleaseInfo GetLatestReleaseInfoOnce()
        {
            lock (this.memoLock)
            {
                return this.cachedLatestReleaseInfo ??
                       (this.cachedLatestReleaseInfo = this.GetLatestReleaseInfo());
            }
        }

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

                // The channel is included because it selects a version rather than carrying a credential,
                // and a wrong one is a common misconfiguration.
                Logger.Information(
                    "Get latest CLI release info from {Url} (release channel: '{ReleaseChannel}')",
                    DescribeUrlForLog(latestReleaseVersionUrl),
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
        /// The published checksum for a download URL, fetched once per downloader instance. Memoised for
        /// the same reason as the release info: it is now consulted by the download decision as well as
        /// by verification, and a second fetch could disagree with the first if a release lands between
        /// them. Also sets expectedSha, which VerifyCliFile compares against.
        ///
        /// Keyed on the URL, not just on "have we fetched yet": every production caller passes the URL
        /// from the single cached release info, but the public DownloadAsync overload takes an arbitrary
        /// one, and returning the first URL's checksum for a second URL would fail verification against
        /// a binary that was in fact intact.
        /// </summary>
        // internal for testability (the tests assert the fetch happens once).
        internal string GetLatestCliShaOnce(string cliDownloadUrl)
        {
            lock (this.memoLock)
            {
                if (this.expectedSha != null
                    && string.Equals(this.expectedShaUrl, cliDownloadUrl, StringComparison.Ordinal))
                {
                    return this.expectedSha;
                }

                this.expectedSha = this.GetLatestCliSha(cliDownloadUrl);
                this.expectedShaUrl = cliDownloadUrl;

                return this.expectedSha;
            }
        }

        /// <summary>
        /// Whether the binary at <paramref name="cliFilePath"/> speaks the LS protocol version this
        /// extension implements. The only way to establish that for a binary we did not just download:
        /// a build fetched from the protocol-keyed release URL satisfies it by construction, but one
        /// already on disk carries no such guarantee, and neither its file name nor its checksum says
        /// anything about it.
        ///
        /// Costs a process launch, so this is deliberately reached only where the answer is not already
        /// known — the download-failure fallback and a failed release lookup — never on the path where
        /// the checksum has already proven the binary is the current release.
        /// </summary>
        // internal virtual for testability: a test double avoids launching a real process.
        internal virtual bool IsCliProtocolSupported(string cliFilePath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = cliFilePath,
                    Arguments = "language-server --protocolVersion",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        Logger.Warning("Could not start the CLI at {Path} to read its protocol version", cliFilePath);

                        return false;
                    }

                    // WaitForExit before reading: ReadToEnd on a process that never writes would block
                    // with no timeout, which is the hang this check exists to prevent.
                    //
                    // A binary that writes more than the pipe buffer holds — a legacy CLI printing help
                    // text because it does not recognise the arguments — blocks on that write and so
                    // never exits, which this wait then reports as a timeout. That costs the full
                    // timeout, but the verdict is still the right one and ReadToEnd below is never
                    // reached, so it cannot deadlock. It is not worth an async read to make an
                    // unusable binary unusable faster.
                    if (!process.WaitForExit(ProtocolProbeTimeoutMs))
                    {
                        Logger.Warning(
                            "CLI at {Path} did not report a protocol version within {TimeoutMs}ms",
                            cliFilePath,
                            ProtocolProbeTimeoutMs);

                        try
                        {
                            process.Kill();
                        }
                        catch (Exception e) when (e is InvalidOperationException || e is System.ComponentModel.Win32Exception)
                        {
                            // Already gone, or not ours to kill.
                        }

                        return false;
                    }

                    var reported = process.StandardOutput.ReadToEnd().Trim();

                    // "development" is what a locally-built language server reports; treat it as
                    // compatible so a dev CLI is usable, matching snyk-ls' own handling.
                    var supported = string.Equals(reported, LsConstants.ProtocolVersion, StringComparison.Ordinal)
                        || string.Equals(reported, DevelopmentProtocolVersion, StringComparison.Ordinal);

                    if (!supported)
                    {
                        Logger.Warning(
                            "CLI at {Path} reports protocol version '{Reported}', expected '{Expected}'",
                            cliFilePath,
                            reported,
                            LsConstants.ProtocolVersion);
                    }

                    return supported;
                }
            }
            catch (Exception e)
            {
                // A binary that cannot be launched at all — wrong architecture, missing execute
                // permission, quarantined by AV — lands here, which is the answer we want: unusable.
                Logger.Warning(e, "Could not read the protocol version from the CLI at {Path}", cliFilePath);

                return false;
            }
        }

        /// <summary>
        /// Request last cli sha.
        /// </summary>
        /// <returns>CLI sha string.</returns>
        // virtual for testability: a test double replaces the network call. Prefer GetLatestCliShaOnce
        // internally — this always issues a request.
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
        /// Whether the binary we want is not the binary we have.
        ///
        /// Decided on the bytes, not on a recorded version string. CurrentCliVersion says what the last
        /// install believed it wrote; it cannot see a truncated file, a binary swapped out from under us
        /// by another tool sharing the path, or settings copied from another machine. In every one of
        /// those the recorded version matched the latest release and the update was skipped, leaving the
        /// language server to launch a binary that could not work — with no way out, because the next
        /// startup repeated the same comparison.
        ///
        /// Two requests, both memoised for the life of this instance: the protocol-keyed release version
        /// and its checksum.
        /// </summary>
        /// <param name="cliFileDestinationPath">Path to CLI file.</param>
        /// <returns>True when the current release should be downloaded.</returns>
        public bool IsCliDownloadNeeded(string cliFileDestinationPath = null)
        {
            // Every branch below logs the path it decided on. Without that, a wrong decision is
            // indistinguishable from a broken download: the log showed neither where we looked nor
            // why we concluded an update was needed.
            if (!this.IsCliFileExists(cliFileDestinationPath))
            {
                Logger.Information("CLI download needed: no file at {Path}", cliFileDestinationPath);

                return true;
            }

            try
            {
                var latestReleaseInfo = this.GetLatestReleaseInfoOnce();
                var expectedSha = this.GetLatestCliShaOnce(latestReleaseInfo.Url);

                if (this.IsCliUpToDateOnce(cliFileDestinationPath, expectedSha))
                {
                    // Nothing further to check: this URL is keyed on the protocol version, so a binary
                    // whose bytes match what it serves satisfies that protocol by construction. That is
                    // why the happy path never launches the CLI to ask.
                    Logger.Information(
                        "No CLI download needed: {Path} matches the checksum of {LatestVersion}, the latest release for protocol version {ProtocolVersion}",
                        cliFileDestinationPath,
                        latestReleaseInfo.Name,
                        LsConstants.ProtocolVersion);

                    return false;
                }

                Logger.Information(
                    "CLI download needed: {Path} does not match the checksum of {LatestVersion} (recorded locally as {CurrentVersion})",
                    cliFileDestinationPath,
                    latestReleaseInfo.Name,
                    string.IsNullOrEmpty(this.SnykOptions.CurrentCliVersion) ? "(unrecorded)" : this.SnykOptions.CurrentCliVersion);

                return true;
            }
            catch (Exception ex)
            {
                // The lookup failed, so which release is current is unknown and the checksum comparison
                // above could not run. Fall back to the one question that needs no network: does the
                // binary we already have speak our protocol version? If it does, use it. If it does not,
                // ask for the download — it will probably fail too, and failing visibly is the point,
                // because silently keeping an unusable CLI is what left the language server dead.
                var protocolSupported = this.IsCliProtocolSupported(cliFileDestinationPath);

                Logger.Error(ex,
                    "Could not fetch the latest CLI release info, so whether {Path} is current is unknown. It {Verdict} protocol version {ProtocolVersion}",
                    cliFileDestinationPath,
                    protocolSupported ? "reports the expected" : "does NOT report the expected",
                    LsConstants.ProtocolVersion);

                return !protocolSupported;
            }
        }


        /// <summary>
        /// Check is CLI file not exists by provided location.
        /// </summary>
        /// <param name="cliFileDestinationPath">CLI location path.</param>
        /// <returns>True if CLI file not exists.</returns>
        public bool IsCliFileExists(string cliFileDestinationPath = null) => File.Exists(cliFileDestinationPath);

        /// <summary>
        /// <see cref="IsCliUpToDate(string, string)"/>, hashed at most once per binary per instance.
        ///
        /// The comparison reads the whole ~175 MB file, and a startup asks the question three times, so
        /// without this the binary was hashed three times per launch — about a second of it cold, and far
        /// worse when CliCustomPath names a network share, where every hash pulls the file over the wire.
        ///
        /// Keyed on length and last-write-time as well as path, so the memo answers only for the exact
        /// file it measured: if the binary is replaced — including by our own install — the key changes
        /// and the next caller hashes again rather than trusting a stale verdict. That is also why this
        /// needs nothing persisted between sessions.
        /// </summary>
        private bool IsCliUpToDateOnce(string cliFileDestinationPath, string expectedSha)
        {
            lock (this.memoLock)
            {
                // Stat inside the lock, not before it: a key snapshotted outside could be taken for the
                // pre-install file, block here while another caller completes an install and writes the
                // memo, then wake and match the stored key for a file that has since been replaced —
                // which is exactly what the doc above promises cannot happen. The stat is the cheap part.
                var key = BuildUpToDateMemoKey(cliFileDestinationPath, expectedSha);

                if (key != null && string.Equals(key, this.upToDateMemoKey, StringComparison.Ordinal))
                {
                    return this.upToDateMemoVerdict;
                }

                var verdict = IsCliUpToDate(cliFileDestinationPath, expectedSha, this.ComputeChecksum);

                // Only memoise when the file could actually be measured; otherwise re-ask every time.
                if (key != null)
                {
                    this.upToDateMemoKey = key;
                    this.upToDateMemoVerdict = verdict;
                }

                return verdict;
            }
        }

        // virtual for testability: hashing the binary is the expensive part of the decision, and the
        // tests count how often it happens — a cost that is invisible from the verdict alone.
        internal virtual string ComputeChecksum(string filePath) => Sha256.Checksum(filePath);

        // Null when the file cannot be stat'd, which makes the result unmemoisable rather than wrong.
        private static string BuildUpToDateMemoKey(string cliFileDestinationPath, string expectedSha)
        {
            if (string.IsNullOrEmpty(cliFileDestinationPath))
            {
                return null;
            }

            try
            {
                var info = new FileInfo(cliFileDestinationPath);

                if (!info.Exists)
                {
                    return null;
                }

                // Unit separator: cannot occur in a path, a length or a checksum, so the parts cannot
                // run together into a colliding key.
                return string.Join(
                    "\u001f",
                    cliFileDestinationPath,
                    info.Length.ToString(CultureInfo.InvariantCulture),
                    info.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture),
                    expectedSha ?? string.Empty);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Whether a CLI already on disk is worth falling back to after a download did not happen.
        ///
        /// Presence is not enough, which is what made a failed download look survivable: the language
        /// server was restarted against whatever file was there, so a truncated or protocol-incompatible
        /// binary produced a server that never came up and an IDE that sat on "loading" indefinitely.
        /// An older release that still speaks our protocol is genuinely usable; anything else is not,
        /// and saying so is more useful than starting a server that cannot work.
        ///
        /// This is one of the two places the protocol probe runs, and it is only reached after a
        /// download has already failed or been cancelled — never on a normal startup.
        /// </summary>
        public bool IsExistingCliUsable(string cliFileDestinationPath)
        {
            if (!this.IsCliFileExists(cliFileDestinationPath))
            {
                Logger.Information("No CLI to fall back to at {Path}", cliFileDestinationPath);

                return false;
            }

            return this.IsCliProtocolSupported(cliFileDestinationPath);
        }

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

                return;
            }

            progressWorker.IsWorkFinished = true;

            // Raises DownloadFinished because that is what starts the language server against the loaded
            // solution and clears the tool window's loading state. Deliberately NOT the finished-callback
            // list: those callbacks mean "record what was installed", and running them here re-fetches
            // the release name over the network (breaking offline startup) and writes a version for an
            // install that never happened. binaryWasDownloaded:false so subscribers that report progress
            // do not announce a download that did not occur.
            progressWorker.DownloadFinished(binaryWasDownloaded: false);
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

            Logger.Information("Latest relase information: version {Version} and url {Url}", latestReleaseInfo.Version, DescribeUrlForLog(latestReleaseInfo.Url));

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
                // The staging copy is inside the fallback, not above it. Staging adds a longer path and
                // a second file to create, so it can fail where a plain overwrite would have worked
                // (MAX_PATH, a directory that permits writes but not creates, a share at quota). Those
                // are installs that succeeded before staging existed, so they must not fail now.
                try
                {
                    File.Copy(sourceFilePath, stagingPath, overwrite: true);
                    File.Replace(stagingPath, cliFileDestinationPath, destinationBackupFileName: null);
                }
                // PathTooLongException derives from IOException, so MAX_PATH overflow is covered here.
                catch (Exception e) when (e is IOException || e is PlatformNotSupportedException || e is UnauthorizedAccessException)
                {
                    // File.Replace also needs a volume that supports it; a custom CLI path can name an
                    // SMB share or a FAT volume. Failing the install outright would be worse than a
                    // non-atomic overwrite, so fall back — but say so, because the guarantee is gone.
                    Logger.Warning(e, "Could not stage and replace at {Path}; overwriting in place", cliFileDestinationPath);
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
            // Captured before the attempt: afterwards a partial write is indistinguishable from a
            // binary that was already there, which is what made the failure message misreport a
            // first install as an update.
            var priorCliExisted = File.Exists(cliFileDestinationPath);

            try
            {
                InstallCliFile(sourceFilePath, cliFileDestinationPath);
            }
            catch (Exception e)
            {
                this.ReportInstallFailure(e, cliFileDestinationPath, priorCliExisted);

                // Set on the failure path as well as in FinishDownload: the caller's finally gates
                // disposal of the cancellation token source and TaskFinished on this flag, so a failed
                // install otherwise leaked the token and never refreshed the toolbar, leaving Scan and
                // Clean disabled for the rest of the session even though the download had ended.
                progressWorker.IsWorkFinished = true;

                throw;
            }

            // Outside the catch above: it diagnoses the file copy, and the callbacks are what start
            // the language server. A callback failing is not the CLI failing to install, and must not
            // be reported as one. It still propagates; the caller has a single failure channel, so it
            // surfaces DownloadFailed either way — but the install-specific diagnosis stays accurate.
            this.FinishDownload(progressWorker, downloadFinishedCallbacks, binaryWasDownloaded: true);
        }

        // virtual for testability: the notification sink is a static singleton, so a test cannot
        // otherwise observe whether a failure was diagnosed as an install failure.
        internal virtual void ReportInstallFailure(Exception e, string cliFileDestinationPath, bool priorCliExisted)
        {
            var message = BuildInstallFailureMessage(e, cliFileDestinationPath, priorCliExisted);

            // Null-conditional: the download can run before the package initialises this.
            NotificationService.Instance?.ShowErrorInfoBar(message);
            Logger.Error(e, "Error on CLI copy from temp file to {Path}", cliFileDestinationPath);
        }

        /// <summary>
        /// The user-facing text for a failed install. Separated from the notification because the sink
        /// is a static singleton, so this is the only way to assert what the user is actually told.
        /// </summary>
        // internal for testability.
        internal static string BuildInstallFailureMessage(Exception e, string cliFileDestinationPath, bool priorCliExisted)
        {
            // Keyed on whether a binary was there BEFORE the attempt, not on File.Exists afterwards: a
            // failed first install can leave a partial file, which an after-the-fact probe would report
            // as an installed CLI.
            //
            // States that the prior binary is present rather than that it will work: the fallback in
            // InstallCliFile overwrites in place on volumes that cannot do an atomic replace, so on
            // that path the file that survives may itself be a partial write.
            return priorCliExisted
                ? $"Snyk CLI could not be updated at {cliFileDestinationPath}: {e.Message} The previously installed CLI is still in place."
                : $"Snyk CLI could not be installed at {cliFileDestinationPath}: {e.Message}";
        }

        /// <summary>
        /// True when the binary already there is the one we would install. The language server runs the
        /// binary at this path while the extension may be re-downloading it, so overwriting a copy that
        /// is already current buys nothing and risks a sharing violation.
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
                // Memoised: IsCliDownloadNeeded has normally already fetched this, so reaching here
                // costs no request. Still checked rather than assumed — DownloadAsync is public and
                // callable without having gone through the decision above.
                //
                // Use the returned value, not a re-read of expectedSha: reading the guarded field from
                // outside the lock is safe here only by accident of ordering, and it is exactly the
                // pattern the lock exists to remove.
                var latestSha = this.GetLatestCliShaOnce(cliDownloadUrl);

                if (this.IsCliUpToDateOnce(cliFileDestinationPath, latestSha))
                {
                    Logger.Information(
                        "CLI at {Path} already matches the expected checksum — skipping download",
                        cliFileDestinationPath);

                    // Without this the progress bar jumps from 0 straight to finished.
                    progressWorker.UpdateProgress(100);

                    // The binary on disk already matched, so nothing was fetched. Announcing a
                    // download here told the user the CLI had been updated when it had not.
                    this.FinishDownload(progressWorker, downloadFinishedCallbacks, binaryWasDownloaded: false);

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

        // binaryWasDownloaded has no default: both call sites reach here for opposite reasons, and a
        // default silently gave the checksum-match path a "downloaded successfully" it had not earned.
        private void FinishDownload(
            ISnykProgressWorker progressWorker,
            List<CliDownloadFinishedCallback> downloadFinishedCallbacks,
            bool binaryWasDownloaded)
        {
            Logger.Information("Fire DownloadFinished event");

            if (downloadFinishedCallbacks != null)
            {
                downloadFinishedCallbacks.ForEach(downloadFinishedCallback => downloadFinishedCallback());
            }

            // Set here as well as on the nothing-to-do path: the caller's finally gates disposal of the
            // cancellation token source and TaskFinished on it, so without this an actual install left
            // the token undisposed and never refreshed the toolbar state.
            progressWorker.IsWorkFinished = true;

            progressWorker.DownloadFinished(binaryWasDownloaded);
        }
    }
}
