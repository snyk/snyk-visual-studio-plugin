using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Snyk.VisualStudio.Extension;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Integration.Tests
{
    // Downloads a real snyk-win.exe (same network call SnykCliDownloaderTest already makes against
    // static.snyk.io) so SnykCliDownloader.IsCliProtocolSupported's real process spawn and stdout
    // parsing is exercised end-to-end, not just the canned-bool override SnykCliDownloaderInstallTests
    // uses. A .bat fixture can't stand in for this: IsCliProtocolSupported uses UseShellExecute=false
    // (required for RedirectStandardOutput), and Windows refuses to launch a .bat that way
    // (Win32Exception, "not a valid Win32 application").
    //
    // Lives here (not Snyk.VisualStudio.Extension.Tests) because it downloads a ~150MB binary and spawns
    // it as a real process 2-3x per test: under load that destabilized unrelated fast unit tests sharing
    // the same test-host run (a Microsoft.VisualStudio.Sdk.TestFramework fixture race). [Trait("integration",
    // "true")] matches ExtensionStartupTests's convention, so pr-workflow.yml's filter excludes these from
    // the fast PR run and integration-tests.yml picks them up on its own dedicated pass.
    public class CliProtocolSupportedRealCliTests
    {
        private readonly Mock<ISnykOptions> optionsMock;

        public CliProtocolSupportedRealCliTests()
        {
            optionsMock = new Mock<ISnykOptions>();
            optionsMock.Setup(x => x.CliBaseDownloadURL).Returns(SnykCliDownloader.DefaultBaseDownloadUrl);
            optionsMock.Setup(x => x.CliReleaseChannel).Returns("preview");
        }

        [Trait("integration", "true")]
        [Fact]
        public async Task IsCliProtocolSupported_RealCli_AndProtocolVersionMatches()
        {
            // Own file name (not SnykCli.CliFileName) so this doesn't collide with the other CLI
            // downloader tests that share that fixed name.
            var tempCliPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}-snyk-win.exe");
            try
            {
                var cliDownloader = new SnykCliDownloader(optionsMock.Object);
                await cliDownloader.DownloadAsync(new Mock<ISnykProgressWorker>().Object, tempCliPath);
                Assert.True(File.Exists(tempCliPath));

                // Independent sanity check that the download+spawn pipeline genuinely works. Not
                // asserted against a specific value: the "preview" channel's latest CLI is fetched via
                // the pointer keyed to LsConstants.ProtocolVersion (BuildLatestReleaseVersionUrl), so by
                // construction it should report that same version - which is exactly what
                // IsCliProtocolSupported below verifies, using the real implementation instead of
                // duplicating the comparison here.
                var actualProtocolVersion = RunProtocolVersionProbe(tempCliPath);
                Assert.False(string.IsNullOrWhiteSpace(actualProtocolVersion));

                Assert.True(cliDownloader.IsCliProtocolSupported(tempCliPath));
            }
            finally
            {
                DeleteFileTolerant(tempCliPath);
            }
        }

        // Downloads a real CLI pinned to protocol 24 (superseded by 25, see IDE-2404) and asserts the
        // mismatch is real, not just a canned override returning false. Snyk's CDN keeps a
        // per-protocol-version pointer file - the same mechanism GetLatestReleaseInfo uses for the
        // CURRENT protocol version (LsConstants.ProtocolVersion, baked into LatestReleaseVersionUrlScheme)
        // - so an older pointer can be fetched directly for an old, still-hosted release.
        //
        // This binary predates the --protocolVersion flag itself: it doesn't report "24", it doesn't
        // recognize the flag and dumps CLI help text instead - exactly the "CLI help" case seen in the
        // real IDE-2404 repro log. That's still a genuine mismatch (help text != "25"), just not via the
        // literal version string, so this asserts the mismatch rather than a specific reported value.
        [Trait("integration", "true")]
        [Fact]
        public async Task IsCliProtocolSupported_RealCli_AndOlderLsProtocolVersionDoesntMatch()
        {
            var tempCliPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}-snyk-win.exe");
            try
            {
                var cliDownloader = new SnykCliDownloader(optionsMock.Object);
                var oldVersionPointerUrl =
                    $"{SnykCliDownloader.ResolveBaseDownloadUrl(optionsMock.Object.CliBaseDownloadURL)}" +
                    $"/cli/{SnykCliDownloader.ResolveReleaseChannel(optionsMock.Object.CliReleaseChannel)}" +
                    "/ls-protocol-version-24";

                string oldVersionTag;
                using (var webClient = new SnykWebClient())
                {
                    oldVersionTag = "v" + webClient.DownloadString(oldVersionPointerUrl).Replace("\n", string.Empty).Trim();
                }

                // Mirrors SnykCliDownloader's internal LatestReleaseDownloadUrlScheme ({base}/cli/{version}/
                // snyk-win.exe) using only public members - BuildCliDownloadUrl itself is internal, and
                // InternalsVisibleTo only reaches Snyk.VisualStudio.Extension.Tests and Integration.Tests
                // as whole assemblies, which is enough for the type but not worth relying on here too.
                var oldCliDownloadUrl =
                    $"{SnykCliDownloader.ResolveBaseDownloadUrl(optionsMock.Object.CliBaseDownloadURL)}" +
                    $"/cli/{oldVersionTag}/{SnykCli.CliFileName}";
                await cliDownloader.DownloadAsync(new Mock<ISnykProgressWorker>().Object, tempCliPath, oldCliDownloadUrl);
                Assert.True(File.Exists(tempCliPath));

                // Ground truth, obtained independently of the method under test. Not asserted against a
                // specific value - see comment above - just used to confirm it's not "25".
                var actualProtocolVersion = RunProtocolVersionProbe(tempCliPath);
                Assert.NotEqual(LsConstants.ProtocolVersion, actualProtocolVersion);

                Assert.False(cliDownloader.IsCliProtocolSupported(tempCliPath));
            }
            finally
            {
                DeleteFileTolerant(tempCliPath);
            }
        }

        // Deliberately independent of SnykCliDownloader.IsCliProtocolSupported: if this test called into
        // the same method it would just be checking the code against itself.
        private static string RunProtocolVersionProbe(string cliPath)
        {
            var info = new ProcessStartInfo
            {
                FileName = cliPath,
                Arguments = "language-server --protocolVersion",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = Process.Start(info))
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Trim();
            }
        }

        // Windows can briefly keep the just-exited exe's image locked (AV scan / image-section
        // teardown) even after Process.WaitForExit returns, so an immediate File.Delete can throw
        // UnauthorizedAccessException/IOException. That's a cleanup nuance, not a test failure — retry
        // with backoff, then give up quietly (a leftover temp file is harmless; %TEMP% gets reaped).
        private static void DeleteFileTolerant(string path)
        {
            for (var attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }

                    return;
                }
                catch (IOException) when (attempt < 5)
                {
                    Thread.Sleep(200);
                }
                catch (UnauthorizedAccessException) when (attempt < 5)
                {
                    Thread.Sleep(200);
                }
            }
        }
    }
}
