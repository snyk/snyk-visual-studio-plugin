using System;
using System.Collections.Generic;
using Moq;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Extension;
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
        // Not a host. Each is logged as a misconfiguration rather than silently swapped.
        [InlineData("/downloads.snyk.io")]
        [InlineData("//downloads.snyk.io")]
        [InlineData(@"C:\downloads")]
        [InlineData("C:/downloads")]
        [InlineData("file:///c:/downloads")]
        [InlineData("ftp://downloads.snyk.io")]
        [InlineData("javascript:alert(1)")]
        [InlineData("not a url")]
        // "http//host" — a scheme typed without its colon parses with "http" as the host.
        [InlineData("http//downloads.snyk.io")]
        // .NET rewrites these bare tokens into addresses: 12345 -> 0.0.48.57, 0x7f000001 -> 127.0.0.1.
        [InlineData("12345")]
        [InlineData("0x7f000001")]
        // A drive letter followed by digits looks exactly like host:port.
        [InlineData(@"D:8081")]
        // A bare token is indistinguishable from a hostname but is never what was meant.
        [InlineData("notaurl")]
        [InlineData("none")]
        // Unqualified single labels stay rejected...
        [InlineData("artifactory")]
        // ...and a one-character label is a drive letter, never a host, even with a port.
        [InlineData("C:/downloads")]
        // Credentials need an explicit scheme, or the userinfo is mistaken for a port.
        [InlineData("user@artifacts.internal")]
        [InlineData("user:pass@artifacts.internal")]
        // A query or fragment cannot survive composition — the path would land inside it.
        [InlineData("downloads.snyk.io?token=x")]
        [InlineData("downloads.snyk.io#frag")]
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
        [InlineData("1.2.3.4:8081", "https://1.2.3.4:8081")]
        // IPv6 literals: the colon inside the brackets is not a port separator.
        [InlineData("[fd00::1]", "https://[fd00::1]")]
        [InlineData("[fd00::1]:8081", "https://[fd00::1]:8081")]
        [InlineData("[fd00::1]:8081/snyk", "https://[fd00::1]:8081/snyk")]
        [InlineData("  downloads.snyk.io  ", "https://downloads.snyk.io")]
        // Single-label intranet hosts are ordinary in corporate networks, but only count as a host when
        // a port or path qualifies them — a bare token is a typo, not a mirror.
        [InlineData("artifactory:8081", "https://artifactory:8081")]
        [InlineData("artifactory:8081/repo", "https://artifactory:8081/repo")]
        [InlineData("nexus/snyk", "https://nexus/snyk")]
        public void ResolveBaseDownloadUrl_AssumesHttps_ForASchemelessHost(string configured, string expected)
        {
            Assert.Equal(expected, SnykCliDownloader.ResolveBaseDownloadUrl(configured));
        }

        [Theory]
        // A trailing slash is exactly what an address bar produces, and composing on top of it yields
        // "host//cli/...", which CDN-backed origins serve as a different key.
        [InlineData("https://downloads.snyk.io/", "https://downloads.snyk.io")]
        [InlineData("downloads.snyk.io/", "https://downloads.snyk.io")]
        [InlineData("https://downloads.snyk.io/fips/", "https://downloads.snyk.io/fips")]
        public void ResolveBaseDownloadUrl_TrimsATrailingSlash(string configured, string expected)
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
        // Credentials are preserved when the scheme is explicit — a private mirror may require them.
        [InlineData("https://user:token@artifacts.internal/snyk")]
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
        // The settings form completes a scheme-less host but never substitutes the default for an
        // unusable value: the user has to be able to see and correct what they typed.
        [InlineData("downloads.snyk.io", "https://downloads.snyk.io")]
        [InlineData("artifacts.internal:8081/snyk", "https://artifacts.internal:8081/snyk")]
        [InlineData("https://downloads.snyk.io/fips", "https://downloads.snyk.io/fips")]
        [InlineData(@"C:\downloads", @"C:\downloads")]
        [InlineData("notaurl", "notaurl")]
        // Normalisation is shared with the resolver, so what is stored is what will be requested.
        [InlineData("downloads.snyk.io/", "https://downloads.snyk.io")]
        [InlineData("https://downloads.snyk.io/fips/", "https://downloads.snyk.io/fips")]
        [InlineData("", "")]
        [InlineData("   ", "")]
        public void CompleteBaseDownloadUrl_RepairsIntentWithoutReplacingTheValue(string configured, string expected)
        {
            Assert.Equal(expected, SnykCliDownloader.CompleteBaseDownloadUrl(configured));
        }

        [Theory]
        [InlineData("", true, null)]
        [InlineData("https://downloads.snyk.io/fips", true, null)]
        [InlineData("artifactory:8081", true, null)]
        [InlineData("downloads.snyk.io?token=x", false, "it carries a query or fragment")]
        [InlineData(@"C:\downloads", false, "it is not a usable host")]
        public void TryResolveBaseDownloadUrl_ReportsWhyAValueWasRejected(string configured, bool expectedUsable, string expectedReason)
        {
            // The reason travels to the user via the download path; the resolver itself stays silent
            // because it runs on every settings round trip.
            var usable = SnykCliDownloader.TryResolveBaseDownloadUrl(configured, out var resolved, out var reason);

            Assert.Equal(expectedUsable, usable);
            Assert.Equal(expectedReason, reason);
            Assert.True(UriExtensions.IsValidWebUrl(resolved), "the resolved value is always usable");
        }

        [Theory]
        [InlineData("https://user:token@artifacts.internal/snyk", "https://<credentials>@artifacts.internal/snyk")]
        [InlineData("http://user:token@artifacts.internal:8081/a", "http://<credentials>@artifacts.internal:8081/a")]
        // Scheme-less credentials are the case that matters most and the easiest to miss:
        // "user:pass@host" parses as an absolute URI with scheme "user" and an EMPTY UserInfo, so
        // relying on Uri.UserInfo alone logs the secret verbatim. It reaches the log because
        // ResolveBaseDownloadUrl rejects it and warns with the configured value.
        [InlineData("user:pass@artifacts.internal", "<credentials>@artifacts.internal")]
        [InlineData("user@artifacts.internal", "<credentials>@artifacts.internal")]
        [InlineData("user:pass@artifacts.internal:8081/snyk", "<credentials>@artifacts.internal:8081/snyk")]
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
        public void Resolvers_PreserveConfiguredValues()
        {
            Assert.Equal("https://example.internal", SnykCliDownloader.ResolveBaseDownloadUrl("https://example.internal"));
            Assert.Equal("rc", SnykCliDownloader.ResolveReleaseChannel("rc"));
        }

        public static IEnumerable<object[]> PathologicalConfigurations()
        {
            var baseUrls = new[] { null, "", "   ", "downloads.snyk.io", "downloads.snyk.io/", "https://downloads.snyk.io/", "http://artifacts.internal/snyk", "[fd00::1]:8081", "/cli", @"C:\downloads", "file:///c:/x", "not a url", "12345" };
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
            // WebClient something it will resolve as a local file path, and the composed path must not
            // pick up a double slash from a trailing slash in the base. Asserts http-or-https because
            // an internal mirror on plain http is explicitly supported.
            var downloader = Downloader(baseUrl, channel);

            foreach (var url in new[] { downloader.BuildLatestReleaseVersionUrl(), downloader.BuildCliDownloadUrl("v1.1292.0") })
            {
                Assert.True(UriExtensions.IsValidWebUrl(url), $"base '{baseUrl}' + channel '{channel}' produced '{url}'");
                Assert.DoesNotContain("//", new Uri(url).AbsolutePath);
            }
        }
    }
}
