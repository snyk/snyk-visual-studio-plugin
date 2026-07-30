using System;
using System.Collections.Generic;
using Moq;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    /// <summary>
    /// Download-URL construction. Network-free (unlike <see cref="SnykCliDownloaderTest"/>): these
    /// pin the shape of the URLs the downloader builds from the CLI options.
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
            // The LS registers binary_base_url and cli_release_channel with empty defaults and echoes
            // both in $/snyk.configuration, and the settings form lets the user clear them. An empty
            // value must resolve to the canonical default, never be interpolated verbatim: the empty
            // pair produced the relative "/cli//ls-protocol-version-25", which WebClient resolved
            // through Path.GetFullPath into a local file path (C:\cli\ls-protocol-version-25) and threw
            // DirectoryNotFoundException instead of downloading anything.
            var url = Downloader(baseDownloadUrl, releaseChannel).BuildLatestReleaseVersionUrl();

            Assert.Equal(ExpectedDefaultVersionUrl, url);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        public void BuildLatestReleaseVersionUrl_IsAbsoluteHttps_WhenOptionsAreNotSet(string baseDownloadUrl, string releaseChannel)
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
        // Unset — the ordinary case: a cleared field, or the LS echoing its own empty default.
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        // Not a host at all. These cannot be repaired by assuming a scheme, so the default is the only
        // safe answer (each is logged as a misconfiguration rather than silently swapped).
        [InlineData("/downloads.snyk.io")]
        [InlineData(@"C:\downloads")]
        [InlineData("file:///c:/downloads")]
        [InlineData("ftp://downloads.snyk.io")]
        [InlineData("not a url")]
        public void ResolveBaseDownloadUrl_FallsBackToDefault(string configured)
        {
            Assert.Equal(SnykCliDownloader.DefaultBaseDownloadUrl, SnykCliDownloader.ResolveBaseDownloadUrl(configured));
        }

        [Theory]
        // Typed like a browser address bar. The configured host must be honoured — silently sending an
        // egress-restricted customer to the public download host instead is worse than either using
        // what they meant or failing loudly.
        [InlineData("downloads.snyk.io", "https://downloads.snyk.io")]
        [InlineData("downloads.snyk.io/fips", "https://downloads.snyk.io/fips")]
        [InlineData("artifacts.internal:8081/snyk", "https://artifacts.internal:8081/snyk")]
        [InlineData("localhost:3000", "https://localhost:3000")]
        [InlineData("  downloads.snyk.io  ", "https://downloads.snyk.io")]
        public void ResolveBaseDownloadUrl_AssumesHttps_ForASchemelessHost(string configured, string expected)
        {
            Assert.Equal(expected, SnykCliDownloader.ResolveBaseDownloadUrl(configured));
        }

        [Fact]
        public void BuildLatestReleaseVersionUrl_UsesTheSchemelessHostTheUserTyped()
        {
            var url = Downloader("downloads.snyk.io/fips", "preview").BuildLatestReleaseVersionUrl();

            Assert.Equal(
                "https://downloads.snyk.io/fips/cli/preview/ls-protocol-version-" + LsConstants.ProtocolVersion,
                url);
        }

        [Theory]
        [InlineData("https://downloads.snyk.io")]
        [InlineData("https://downloads.snyk.io/fips")]
        [InlineData("http://artifacts.internal/snyk")]
        public void ResolveBaseDownloadUrl_KeepsAbsoluteWebUrls(string configured)
        {
            Assert.Equal(configured, SnykCliDownloader.ResolveBaseDownloadUrl(configured));
        }

        [Theory]
        [InlineData(@"C:\downloads")]
        [InlineData("file:///c:/downloads")]
        [InlineData("not a url")]
        public void BuildLatestReleaseVersionUrl_IsAbsoluteHttps_WhenBaseUrlIsNotAHost(string configured)
        {
            var url = Downloader(configured, "stable").BuildLatestReleaseVersionUrl();

            Assert.Equal(ExpectedDefaultVersionUrl, url);
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
        public void Resolvers_PreserveConfiguredValues()
        {
            Assert.Equal("https://example.internal", SnykCliDownloader.ResolveBaseDownloadUrl("https://example.internal"));
            Assert.Equal("rc", SnykCliDownloader.ResolveReleaseChannel("rc"));
        }

        public static IEnumerable<object[]> PathologicalConfigurations()
        {
            var baseUrls = new[] { null, "", "   ", "downloads.snyk.io", "/cli", @"C:\downloads", "file:///c:/x", "not a url" };
            var channels = new[] { null, "", "   ", "stable", "v1.1292.0" };

            foreach (var baseUrl in baseUrls)
            {
                foreach (var channel in channels)
                {
                    yield return new object[] { baseUrl, channel };
                }
            }
        }

        [Theory]
        [MemberData(nameof(PathologicalConfigurations))]
        public void EveryBuiltUrl_IsAnAbsoluteWebUrl_ForAnyConfiguredValue(string baseUrl, string channel)
        {
            // Blanket invariant: whatever lands in the options, the downloader must never hand
            // WebClient something it will resolve as a local file path. A Theory rather than one Fact
            // so a regression reports every failing combination, not just the first.
            var downloader = Downloader(baseUrl, channel);

            foreach (var url in new[] { downloader.BuildLatestReleaseVersionUrl(), downloader.BuildCliDownloadUrl("v1.1292.0") })
            {
                Assert.True(
                    Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps,
                    $"base '{baseUrl}' + channel '{channel}' produced non-https URL '{url}'");
            }
        }
    }
}
