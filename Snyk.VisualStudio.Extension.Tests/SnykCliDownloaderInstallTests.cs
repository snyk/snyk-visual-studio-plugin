using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Moq;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    /// <summary>
    /// Install and download-decision behaviour, network-free. A test double stands in for the two
    /// release-info requests so the decision paths can be exercised without downloading a CLI.
    /// </summary>
    public class SnykCliDownloaderInstallTests : IDisposable
    {
        private readonly string workDir;
        private readonly Mock<ISnykProgressWorker> progressWorkerMock = new Mock<ISnykProgressWorker>();

        public SnykCliDownloaderInstallTests()
        {
            this.workDir = Path.Combine(Path.GetTempPath(), "snyk-cli-install-tests-" + Guid.NewGuid().ToString("N"));
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

        // Stands in for the two network calls. Throwing variants let the failure paths be exercised.
        private class FakeDownloader : SnykCliDownloader
        {
            private readonly string sha;
            private readonly Exception releaseInfoFailure;

            public FakeDownloader(ISnykOptions options, string sha = null, Exception releaseInfoFailure = null)
                : base(options)
            {
                this.sha = sha;
                this.releaseInfoFailure = releaseInfoFailure;
            }

            public override LatestReleaseInfo GetLatestReleaseInfo()
            {
                if (this.releaseInfoFailure != null)
                {
                    throw this.releaseInfoFailure;
                }

                return new LatestReleaseInfo { Version = "v1.1292.0", Name = "v1.1292.0", Url = this.BuildCliDownloadUrl("v1.1292.0") };
            }

            public override string GetLatestCliSha(string cliDownloadUrl) => this.sha;
        }

        private static ISnykOptions Options(string currentCliVersion = null)
        {
            var options = new Mock<ISnykOptions>();
            options.SetupAllProperties();
            options.Object.CliBaseDownloadURL = SnykCliDownloader.DefaultBaseDownloadUrl;
            options.Object.CliReleaseChannel = SnykCliDownloader.DefaultReleaseChannel;
            options.Object.CurrentCliVersion = currentCliVersion;
            return options.Object;
        }

        [Fact]
        public void InstallCliFile_CopiesIntoADestinationDirectoryThatDoesNotExistYet()
        {
            // snyk-ls reports its own CLI location as cli_path, so the destination folder may not exist.
            var source = Path.Combine(this.workDir, "downloaded.exe");
            File.WriteAllText(source, "cli-binary");
            var destination = Path.Combine(this.workDir, "snyk-ls", "snyk-win.exe");

            SnykCliDownloader.InstallCliFile(source, destination);

            Assert.Equal("cli-binary", File.ReadAllText(destination));
        }

        [Fact]
        public void InstallCliFile_OverwritesAnExistingBinary()
        {
            var source = Path.Combine(this.workDir, "downloaded.exe");
            File.WriteAllText(source, "new-cli");
            var destination = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(destination, "old-cli");

            SnykCliDownloader.InstallCliFile(source, destination);

            Assert.Equal("new-cli", File.ReadAllText(destination));
        }

        [Fact]
        public void InstallCliFile_ThrowsWhenTheDestinationCannotBeWritten()
        {
            var source = Path.Combine(this.workDir, "downloaded.exe");
            File.WriteAllText(source, "cli-binary");
            var destinationIsAnExistingDirectory = Path.Combine(this.workDir, "occupied");
            Directory.CreateDirectory(destinationIsAnExistingDirectory);

            Assert.ThrowsAny<Exception>(() => SnykCliDownloader.InstallCliFile(source, destinationIsAnExistingDirectory));
        }

        [Fact]
        public void InstallAndFinish_PropagatesTheFailureAndDoesNotReportSuccess()
        {
            // The regression this guards: the copy failure used to be swallowed, so FinishDownload never
            // ran and SnykTasksService fired neither DownloadFinished (which starts the language server)
            // nor DownloadFailed — the extension sat in its initializing state with no error and no
            // recovery. Both halves matter: the exception must escape AND the finished callbacks must
            // not run, or the caller is told a failed install succeeded.
            var source = Path.Combine(this.workDir, "downloaded.exe");
            File.WriteAllText(source, "cli-binary");
            var destinationIsAnExistingDirectory = Path.Combine(this.workDir, "occupied");
            Directory.CreateDirectory(destinationIsAnExistingDirectory);

            var finishedCallbackRan = false;
            var cut = new FakeDownloader(Options());

            Assert.ThrowsAny<Exception>(() => cut.InstallAndFinish(
                this.progressWorkerMock.Object,
                source,
                destinationIsAnExistingDirectory,
                new List<SnykCliDownloader.CliDownloadFinishedCallback> { () => finishedCallbackRan = true }));

            Assert.False(finishedCallbackRan, "a failed install must not fire the download-finished callbacks");
            this.progressWorkerMock.Verify(w => w.DownloadFinished(), Times.Never);
        }

        [Fact]
        public void InstallAndFinish_InstallsAndReportsSuccess()
        {
            var source = Path.Combine(this.workDir, "downloaded.exe");
            File.WriteAllText(source, "cli-binary");
            var destination = Path.Combine(this.workDir, "snyk-ls", "snyk-win.exe");

            var finishedCallbackRan = false;
            var cut = new FakeDownloader(Options());

            cut.InstallAndFinish(
                this.progressWorkerMock.Object,
                source,
                destination,
                new List<SnykCliDownloader.CliDownloadFinishedCallback> { () => finishedCallbackRan = true });

            Assert.Equal("cli-binary", File.ReadAllText(destination));
            Assert.True(finishedCallbackRan);
            this.progressWorkerMock.Verify(w => w.DownloadFinished(), Times.Once);
        }

        [Fact]
        public async Task DownloadAsync_SkipsTheDownload_WhenTheBinaryOnDiskAlreadyMatchesAsync()
        {
            // The load-bearing half of the install fix: snyk-ls downloads the same binary to the same
            // path, so when it already matches there is nothing to install — and nothing to fight over.
            // FinishDownload must still run, or the language server is never started.
            var destination = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(destination, "already-the-latest-cli");
            var expectedSha = Sha256.Checksum(destination);
            var lastWriteBefore = File.GetLastWriteTimeUtc(destination);

            var finishedCallbackRan = false;
            var cut = new FakeDownloader(Options(), sha: expectedSha);

            await cut.DownloadAsync(
                this.progressWorkerMock.Object,
                destination,
                "https://downloads.snyk.io/cli/v1.1292.0/snyk-win.exe",
                new List<SnykCliDownloader.CliDownloadFinishedCallback> { () => finishedCallbackRan = true });

            Assert.True(finishedCallbackRan, "FinishDownload must still run, or the language server is never started");
            Assert.Equal("already-the-latest-cli", File.ReadAllText(destination));
            Assert.Equal(lastWriteBefore, File.GetLastWriteTimeUtc(destination));
            this.progressWorkerMock.Verify(w => w.UpdateProgress(100), Times.Once);
        }

        [Fact]
        public void IsCliDownloadNeeded_ReturnsTrue_WhenTheCheckFailsAndNoCliIsInstalled()
        {
            // Reporting "up to date" because the version lookup failed left the extension starting the
            // language server with no CLI at all, and no user-visible signal.
            var missingCli = Path.Combine(this.workDir, "not-installed.exe");
            var cut = new FakeDownloader(Options(), releaseInfoFailure: new InvalidOperationException("network down"));

            Assert.True(cut.IsCliDownloadNeeded(missingCli));
        }

        [Fact]
        public void IsCliDownloadNeeded_ReturnsFalse_WhenTheCheckFailsButACliIsInstalled()
        {
            // Offline with a usable CLI already on disk: keep using it rather than forcing a doomed
            // download that would surface as an error banner on every startup.
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "an-existing-cli");
            var cut = new FakeDownloader(Options(), releaseInfoFailure: new InvalidOperationException("network down"));

            Assert.False(cut.IsCliDownloadNeeded(installedCli));
        }

        [Fact]
        public void IsCliDownloadNeeded_ReturnsFalse_WhenTheInstalledVersionIsCurrent()
        {
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "an-existing-cli");
            var cut = new FakeDownloader(Options(currentCliVersion: "v1.1292.0"));

            Assert.False(cut.IsCliDownloadNeeded(installedCli));
        }

        [Fact]
        public void IsCliDownloadNeeded_ReturnsTrue_WhenANewerVersionIsAvailable()
        {
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "an-existing-cli");
            var cut = new FakeDownloader(Options(currentCliVersion: "v1.1000.0"));

            Assert.True(cut.IsCliDownloadNeeded(installedCli));
        }
    }
}
