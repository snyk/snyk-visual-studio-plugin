using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    /// <summary>
    /// Install and download-decision behaviour. A test double replaces the two network calls.
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

        private class FakeDownloader : SnykCliDownloader
        {
            private readonly string sha;
            private readonly Exception releaseInfoFailure;
            private readonly bool protocolSupported;

            public FakeDownloader(ISnykOptions options, string sha = null, Exception releaseInfoFailure = null, bool protocolSupported = true)
                : base(options)
            {
                this.sha = sha;
                this.releaseInfoFailure = releaseInfoFailure;
                this.protocolSupported = protocolSupported;
            }

            // The fixtures are text files, so the real probe would try to execute them.
            public int ProtocolProbes { get; private set; }

            internal override bool IsCliProtocolSupported(string cliFilePath)
            {
                this.ProtocolProbes++;

                return this.protocolSupported;
            }

            // Counts attempted round-trips — incremented before any injected failure, so a test can
            // tell "never asked" from "asked and it blew up".
            public int ReleaseInfoFetches { get; private set; }

            public override LatestReleaseInfo GetLatestReleaseInfo()
            {
                this.ReleaseInfoFetches++;

                if (this.releaseInfoFailure != null)
                {
                    throw this.releaseInfoFailure;
                }

                return new LatestReleaseInfo { Version = "v1.1292.0", Name = "v1.1292.0", Url = this.BuildCliDownloadUrl("v1.1292.0") };
            }

            public override string GetLatestCliSha(string cliDownloadUrl) => this.sha;

            // Records whether the failure was diagnosed as a CLI install failure. The real
            // implementation reports through NotificationService.Instance, a static singleton that is
            // null under test, so overriding is the only way to observe it.
            public int InstallFailuresReported { get; private set; }

            internal override void ReportInstallFailure(Exception e, string cliFileDestinationPath) =>
                this.InstallFailuresReported++;
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
            // The configured destination folder may not exist.
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

        // The atomicity of the replace itself is not reachable from a unit test — it needs the process
        // to die mid-write. It comes from File.Replace being a single filesystem operation rather than
        // a streamed overwrite. What the three tests below pin is the staging mechanics around it:
        // cleanup on both paths, and that a first install does not go through File.Replace (which
        // throws without an existing destination).

        [Fact]
        public void InstallCliFile_LeavesNoStagingFileBehind_WhenReplacingAnExistingBinary()
        {
            var source = Path.Combine(this.workDir, "downloaded.exe");
            File.WriteAllText(source, "new-cli");
            var destination = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(destination, "old-cli");

            SnykCliDownloader.InstallCliFile(source, destination);

            Assert.Equal("new-cli", File.ReadAllText(destination));
            Assert.Empty(Directory.GetFiles(this.workDir, "*.new"));
            Assert.Equal(
                new[] { "downloaded.exe", "snyk-win.exe" },
                Directory.GetFiles(this.workDir).Select(Path.GetFileName).OrderBy(n => n).ToArray());
        }

        [Fact]
        public void InstallCliFile_DoesNotTouchTheExistingBinary_WhenTheSourceIsMissing()
        {
            // Also passes on a plain File.Copy, which throws before opening the destination. Kept
            // because the staging path has more ways to go wrong: it must not leave the destination
            // half-replaced, and must not leave the staging file behind either.
            var missingSource = Path.Combine(this.workDir, "never-downloaded.exe");
            var destination = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(destination, "old-cli");

            Assert.ThrowsAny<Exception>(() => SnykCliDownloader.InstallCliFile(missingSource, destination));

            Assert.Equal("old-cli", File.ReadAllText(destination));
            Assert.Empty(Directory.GetFiles(this.workDir, "*.new"));
        }

        [Fact]
        public void InstallCliFile_InstallsWhenNoBinaryIsPresentYet()
        {
            // First install: File.Replace throws FileNotFoundException without an existing
            // destination, so this path must not go through it.
            var source = Path.Combine(this.workDir, "downloaded.exe");
            File.WriteAllText(source, "new-cli");
            var destination = Path.Combine(this.workDir, "fresh", "snyk-win.exe");

            SnykCliDownloader.InstallCliFile(source, destination);

            Assert.Equal("new-cli", File.ReadAllText(destination));
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(destination), "*.new"));
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
            // Both halves matter: the exception must escape, and the finished callbacks — which start
            // the language server — must not run.
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
        public void InstallAndFinish_DoesNotReportAnInstallFailure_WhenACallbackThrows()
        {
            // A callback throwing is the language server failing to start, not the CLI failing to
            // install — the copy has already succeeded by then, so it must not be diagnosed as a copy
            // error. The exception still reaches the caller as-is.
            var source = Path.Combine(this.workDir, "downloaded.exe");
            File.WriteAllText(source, "cli-binary");
            var destination = Path.Combine(this.workDir, "snyk-ls", "snyk-win.exe");

            var cut = new FakeDownloader(Options());

            var thrown = Assert.Throws<InvalidOperationException>(() => cut.InstallAndFinish(
                this.progressWorkerMock.Object,
                source,
                destination,
                new List<SnykCliDownloader.CliDownloadFinishedCallback>
                {
                    () => { throw new InvalidOperationException("language server failed to start"); },
                }));

            Assert.Equal(0, cut.InstallFailuresReported);
            Assert.Equal("language server failed to start", thrown.Message);
            Assert.Equal("cli-binary", File.ReadAllText(destination));
            this.progressWorkerMock.Verify(w => w.DownloadFinished(), Times.Never);
        }

        [Fact]
        public void InstallAndFinish_ReportsAnInstallFailure_WhenTheCopyFails()
        {
            // The other half of the pair above: a genuine copy failure must still be diagnosed.
            var source = Path.Combine(this.workDir, "downloaded.exe");
            File.WriteAllText(source, "cli-binary");
            var destinationIsAnExistingDirectory = Path.Combine(this.workDir, "occupied");
            Directory.CreateDirectory(destinationIsAnExistingDirectory);

            var cut = new FakeDownloader(Options());

            Assert.ThrowsAny<Exception>(() => cut.InstallAndFinish(
                this.progressWorkerMock.Object,
                source,
                destinationIsAnExistingDirectory,
                new List<SnykCliDownloader.CliDownloadFinishedCallback>()));

            Assert.Equal(1, cut.InstallFailuresReported);
        }

        [Fact]
        public async Task DownloadAsync_SkipsTheDownload_WhenTheBinaryOnDiskAlreadyMatchesAsync()
        {
            // Nothing to install when the binary already matches, but the finished callbacks must still
            // run or the language server is never started.
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
        public void IsCliDownloadNeeded_ReturnsTrue_WithoutAskingTheNetwork_WhenNoCliIsInstalled()
        {
            // Short-circuits on the missing file, so the version check never runs — worth pinning,
            // because it means a first install does not depend on the network being up. The injected
            // failure would fire if the order ever changed.
            var missingCli = Path.Combine(this.workDir, "not-installed.exe");
            var cut = new FakeDownloader(Options(), releaseInfoFailure: new InvalidOperationException("network down"));

            Assert.True(cut.IsCliDownloadNeeded(missingCli));
            Assert.Equal(0, cut.ReleaseInfoFetches);

            // Nor is there any point launching a binary that is not there.
            Assert.Equal(0, cut.ProtocolProbes);
        }

        [Fact]
        public void IsCliDownloadNeeded_ReturnsFalse_WhenTheCheckFailsButACliIsInstalled()
        {
            // Offline with a usable CLI: keep using it rather than failing on every startup. This is
            // the test that reaches the catch — the file exists, so the version check runs and throws.
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "an-existing-cli");
            var cut = new FakeDownloader(Options(), releaseInfoFailure: new InvalidOperationException("network down"));

            Assert.False(cut.IsCliDownloadNeeded(installedCli));

            // Proves the fallback was exercised rather than short-circuited past.
            Assert.Equal(1, cut.ReleaseInfoFetches);
        }

        [Fact]
        public void IsCliDownloadNeeded_ReturnsFalse_WhenTheInstalledVersionIsCurrent()
        {
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "an-existing-cli");
            var cut = new FakeDownloader(Options(currentCliVersion: "v1.1292.0"));

            Assert.False(cut.IsCliDownloadNeeded(installedCli));

            // A matching version string is not evidence the binary works, so it is also probed.
            Assert.Equal(1, cut.ProtocolProbes);
        }

        [Fact]
        public void IsCliDownloadNeeded_ReturnsTrue_WhenThePresentCliCannotReportTheProtocolVersion()
        {
            // The case File.Exists plus a version string cannot see: right path, right recorded
            // version, unusable binary. Previously this started a language server that could only fail.
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "a-truncated-or-stale-cli");
            var cut = new FakeDownloader(Options(currentCliVersion: "v1.1292.0"), protocolSupported: false);

            Assert.True(cut.IsCliDownloadNeeded(installedCli));
            Assert.Equal(1, cut.ProtocolProbes);
        }

        [Fact]
        public async Task AutoUpdateCliAsync_FiresTheFinishedCallbacks_WhenNoDownloadIsNeededAsync()
        {
            // The callbacks start the language server, record the installed version and raise
            // DownloadFinished, which is what moves the tool window off "Snyk Security is loading...".
            // Taking the nothing-to-do path must not skip them.
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "an-up-to-date-cli");

            var finishedCallbackRan = false;
            var cut = new FakeDownloader(Options(currentCliVersion: "v1.1292.0"));

            await cut.AutoUpdateCliAsync(
                this.progressWorkerMock.Object,
                installedCli,
                new List<SnykCliDownloader.CliDownloadFinishedCallback> { () => finishedCallbackRan = true });

            Assert.True(finishedCallbackRan, "the finished callbacks must run even when no download was needed");
            this.progressWorkerMock.Verify(w => w.DownloadFinished(), Times.Once);
            this.progressWorkerMock.VerifySet(w => w.IsWorkFinished = true, Times.Once());

            // Nothing was fetched: the binary already on disk is the one we want.
            Assert.Equal("an-up-to-date-cli", File.ReadAllText(installedCli));
        }

        [Fact]
        public void IsCliDownloadNeeded_DoesNotProbe_WhenANewerVersionIsAlreadyKnown()
        {
            // Downloading anyway, so spending ~a second launching the old binary buys nothing.
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "an-existing-cli");
            var cut = new FakeDownloader(Options(currentCliVersion: "v1.1000.0"));

            Assert.True(cut.IsCliDownloadNeeded(installedCli));
            Assert.Equal(0, cut.ProtocolProbes);
        }

        [Fact]
        public void IsCliDownloadNeeded_ReturnsTrue_WhenANewerVersionIsAvailable()
        {
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "an-existing-cli");
            var cut = new FakeDownloader(Options(currentCliVersion: "v1.1000.0"));

            Assert.True(cut.IsCliDownloadNeeded(installedCli));
        }

        [Fact]
        public void GetLatestReleaseInfoOnce_FetchesOncePerDownloader()
        {
            // One update asked three times: the version probe, the log line in DownloadAsync, and the
            // callback that persists CurrentCliVersion. Those answers need not agree — a release
            // published mid-update meant installing one version and recording the next, after which
            // the version check reports "current" and the install never moves off the older binary.
            var cut = new FakeDownloader(Options());

            var first = cut.GetLatestReleaseInfoOnce();
            var second = cut.GetLatestReleaseInfoOnce();

            Assert.Equal(1, cut.ReleaseInfoFetches);
            Assert.Same(first, second);
        }

        [Fact]
        public void IsCliDownloadNeeded_ThenDownloadAsync_ShareOneReleaseInfoFetch()
        {
            var destination = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(destination, "an-existing-cli");
            var cut = new FakeDownloader(Options(currentCliVersion: "v1.1000.0"));

            Assert.True(cut.IsCliDownloadNeeded(destination));
            Assert.Equal(1, cut.ReleaseInfoFetches);

            // The second consumer of the same information must not re-ask.
            Assert.Equal("v1.1292.0", cut.GetLatestReleaseInfoOnce().Name);
            Assert.Equal(1, cut.ReleaseInfoFetches);
        }
    }
}
