using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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
    // Downloads and spawns real CLI binaries, so these tests run only in the integration suite.
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
        public async Task CheckCliProtocol_RealCli_AndProtocolVersionMatches()
        {
            var tempCliPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}-snyk-win.exe");
            try
            {
                var cliDownloader = new SnykCliDownloader(optionsMock.Object);
                await cliDownloader.DownloadAsync(new Mock<ISnykProgressWorker>().Object, tempCliPath);
                Assert.True(File.Exists(tempCliPath));

                // Verify the fixture independently before exercising the implementation under test.
                var actualProtocolVersion = RunProtocolVersionProbe(tempCliPath);
                Assert.False(string.IsNullOrWhiteSpace(actualProtocolVersion));

                Assert.Equal(CliProtocolCheckResult.Supported, cliDownloader.CheckCliProtocol(tempCliPath));
            }
            finally
            {
                DeleteFileTolerant(tempCliPath);
            }
        }

        // Snyk's CDN exposes a pointer per protocol version. The protocol-24 CLI predates
        // --protocolVersion and prints help text, which is still a conclusive mismatch.
        [Trait("integration", "true")]
        [Fact]
        public async Task CheckCliProtocol_RealCli_AndOlderLsProtocolVersionDoesntMatch()
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

                // Mirror the production download URL shape for the pinned release.
                var oldCliDownloadUrl =
                    $"{SnykCliDownloader.ResolveBaseDownloadUrl(optionsMock.Object.CliBaseDownloadURL)}" +
                    $"/cli/{oldVersionTag}/{SnykCli.CliFileName}";
                await cliDownloader.DownloadAsync(new Mock<ISnykProgressWorker>().Object, tempCliPath, oldCliDownloadUrl);
                Assert.True(File.Exists(tempCliPath));

                var actualProtocolVersion = RunProtocolVersionProbe(tempCliPath);
                Assert.NotEqual(LsConstants.ProtocolVersion, actualProtocolVersion);

                Assert.Equal(CliProtocolCheckResult.Unsupported, cliDownloader.CheckCliProtocol(tempCliPath));
            }
            finally
            {
                DeleteFileTolerant(tempCliPath);
            }
        }

        // Keep this probe independent of the implementation under test. Drain stdout before waiting so
        // help text cannot fill the pipe and block the child.
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

            var output = new StringBuilder();

            using (var process = new Process { StartInfo = info, EnableRaisingEvents = true })
            {
                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        output.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();

                if (!process.WaitForExit(SnykCliDownloader.ProtocolProbeTimeoutMs))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                        // The process may already have exited.
                    }

                    throw new TimeoutException($"CLI at {cliPath} did not respond to --protocolVersion in time.");
                }

                // On .NET Framework, the infinite overload flushes asynchronous output callbacks.
                process.WaitForExit();

                return output.ToString().Trim();
            }
        }

        // Windows can briefly keep an exited executable locked, so cleanup tolerates transient failures.
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
