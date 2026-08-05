using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
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
    /// Outcome of <see cref="SnykCliDownloader.CheckCliProtocol"/>. Kept distinct from a plain bool so a
    /// caller can tell "confirmed incompatible" apart from "couldn't be checked at all" (IDE-2404) -
    /// collapsing those into one "not supported" answer misattributes a timeout or a transient error
    /// (e.g. an AV scanner still holding a just-downloaded binary) as a genuine version mismatch.
    /// </summary>
    internal enum CliProtocolCheckResult
    {
        Supported,
        Unsupported,
        TimedOut,
        CheckFailed,
    }

    /// <summary>
    /// Donwnload last Snyk CLI version.
    /// </summary>
    public class SnykCliDownloader
    {
        public const string DefaultBaseDownloadUrl = "https://downloads.snyk.io";
        public const string DefaultReleaseChannel = "stable";

        // What a locally-built language server reports instead of a protocol number.
        private const string DevelopmentProtocolVersion = "development";

        // Generous: a cold first run can be slow while the on-access scanner reads ~175MB. Still
        // bounded, because an unbounded wait here would freeze the CLI-download decision.
        // internal (not private) so tests exercising the real process-spawn path (InternalsVisibleTo:
        // Snyk.VisualStudio.Extension.Tests, Integration.Tests) can mirror this instead of a literal
        // that would silently drift if this value is ever tuned.
        internal const int ProtocolProbeTimeoutMs = 20000;

        private const string LatestReleaseVersionUrlScheme = "{0}/cli/{1}/ls-protocol-version-" + LsConstants.ProtocolVersion;
        private const string LatestReleaseDownloadUrlScheme = "{0}/cli/{1}/" + SnykCli.CliFileName;
        private const string Sha256DownloadUrl = "{0}.sha256";

        private static readonly ILogger Logger = LogManager.ForContext<SnykCliDownloader>();

        // Path separators. Not "where the authority ends": a credential may contain one unescaped.
        private static readonly char[] AuthorityDelimiters = { '/', '\\' };

        // A query or fragment cannot contain credentials, so an '@' past one is not a separator.
        private static readonly char[] QueryDelimiters = { '?', '#' };

        private readonly ISnykOptions SnykOptions;

        // Guards the three memos below. One downloader is shared across a startup's concurrent callers
        // (package init, the language client's load, the update itself), and an unguarded
        // "field ?? (field = Fetch())" lets two threads both see null and both fetch. Observed: three
        // release-version requests, three checksum requests and two full hashes of the binary in a
        // single startup, from one shared instance.
        //
        // Held across the fetch deliberately: the second caller waits and then reads the memo, which is
        // the point.
        //
        // The wait is NOT fully bounded, and callers are expected — but not required — to be off the UI
        // thread. SnykWebClient's timeout bounds the two requests; nothing bounds the checksum of a
        // ~175MB binary, which is the longer leg and worse on a network CliCustomPath. SnykVSPackage
        // hops to the pool before asking (see its comment on ShouldDownloadCli), but
        // SnykLanguageClient.OnLoadedAsync does not, so a future VS version calling it on the main
        // thread would hold this lock there. Worth an explicit hop at that call site.
        private readonly object memoLock = new object();

        private string expectedSha;
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
            // prevent. Backslashes too, matching Redact's view of what a separator is.
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
                Logger.Warning(
                    "Ignoring the configured CLI base download URL {Url}: only http and https origins are used. Falling back to {Default}",
                    Redact(normalised),
                    DefaultBaseDownloadUrl);

                return DefaultBaseDownloadUrl;
            }

            return normalised;
        }

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
            var driveLetterEnd = -1;
            var schemeEnd = value.IndexOf(':');

            if (schemeEnd >= 0 && schemeEnd + 1 < value.Length && IsSlash(value[schemeEnd + 1]))
            {
                // A one-character scheme is a Windows drive letter, and "C:\" introduces a path,
                // not an authority: otherwise the first path segment is read as the authority and
                // "C:\snyk@2\cli.exe" comes out as "C:\<credentials>@2\cli.exe". A second slash
                // means it really is a scheme, since no local path starts "C://".
                var isDriveLetter = schemeEnd < 2
                    && !(schemeEnd + 2 < value.Length && IsSlash(value[schemeEnd + 2]));

                if (isDriveLetter)
                {
                    // Remembered so the drive colon is not later mistaken for a userinfo separator.
                    driveLetterEnd = schemeEnd;
                }
                else
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

            // A query or fragment cannot hold credentials, so an '@' past one belongs to it
            // ("host/p?q=a@b") and must not be treated as a userinfo separator.
            var searchEnd = value.IndexOfAny(QueryDelimiters, authorityStart);

            if (searchEnd < 0)
            {
                searchEnd = value.Length;
            }
            else if (LooksLikeCredentialPrefix(value, authorityStart, driveLetterEnd, searchEnd))
            {
                // The '?' or '#' is inside the credential, not starting a query: everything before it
                // holds a ':' and no path separator, which a real "host/path?query" never does. Extend
                // to the path so the '@' after it is still found. A genuine query is unaffected —
                // "host:8081?q=1" extends too, but has no '@' to find.
                var pathStart = value.IndexOfAny(AuthorityDelimiters, searchEnd);

                searchEnd = pathStart < 0 ? value.Length : pathStart;
            }

            if (searchEnd <= authorityStart)
            {
                return value;
            }

            // Last '@', so a credential containing one is covered ("user:tok@en@host").
            var credentialsEnd = value.LastIndexOf('@', searchEnd - 1, searchEnd - authorityStart);

            if (credentialsEnd < 0)
            {
                return value;
            }

            // Is that '@' a userinfo separator, or just an '@' inside a path ("host/path@v2")?
            // Deliberately NOT keyed on the first '/' being the end of the authority: a credential
            // may contain an unescaped '/' — base64 tokens routinely do — and keying off it hid the
            // '@' entirely, so "https://user:aGVsbG8/d29ybGQ=@host" was logged verbatim.
            var firstSlash = value.IndexOfAny(AuthorityDelimiters, authorityStart);

            if (firstSlash >= 0 && firstSlash < credentialsEnd)
            {
                // There is a separator before the '@'. Only a credential if a ':' appears anywhere
                // before that '@' — which distinguishes "user:pa/ss@host" and "u/s/e/r:pw@host" from
                // "host/path@v2". Searching only up to the separator missed a slash in the *username*.
                // "host:8081/path@v2" is over-redacted as a result — losing a path in a log line
                // beats leaking a secret, the same trade this file makes for "C:\\snyk@2".
                var colonStart = driveLetterEnd >= 0 ? driveLetterEnd + 1 : authorityStart;
                var colonAt = colonStart < credentialsEnd
                    ? value.IndexOf(':', colonStart, credentialsEnd - colonStart)
                    : -1;

                if (colonAt < 0)
                {
                    return value;
                }
            }

            return value.Substring(0, authorityStart) + "<credentials>" + value.Substring(credentialsEnd);
        }

        private static bool IsSlash(char c) => c == '/' || c == '\\';

        // True when the text up to a '?' or '#' looks like userinfo rather than a host and path:
        // it contains a ':' and no path separator. Used to tell "user:pa?ss@host" from "host/p?q=a@b".
        private static bool LooksLikeCredentialPrefix(string value, int authorityStart, int driveLetterEnd, int delimiterAt)
        {
            var colonStart = driveLetterEnd >= 0 ? driveLetterEnd + 1 : authorityStart;

            if (colonStart >= delimiterAt)
            {
                return false;
            }

            var length = delimiterAt - colonStart;

            return value.IndexOf(':', colonStart, length) >= 0
                && value.IndexOfAny(AuthorityDelimiters, colonStart, length) < 0;
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
        /// The published checksum for a download URL, fetched once per downloader instance. Memoised for
        /// the same reason as the release info: it is now consulted by the download decision as well as
        /// by verification, and a second fetch could disagree with the first if a release lands between
        /// them. Also sets expectedSha, which VerifyCliFile compares against.
        /// </summary>
        // internal for testability (the tests assert the fetch happens once).
        internal string GetLatestCliShaOnce(string cliDownloadUrl)
        {
            lock (this.memoLock)
            {
                return this.expectedSha ?? (this.expectedSha = this.GetLatestCliSha(cliDownloadUrl));
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
        ///
        /// Thin wrapper over <see cref="CheckCliProtocol"/> (below): that method already does this launch
        /// correctly (async stdout draining, no pipe-buffer deadlock risk) and distinguishes a confirmed
        /// mismatch from a timeout or a failed probe — callers here only need a plain yes/no.
        /// </summary>
        // internal virtual for testability: a test double avoids launching a real process.
        internal virtual bool IsCliProtocolSupported(string cliFilePath) =>
            this.CheckCliProtocol(cliFilePath) == CliProtocolCheckResult.Supported;

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
        /// Whether the binary at <paramref name="cliFilePath"/> can actually serve the language server
        /// protocol we speak, and if not, why. File.Exists cannot tell a working CLI from a truncated,
        /// stale or wrong-architecture one, and a binary that cannot answer this cannot serve initialize
        /// either — it activates, fails, and the language server shuts down with no useful diagnosis.
        /// Mirrors what vscode-extension does before activating its client. Distinguishes why an
        /// incompatible answer was reached - a timeout or a failure to even read the version is not the
        /// same as the CLI actually reporting a mismatched one, and callers that surface a user-facing
        /// message should not claim "wrong version" for either of the former.
        /// </summary>
        // internal virtual for testability: a unit test cannot execute a real CLI.
        internal virtual CliProtocolCheckResult CheckCliProtocol(string cliFilePath)
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

                using (var process = new Process { StartInfo = startInfo })
                {
                    // Drain stdout concurrently via BeginOutputReadLine (started before WaitForExit)
                    // rather than WaitForExit-then-ReadToEnd (PR review finding): the latter assumed "the
                    // output is a few bytes, so it cannot fill the pipe buffer while we wait" - false for
                    // exactly the CLI this check exists to catch, an old binary that doesn't recognize
                    // --protocolVersion and dumps its full --help text instead, which is easily enough to
                    // fill the redirected-stdout pipe buffer. That deadlocks the child on its next write
                    // with nobody draining, so WaitForExit never observes the exit and this stalls the
                    // full ProtocolProbeTimeoutMs on every launch against that CLI - undermining the
                    // whole point of a fast, actionable rejection.
                    var output = new StringBuilder();
                    process.OutputDataReceived += (_, e) =>
                    {
                        if (e.Data != null)
                        {
                            output.AppendLine(e.Data);
                        }
                    };

                    if (!process.Start())
                    {
                        Logger.Warning("Could not start the CLI at {Path} to read its protocol version", cliFilePath);

                        return CliProtocolCheckResult.CheckFailed;
                    }

                    process.BeginOutputReadLine();

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

                        return CliProtocolCheckResult.TimedOut;
                    }

                    var reported = output.ToString().Trim();
                    var supported = IsSupportedProtocolVersion(reported);

                    if (!supported)
                    {
                        Logger.Warning(
                            "CLI at {Path} reports protocol version '{Reported}', expected '{Expected}'",
                            cliFilePath,
                            reported,
                            LsConstants.ProtocolVersion);
                    }

                    return supported ? CliProtocolCheckResult.Supported : CliProtocolCheckResult.Unsupported;
                }
            }
            catch (Exception e)
            {
                Logger.Warning(e, "Could not read the protocol version from the CLI at {Path}", cliFilePath);

                return CliProtocolCheckResult.CheckFailed;
            }
        }

        // internal for testability: pure comparison, no process involved. "development" is what a
        // locally-built language server reports; treat it as compatible so a dev CLI is usable,
        // matching snyk-ls' own handling.
        internal static bool IsSupportedProtocolVersion(string reportedVersion) =>
            string.Equals(reportedVersion, LsConstants.ProtocolVersion, StringComparison.Ordinal)
            || string.Equals(reportedVersion, DevelopmentProtocolVersion, StringComparison.Ordinal);

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
            // Keyed on whether a binary was there BEFORE the attempt, not on File.Exists afterwards.
            // A failed first install can leave a partial file, which made the after-the-fact probe
            // report a "previously installed" CLI that had never existed.
            //
            // States that the prior binary is present rather than that it will work: the fallback in
            // InstallCliFile overwrites in place on volumes that cannot do an atomic replace, so on
            // that path the file that survives may itself be a partial write.
            return priorCliExisted
                ? $"Snyk CLI could not be updated at {cliFileDestinationPath}: {e.Message} The previously installed CLI is still in place."
                : $"Snyk CLI could not be installed at {cliFileDestinationPath}: {e.Message}";
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
