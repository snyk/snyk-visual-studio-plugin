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
    /// Download-URL construction. Network-free (unlike <see cref="SnykCliDownloaderTest"/>): these pin
    /// the shape of the URLs the downloader builds from the CLI options.
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
            // The defect: snyk-ls registers binary_base_url and cli_release_channel with empty defaults
            // and echoes every machine-scope setting in $/snyk.configuration, so empty reached the
            // options and was interpolated verbatim. The empty pair produced the relative
            // "/cli//ls-protocol-version-25", which WebClient resolved through Path.GetFullPath into a
            // local file path (C:\cli\ls-protocol-version-25) and reported as DirectoryNotFoundException
            // instead of downloading anything.
            var url = Downloader(baseDownloadUrl, releaseChannel).BuildLatestReleaseVersionUrl();

            Assert.Equal(ExpectedDefaultVersionUrl, url);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        public void BuildLatestReleaseVersionUrl_IsAbsolute_WhenOptionsAreNotSet(string baseDownloadUrl, string releaseChannel)
        {
            // Guards the failure mode directly: a relative URL is what WebClient turns into a file path.
            var url = Downloader(baseDownloadUrl, releaseChannel).BuildLatestReleaseVersionUrl();

            Assert.True(Uri.TryCreate(url, UriKind.Absolute, out var uri), $"'{url}' is not an absolute URI");
            Assert.Equal(Uri.UriSchemeHttps, uri.Scheme);
        }

        [Fact]
        public void BuildLatestReleaseVersionUrl_UsesConfiguredValues()
        {
            var url = Downloader("https://downloads.snyk.io/fips", "preview").BuildLatestReleaseVersionUrl();

            Assert.Equal(
                "https://downloads.snyk.io/fips/cli/preview/ls-protocol-version-" + LsConstants.ProtocolVersion,
                url);
        }

        [Fact]
        public void BuildLatestReleaseVersionUrl_UsesCustomVersionChannel()
        {
            // "Specify version…" in the settings form stores the version as the release channel.
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
        public void ResolveBaseDownloadUrl_UsesTheDefault_WhenUnset(string configured)
        {
            Assert.Equal(SnykCliDownloader.DefaultBaseDownloadUrl, SnykCliDownloader.ResolveBaseDownloadUrl(configured));
        }

        [Theory]
        [InlineData("https://downloads.snyk.io")]
        [InlineData("https://downloads.snyk.io/fips")]
        // An internal mirror on plain http, and one requiring credentials, are both supported.
        [InlineData("http://artifacts.internal/snyk")]
        [InlineData("https://user:token@artifacts.internal/snyk")]
        public void ResolveBaseDownloadUrl_UsesAConfiguredValueVerbatim(string configured)
        {
            Assert.Equal(configured, SnykCliDownloader.ResolveBaseDownloadUrl(configured));
        }

        [Fact]
        public void ResolveBaseDownloadUrl_TrimsSurroundingWhitespace()
        {
            // Matches snyk-ls (applyCliBaseDownloadURL) and VS Code (getCliBaseDownloadUrl).
            Assert.Equal("https://downloads.snyk.io", SnykCliDownloader.ResolveBaseDownloadUrl("  https://downloads.snyk.io  "));
        }

        [Theory]
        // A value that is not a usable URL is NOT rewritten. Every other Snyk IDE passes the configured
        // value through unchanged — Eclipse's LsBinaries.resolveBaseUrl, VS Code's
        // getCliBaseDownloadUrl and snyk-ls's applyCliBaseDownloadURL all default only on empty — so a
        // scheme-less host fails here exactly as it does there, rather than working in one IDE only.
        //
        // The trade this pins: the EMPTY case is guaranteed to compose into an absolute URL (the defect
        // this branch fixes, covered above), while a non-empty configured value is the user's and fails
        // as it does in every other IDE. In Visual Studio that failure is a local-path read rather than
        // a network error, which is why only the empty case is defended in depth.
        [InlineData("downloads.snyk.io")]
        [InlineData(@"C:\downloads")]
        [InlineData("not a url")]
        public void ResolveBaseDownloadUrl_DoesNotRewriteAnUnusableValue(string configured)
        {
            Assert.Equal(configured, SnykCliDownloader.ResolveBaseDownloadUrl(configured));
        }

        [Theory]
        [InlineData("https://user:token@artifacts.internal/snyk", "https://<credentials>@artifacts.internal/snyk")]
        [InlineData("http://user:token@artifacts.internal:8081/a", "http://<credentials>@artifacts.internal:8081/a")]
        // Scheme-less credentials are the easiest case to miss: "user:pass@host" parses as an absolute
        // URI with scheme "user" and an EMPTY UserInfo, so relying on Uri.UserInfo alone would log the
        // secret verbatim. A configured value is written to the log on every download.
        [InlineData("user:pass@artifacts.internal", "<credentials>@artifacts.internal")]
        [InlineData("user@artifacts.internal", "<credentials>@artifacts.internal")]
        [InlineData("https://downloads.snyk.io/fips", "https://downloads.snyk.io/fips")]
        [InlineData("downloads.snyk.io", "downloads.snyk.io")]
        // A path segment containing '@' is not userinfo and must not be mistaken for it.
        [InlineData("https://downloads.snyk.io/path@v2", "https://downloads.snyk.io/path@v2")]
        public void Redact_BlanksCredentialsAndLeavesEverythingElse(string value, string expected)
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
