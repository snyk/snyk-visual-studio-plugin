using System;
using System.Collections.Generic;
using System.IO;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Download;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    /// <summary>
    /// Placing the downloaded binary at its destination — which is the configured path, so the folder
    /// may not exist and the language server may be using the binary already there.
    /// </summary>
    public class SnykCliDownloaderDestinationTests : IDisposable
    {
        private readonly string workDir;

        public SnykCliDownloaderDestinationTests()
        {
            this.workDir = Path.Combine(Path.GetTempPath(), "snyk-cli-dest-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(this.workDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(this.workDir))
                {
                    Directory.Delete(this.workDir, recursive: true);
                }
            }
            catch (IOException)
            {
                // Best-effort cleanup.
            }
        }

        [Fact]
        public void PrepareCliDirectory_CreatesTheDestinationDirectory_WhenItDoesNotExist()
        {
            var destination = Path.Combine(this.workDir, "snyk-ls", "snyk-win.exe");
            Assert.False(Directory.Exists(Path.GetDirectoryName(destination)));

            SnykCliDownloader.PrepareCliDirectory(destination);

            Assert.True(Directory.Exists(Path.GetDirectoryName(destination)));
        }

        [Fact]
        public void PrepareCliDirectory_LeavesAnExistingDirectoryAndItsContentsAlone()
        {
            // The destination folder is shared with the language server, which keeps its own files there.
            var destination = Path.Combine(this.workDir, "snyk-win.exe");
            var neighbouringFile = Path.Combine(this.workDir, "snyk-ls-owned.txt");
            File.WriteAllText(neighbouringFile, "do not delete me");

            SnykCliDownloader.PrepareCliDirectory(destination);
            SnykCliDownloader.PrepareCliDirectory(destination); // idempotent

            Assert.True(Directory.Exists(this.workDir));
            Assert.Equal("do not delete me", File.ReadAllText(neighbouringFile));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("snyk-win.exe")] // no directory component
        public void PrepareCliDirectory_DoesNotThrow_ForPathsWithoutADirectory(string destination)
        {
            SnykCliDownloader.PrepareCliDirectory(destination);
        }

        [Fact]
        public void IsCliUpToDate_TrueWhenTheFileOnDiskMatchesTheExpectedChecksum()
        {
            var destination = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(destination, "cli-binary-contents");
            var sha = Sha256.Checksum(destination);

            Assert.True(SnykCliDownloader.IsCliUpToDate(destination, sha));
            Assert.True(SnykCliDownloader.IsCliUpToDate(destination, sha.ToLowerInvariant()));
        }

        [Fact]
        public void IsCliUpToDate_FalseWhenTheChecksumDiffers()
        {
            var destination = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(destination, "an older cli");

            Assert.False(SnykCliDownloader.IsCliUpToDate(destination, Sha256.ComputeHash("something else")));
        }

        [Fact]
        public void IsCliUpToDate_FalseWhenTheFileIsMissing()
        {
            var destination = Path.Combine(this.workDir, "does-not-exist.exe");

            Assert.False(SnykCliDownloader.IsCliUpToDate(destination, Sha256.ComputeHash("anything")));
        }

        public static IEnumerable<object[]> ChecksumFailures() => new[]
        {
            new object[] { new UnauthorizedAccessException("access denied") },
            new object[] { new InvalidOperationException("FIPS validated algorithms required") },
            new object[] { new IOException("file in use") },
        };

        [Theory]
        [MemberData(nameof(ChecksumFailures))]
        public void IsCliUpToDate_ReturnsFalse_WhenTheChecksumCannotBeComputed(Exception failure)
        {
            // Any failure means "not verifiable" and must resolve to false, not escape.
            var destination = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(destination, "cli-binary-contents");
            var sha = Sha256.Checksum(destination);

            var result = SnykCliDownloader.IsCliUpToDate(destination, sha, _ => throw failure);

            Assert.False(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsCliUpToDate_FalseWhenTheExpectedChecksumIsUnknown(string expectedSha)
        {
            var destination = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(destination, "cli-binary-contents");

            Assert.False(SnykCliDownloader.IsCliUpToDate(destination, expectedSha));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        // Blank, not just empty: the settings page stores whatever was typed, and a whitespace-only
        // custom path would otherwise resolve to itself — File.Exists("   ") is always false, so the
        // CLI reads as missing and the download lands somewhere unusable.
        [InlineData("   ")]
        [InlineData("\t")]
        public void GetCliFilePath_FallsBackToTheDefault_WhenNoCustomPathIsConfigured(string customCliPath)
        {
            Assert.Equal(SnykCli.GetSnykCliDefaultPath(), SnykCli.GetCliFilePath(customCliPath));
        }

        [Theory]
        [InlineData(@"C:\custom\snyk.exe", @"C:\custom\snyk.exe")]
        // Surrounding whitespace is not part of the path, and a trailing space makes it unopenable.
        [InlineData(@"  C:\custom\snyk.exe  ", @"C:\custom\snyk.exe")]
        public void GetCliFilePath_UsesAConfiguredPath(string customCliPath, string expected)
        {
            Assert.Equal(expected, SnykCli.GetCliFilePath(customCliPath));
        }
    }
}
