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
        public void BuildLatestReleaseVersionUrl_IsAbsolute_WhenOptionsAreNotSet(string baseDownloadUrl, string releaseChannel)
        {
            // A relative URL is what WebClient turns into a file path.
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
        // Only the empty case is defaulted; a configured value is passed through as the user typed it,
        // as in every other Snyk IDE.
        [InlineData("downloads.snyk.io")]
        [InlineData(@"C:\downloads")]
        [InlineData("not a url")]
        public void ResolveBaseDownloadUrl_DoesNotRewriteAnUnusableValue(string configured)
        {
            Assert.Equal(configured, SnykCliDownloader.ResolveBaseDownloadUrl(configured));
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
