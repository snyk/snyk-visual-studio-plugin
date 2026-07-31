using System;
using System.Collections.Generic;
using System.IO;
using Snyk.VisualStudio.Extension.Download;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    /// <summary>
    /// Placing the downloaded binary at its destination. Network-free (unlike
    /// <see cref="SnykCliDownloaderTest"/>).
    ///
    /// The destination is not necessarily the plugin's own AppData folder: snyk-ls reports its own
    /// CLI location (<c>%LocalAppData%\snyk-ls\snyk-win.exe</c>) as <c>cli_path</c> in
    /// $/snyk.configuration, which lands in CliCustomPath — so the folder may not exist, and the
    /// Language Server may be using the binary that is already there.
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
            // The regression: the downloader created its own AppData folder and then copied to the
            // configured path, so a custom destination in a not-yet-existing folder (the snyk-ls one,
            // before the LS has created it) failed the copy with DirectoryNotFoundException — surfaced
            // to the user as "CLI could not be updated. Please check if another process is using ...".
            var destination = Path.Combine(this.workDir, "snyk-ls", "snyk-win.exe");
            Assert.False(Directory.Exists(Path.GetDirectoryName(destination)));

            SnykCliDownloader.PrepareCliDirectory(destination);

            Assert.True(Directory.Exists(Path.GetDirectoryName(destination)));
        }

        [Fact]
        public void PrepareCliDirectory_LeavesAnExistingDirectoryAndItsContentsAlone()
        {
            // Guards against a "recreate the directory" implementation: the destination folder is
            // shared with snyk-ls, which keeps its own files there.
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
            // snyk-ls downloads the same CLI to the same path and may be running it. When the binary
            // already there is the one we would install, there is nothing to update: copying over it
            // only risks a sharing violation with the LS.
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
            // An ACL-denied destination.
            new object[] { new UnauthorizedAccessException("access denied") },
            // SHA256Managed under a FIPS-enforcing Windows policy.
            new object[] { new InvalidOperationException("FIPS validated algorithms required") },
            // The destination locked by the language server mid-scan.
            new object[] { new IOException("file in use") },
        };

        [Theory]
        [MemberData(nameof(ChecksumFailures))]
        public void IsCliUpToDate_ReturnsFalse_WhenTheChecksumCannotBeComputed(Exception failure)
        {
            // "Not verifiable" must never escape — every failure has to resolve to false so the normal
            // download path runs and reports a real error. Catching only IOException let the first two
            // of these abort the download before it was attempted.
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
    }
}
