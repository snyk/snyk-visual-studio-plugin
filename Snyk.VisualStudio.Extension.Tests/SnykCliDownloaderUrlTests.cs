using System;
using Moq;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    /// <summary>
    /// Download-URL construction from the CLI options. Network-free.
    /// </summary>
    public class SnykCliDownloaderUrlTests
    {
        private const string ExpectedDefaultVersionUrl =
            "https://downloads.snyk.io/cli/stable/ls-protocol-version-" + LsConstants.ProtocolVersion;

        private static SnykCliDownloader Downloader(string baseDownloadUrl, string releaseChannel)
        {
            var options = new Mock<ISnykOptions>();
            options.SetupGet(x => x.CliBaseDownloadURL).Returns(baseDownloadUrl);
            options.SetupGet(x => x.CliReleaseChannel).Returns(releaseChannel);
            return new SnykCliDownloader(options.Object);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("   ", "   ")]
        // Mixed: only one of the two unset.
        [InlineData("", "stable")]
        [InlineData("https://downloads.snyk.io", "")]
        public void BuildLatestReleaseVersionUrl_UsesDefaults_WhenOptionsAreNotSet(string baseDownloadUrl, string releaseChannel)
        {
            // An empty pair composes to "/cli//ls-protocol-version-25", which WebClient resolves to a
            // local file path rather than requesting anything.
            var url = Downloader(baseDownloadUrl, releaseChannel).BuildLatestReleaseVersionUrl();

            Assert.Equal(ExpectedDefaultVersionUrl, url);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("/", "stable")]
        [InlineData("///", "")]
        [InlineData("https://", "stable")]
        public void BuildLatestReleaseVersionUrl_IsAbsolute_WhenOptionsAreNotSet(string baseDownloadUrl, string releaseChannel)
        {
            // A relative URL is what WebClient turns into a file path.
            var url = Downloader(baseDownloadUrl, releaseChannel).BuildLatestReleaseVersionUrl();

            Assert.True(Uri.TryCreate(url, UriKind.Absolute, out var uri), $"'{url}' is not an absolute URI");
            Assert.Equal(Uri.UriSchemeHttps, uri.Scheme);
        }

        [Fact]
        public void BuildLatestReleaseVersionUrl_UsesTheConfiguredBaseUrlAndChannelVerbatim()
        {
            var url = Downloader("https://downloads.snyk.io/fips", "preview").BuildLatestReleaseVersionUrl();

            Assert.Equal(
                "https://downloads.snyk.io/fips/cli/preview/ls-protocol-version-" + LsConstants.ProtocolVersion,
                url);
        }

        [Fact]
        public void BuildLatestReleaseVersionUrl_UsesCustomVersionChannel()
        {
            // The settings form stores a pinned version as the release channel.
            var url = Downloader("https://downloads.snyk.io", "v1.1292.0").BuildLatestReleaseVersionUrl();

            Assert.Equal(
                "https://downloads.snyk.io/cli/v1.1292.0/ls-protocol-version-" + LsConstants.ProtocolVersion,
                url);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void BuildCliDownloadUrl_UsesDefaultBaseUrl_WhenBaseUrlIsNotSet(string baseDownloadUrl)
        {
            var url = Downloader(baseDownloadUrl, "stable").BuildCliDownloadUrl("v1.1292.0");

            Assert.Equal("https://downloads.snyk.io/cli/v1.1292.0/" + SnykCli.CliFileName, url);
        }

        [Fact]
        public void BuildCliDownloadUrl_UsesConfiguredBaseUrl()
        {
            var url = Downloader("https://downloads.snyk.io/fips", "stable").BuildCliDownloadUrl("v1.1292.0");

            Assert.Equal("https://downloads.snyk.io/fips/cli/v1.1292.0/" + SnykCli.CliFileName, url);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        // Values that BECOME unusable once normalised. Guarding before trimming let these through to
        // compose a relative URL — the exact failure this method exists to prevent.
        [InlineData("/")]
        [InlineData("//")]
        [InlineData("///")]
        [InlineData("  /  ")]
        [InlineData(@"\")]
        // A scheme with no authority is unusable for the same reason.
        [InlineData("https://")]
        [InlineData("https:")]
        public void ResolveBaseDownloadUrl_UsesTheDefault_WhenUnset(string configured)
        {
            Assert.Equal(SnykCliDownloader.DefaultBaseDownloadUrl, SnykCliDownloader.ResolveBaseDownloadUrl(configured));
        }

        [Theory]
        [InlineData("https://downloads.snyk.io")]
        [InlineData("https://downloads.snyk.io/fips")]
        [InlineData("http://artifacts.internal/snyk")]
        [InlineData("https://user:token@artifacts.internal/snyk")]
        public void ResolveBaseDownloadUrl_UsesAConfiguredValueVerbatim(string configured)
        {
            Assert.Equal(configured, SnykCliDownloader.ResolveBaseDownloadUrl(configured));
        }

        [Fact]
        public void ResolveBaseDownloadUrl_TrimsSurroundingWhitespace()
        {
            Assert.Equal("https://downloads.snyk.io", SnykCliDownloader.ResolveBaseDownloadUrl("  https://downloads.snyk.io  "));
        }

        [Theory]
        // The URL schemes add their own separator and Uri does not collapse "//" inside a path, so a
        // trailing slash would fetch ".../cli//stable/...". The LS-served settings page resets this
        // field to a value with a trailing slash, so it arrives in practice.
        [InlineData("https://downloads.snyk.io/", "https://downloads.snyk.io")]
        [InlineData("https://downloads.snyk.io///", "https://downloads.snyk.io")]
        [InlineData("  https://artifacts.internal/snyk/  ", "https://artifacts.internal/snyk")]
        public void ResolveBaseDownloadUrl_DropsTrailingSlashes(string configured, string expected)
        {
            Assert.Equal(expected, SnykCliDownloader.ResolveBaseDownloadUrl(configured));
        }

        [Fact]
        public void BuildLatestReleaseVersionUrl_DoesNotDoubleTheSeparator_ForATrailingSlashBaseUrl()
        {
            var url = Downloader("https://downloads.snyk.io/", "stable").BuildLatestReleaseVersionUrl();

            Assert.Contains(".io/cli/", url);
            Assert.DoesNotContain(".io//cli/", url);
        }

        [Theory]
        // Not an http(s) origin, so the default is used instead of the configured value. A UNC target
        // is the reason this is enforced rather than passed through: WebClient accepts it and the
        // version/checksum fetches then perform an implicit SMB authentication against whatever host
        // was configured, handing it the Windows user's NTLM credentials.
        [InlineData(@"\\attacker\share")]
        [InlineData(@"\\10.0.0.1\snyk")]
        [InlineData("file:///C:/downloads")]
        [InlineData(@"C:\downloads")]
        // A scheme-less value is what WebClient resolves to a local path — the original bug.
        [InlineData("downloads.snyk.io")]
        [InlineData("not a url")]
        [InlineData("ftp://artifacts.internal/snyk")]
        public void ResolveBaseDownloadUrl_UsesTheDefault_WhenTheValueIsNotAnHttpOrigin(string configured)
        {
            Assert.Equal(SnykCliDownloader.DefaultBaseDownloadUrl, SnykCliDownloader.ResolveBaseDownloadUrl(configured));
        }

        [Theory]
        // Plain http stays allowed: an internal mirror on http is a legitimate setup, and the checksum
        // is fetched from the same origin either way, so refusing it would break users without adding
        // the integrity guarantee that refusal implies.
        [InlineData("http://artifacts.internal/snyk")]
        [InlineData("https://artifacts.internal/snyk")]
        [InlineData("http://localhost:8081/snyk")]
        public void ResolveBaseDownloadUrl_AllowsBothHttpAndHttps(string configured)
        {
            Assert.Equal(configured, SnykCliDownloader.ResolveBaseDownloadUrl(configured));
        }

        [Theory]
        [InlineData("https://user:token@artifacts.internal/snyk", "https://<credentials>@artifacts.internal/snyk")]
        [InlineData("http://user:token@artifacts.internal:8081/a", "http://<credentials>@artifacts.internal:8081/a")]
        // No scheme, so the whole value is authority.
        [InlineData("user:pass@artifacts.internal", "<credentials>@artifacts.internal")]
        [InlineData("user@artifacts.internal", "<credentials>@artifacts.internal")]
        // Percent-escaped userinfo: Uri.UserInfo unescapes this to "user:token", which does not
        // occur in the original, so anything matching on the parsed value leaks the token.
        [InlineData("https://us%65r:token@artifacts.internal/snyk", "https://<credentials>@artifacts.internal/snyk")]
        [InlineData("https://user:tok%2Den@artifacts.internal/snyk", "https://<credentials>@artifacts.internal/snyk")]
        // An '@' inside the credentials is covered; the scheme survives.
        [InlineData("https://user:tok@en@host/snyk", "https://<credentials>@host/snyk")]
        // A delimiter INSIDE the credential. Keying the authority's end off the first '/' hid the '@'
        // entirely and logged these verbatim — base64 tokens contain '/' routinely.
        [InlineData("https://user:pa/ss@host/p", "https://<credentials>@host/p")]
        [InlineData(@"https://user:pa\ss@host/p", "https://<credentials>@host/p")]
        [InlineData("https://user:pa?ss@host/p", "https://<credentials>@host/p")]
        [InlineData("https://user:pa#ss@host/p", "https://<credentials>@host/p")]
        [InlineData("https://user:aGVsbG8/d29ybGQ=@artifacts.internal/snyk", "https://<credentials>@artifacts.internal/snyk")]
        // A separator in the *username*, before the colon.
        [InlineData("https://u/s/e/r:pw@host/p", "https://<credentials>@host/p")]
        // An '@' in a query is not userinfo, so this must survive.
        [InlineData("https://host/p?q=a@b", "https://host/p?q=a@b")]
        // Accepted over-redaction: a port makes the ':' test fire. Losing a path beats leaking a secret.
        [InlineData("http://host:8081/path@v2", "http://<credentials>@v2")]
        [InlineData("https://downloads.snyk.io/fips", "https://downloads.snyk.io/fips")]
        [InlineData("downloads.snyk.io", "downloads.snyk.io")]
        // An '@' past the authority is not userinfo, whatever the scheme.
        [InlineData("https://downloads.snyk.io/path@v2", "https://downloads.snyk.io/path@v2")]
        [InlineData("ftp://artifacts.internal/path@v2", "ftp://artifacts.internal/path@v2")]
        // A malformed scheme separator must not be a way through.
        [InlineData("//user:pass@host/p", "//<credentials>@host/p")]
        [InlineData(@"https:\\user:pass@host\p", @"https:\\<credentials>@host\p")]
        // A short scheme is still a scheme when "//" follows, so it must not be taken for a drive.
        [InlineData("x://user:pass@host", "x://<credentials>@host")]
        [InlineData("://user:pass@host", "://<credentials>@host")]
        // Local paths must survive intact: ResolveBaseDownloadUrl passes an unusable value through
        // verbatim, and this log line exists to show what the user actually configured.
        [InlineData(@"C:\tools\snyk@2\cli.exe", @"C:\tools\snyk@2\cli.exe")]
        [InlineData(@"\\fileserver\share@2\snyk", @"\\fileserver\share@2\snyk")]
        [InlineData(@"C:/tools/snyk@2", @"C:/tools/snyk@2")]
        [InlineData("/opt/snyk@2/cli", "/opt/snyk@2/cli")]
        // The '@' in the first segment, so the drive letter must not be read as a scheme.
        [InlineData(@"C:\snyk@2\cli.exe", @"C:\snyk@2\cli.exe")]
        [InlineData(@"D:\cli@latest\snyk-win.exe", @"D:\cli@latest\snyk-win.exe")]
        // A doubled separator reads as a scheme, so this over-redacts. Accepted: a drive path and a
        // one-character scheme are indistinguishable here, and losing a path beats leaking a secret.
        [InlineData(@"C:\\snyk@2\cli.exe", @"C:\\<credentials>@2\cli.exe")]
        [InlineData(null, null)]
        [InlineData("", "")]
        public void Redact_BlanksCredentialsInTheAuthorityAndLeavesEverythingElse(string value, string expected)
        {
            Assert.Equal(expected, SnykCliDownloader.Redact(value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void ResolveReleaseChannel_FallsBackToDefault(string configured)
        {
            Assert.Equal(SnykCliDownloader.DefaultReleaseChannel, SnykCliDownloader.ResolveReleaseChannel(configured));
        }

        [Fact]
        public void ResolveReleaseChannel_PreservesAConfiguredValue()
        {
            Assert.Equal("rc", SnykCliDownloader.ResolveReleaseChannel("rc"));
        }
    }
}
