using System;
using System.IO;
using Snyk.VisualStudio.Extension.CLI;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class CliProtocolVersionVerifierTest : IDisposable
    {
        private readonly string existingCliPath;

        public CliProtocolVersionVerifierTest()
        {
            // IsCompatible checks File.Exists before ever invoking the runner, so tests that need to
            // reach the runner need a real (content irrelevant) file on disk.
            existingCliPath = Path.GetTempFileName();
        }

        public void Dispose()
        {
            if (File.Exists(existingCliPath))
            {
                File.Delete(existingCliPath);
            }
        }

        [Fact]
        public void IsCompatible_ReturnsFalse_WhenCliPathDoesNotExist()
        {
            // Runner returns a matching "25" on purpose: if File.Exists were skipped/broken, this
            // would flip to True, so a False here is unambiguously the missing-file branch, not a
            // version mismatch.
            var result = CliProtocolVersionVerifier.IsCompatible(
                @"C:\does\not\exist\snyk-win.exe", "25", _ => "25");

            Assert.False(result);
        }

        [Fact]
        public void IsCompatible_ReturnsFalse_WhenCliPathIsNullOrEmpty()
        {
            Assert.False(CliProtocolVersionVerifier.IsCompatible(null, "25", _ => "25"));
            Assert.False(CliProtocolVersionVerifier.IsCompatible(string.Empty, "25", _ => "25"));
        }

        [Fact]
        public void IsCompatible_ReturnsTrue_WhenReportedVersionMatchesRequired()
        {
            Assert.True(CliProtocolVersionVerifier.IsCompatible(existingCliPath, "25", _ => "25"));
        }

        [Fact]
        public void IsCompatible_ReturnsFalse_WhenReportedVersionDoesNotMatchRequired()
        {
            Assert.False(CliProtocolVersionVerifier.IsCompatible(existingCliPath, "25", _ => "24"));
        }

        [Fact]
        public void IsCompatible_ReturnsTrue_WhenReportedVersionIsDevelopmentSentinel()
        {
            // snyk-ls reports "development" for local/dev builds; always treated as compatible so
            // engineers building the LS from source aren't blocked by this gate.
            Assert.True(CliProtocolVersionVerifier.IsCompatible(
                existingCliPath, "25", _ => CliProtocolVersionVerifier.DevelopmentProtocolVersion));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsCompatible_ReturnsFalse_WhenRunnerReturnsNoUsableOutput(string reported)
        {
            Assert.False(CliProtocolVersionVerifier.IsCompatible(existingCliPath, "25", _ => reported));
        }

        [Fact]
        public void IsCompatible_ReturnsFalse_WhenRunnerThrows()
        {
            Assert.False(CliProtocolVersionVerifier.IsCompatible(
                existingCliPath, "25", _ => throw new InvalidOperationException("boom")));
        }

        [Fact]
        public void IsCompatible_TrimsWhitespaceAroundReportedVersion()
        {
            Assert.True(CliProtocolVersionVerifier.IsCompatible(existingCliPath, "25", _ => "25\r\n"));
        }
    }
}
