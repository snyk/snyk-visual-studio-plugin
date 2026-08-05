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
            private readonly Func<string, string> computeChecksum;

            public FakeDownloader(
                ISnykOptions options,
                string sha = null,
                Exception releaseInfoFailure = null,
                bool protocolSupported = false,
                Func<string, string> computeChecksum = null)
                : base(options)
            {
                this.sha = sha;
                this.releaseInfoFailure = releaseInfoFailure;
                this.protocolSupported = protocolSupported;
                this.computeChecksum = computeChecksum;
            }

            internal override string ComputeChecksum(string filePath) =>
                this.computeChecksum != null ? this.computeChecksum(filePath) : base.ComputeChecksum(filePath);

            // Round-trips for the checksum, counted separately from the version lookup: the download
            // decision now consults both, and each must be memoised independently.
            public int ShaFetches { get; private set; }

            // Launches of the CLI. Asserted at zero on the happy path — the checksum already proves the
            // binary is the current protocol-keyed release, so paying for a process launch is a defect.
            public int ProtocolProbes { get; private set; }

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

            public override string GetLatestCliSha(string cliDownloadUrl)
            {
                this.ShaFetches++;

                return this.sha;
            }

            internal override bool IsCliProtocolSupported(string cliFilePath)
            {
                this.ProtocolProbes++;

                return this.protocolSupported;
            }

            // Records whether the failure was diagnosed as a CLI install failure. The real
            // implementation reports through NotificationService.Instance, a static singleton that is
            // null under test, so overriding is the only way to observe it.
            public int InstallFailuresReported { get; private set; }

            // What the failure was told about the destination's prior state. Null until reported.
            public bool? ReportedPriorCliExisted { get; private set; }

            internal override void ReportInstallFailure(Exception e, string cliFileDestinationPath, bool priorCliExisted)
            {
                this.InstallFailuresReported++;
                this.ReportedPriorCliExisted = priorCliExisted;
            }
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
            this.progressWorkerMock.Verify(w => w.DownloadFinished(It.IsAny<bool>()), Times.Never);
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
            this.progressWorkerMock.Verify(w => w.DownloadFinished(true), Times.Once);

            // The caller's finally gates disposal of the cancellation token source and TaskFinished on
            // this, so an actual install must set it too — not only the nothing-to-do path.
            this.progressWorkerMock.VerifySet(w => w.IsWorkFinished = true, Times.Once());
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
            this.progressWorkerMock.Verify(w => w.DownloadFinished(It.IsAny<bool>()), Times.Never);
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
        public void InstallAndFinish_ReportsAFailedFirstInstallAsAnInstall_NotAnUpdate()
        {
            // A destination that is an existing directory fails the copy while making File.Exists
            // return true — the shape that made the after-the-fact probe call a first install an
            // update. What matters is the state BEFORE the attempt: there was no CLI there.
            var source = Path.Combine(this.workDir, "downloaded.exe");
            File.WriteAllText(source, "cli-binary");
            var destinationIsAnExistingDirectory = Path.Combine(this.workDir, "occupied-fresh");
            Directory.CreateDirectory(destinationIsAnExistingDirectory);

            var cut = new FakeDownloader(Options());

            Assert.ThrowsAny<Exception>(() => cut.InstallAndFinish(
                this.progressWorkerMock.Object,
                source,
                destinationIsAnExistingDirectory,
                new List<SnykCliDownloader.CliDownloadFinishedCallback>()));

            Assert.False(cut.ReportedPriorCliExisted);
        }

        [Fact]
        public void InstallAndFinish_ReportsAFailedUpdateAsAnUpdate()
        {
            // A real prior binary, and a source that cannot be read, so the install fails after the
            // destination genuinely existed.
            var missingSource = Path.Combine(this.workDir, "never-downloaded.exe");
            var destination = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(destination, "the-previous-cli");

            var cut = new FakeDownloader(Options());

            Assert.ThrowsAny<Exception>(() => cut.InstallAndFinish(
                this.progressWorkerMock.Object,
                missingSource,
                destination,
                new List<SnykCliDownloader.CliDownloadFinishedCallback>()));

            Assert.True(cut.ReportedPriorCliExisted);
        }

        [Fact]
        public void BuildInstallFailureMessage_SaysInstalled_WhenNoCliWasThereBefore()
        {
            // Asserted on the real message, not through the FakeDownloader override: every other test
            // here replaces ReportInstallFailure, so without this the text the user actually reads was
            // never exercised. "Previously installed" for a first install is the falsehood being ruled
            // out — a partial write left by the failure makes File.Exists say otherwise.
            var message = SnykCliDownloader.BuildInstallFailureMessage(
                new IOException("disk full"), @"C:\cli\snyk-win.exe", priorCliExisted: false);

            Assert.Equal(@"Snyk CLI could not be installed at C:\cli\snyk-win.exe: disk full", message);
            Assert.DoesNotContain("previously installed", message);
            Assert.DoesNotContain("updated", message);
        }

        [Fact]
        public void BuildInstallFailureMessage_SaysUpdated_WhenACliWasThereBefore()
        {
            var message = SnykCliDownloader.BuildInstallFailureMessage(
                new IOException("locked"), @"C:\cli\snyk-win.exe", priorCliExisted: true);

            Assert.Equal(
                @"Snyk CLI could not be updated at C:\cli\snyk-win.exe: locked The previously installed CLI is still in place.",
                message);
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

            // Nothing was fetched, so the status bar must not claim a successful download. Asserting
            // the flag and not just the call: it defaulted to true, which is how this path came to
            // report "Snyk CLI downloaded successfully" for a download that never happened.
            this.progressWorkerMock.Verify(w => w.DownloadFinished(false), Times.Once);
            this.progressWorkerMock.Verify(w => w.DownloadFinished(true), Times.Never);
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

        }

        [Fact]
        public void IsCliDownloadNeeded_ReturnsFalse_WhenTheCheckFailsButTheInstalledCliSpeaksOurProtocol()
        {
            // Offline with a usable CLI: keep using it rather than failing on every startup. Which
            // release is current is unknowable here, so the only question left is the one that needs no
            // network — does what we have speak our protocol version?
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "an-existing-cli");
            var cut = new FakeDownloader(
                Options(),
                releaseInfoFailure: new InvalidOperationException("network down"),
                protocolSupported: true);

            Assert.False(cut.IsCliDownloadNeeded(installedCli));

            // Proves the catch was reached rather than short-circuited past, and that the probe is what
            // decided it.
            Assert.Equal(1, cut.ReleaseInfoFetches);
            Assert.Equal(1, cut.ProtocolProbes);
        }

        [Fact]
        public void IsCliDownloadNeeded_ReturnsTrue_WhenTheCheckFailsAndTheInstalledCliDoesNotSpeakOurProtocol()
        {
            // The case that used to be silently survivable: offline AND holding a CLI that cannot work.
            // Returning false here kept a dead language server for the rest of the session, and every
            // later startup repeated the decision. Asking for the download will very likely fail too —
            // that is the point, because it surfaces instead of hanging.
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "a-cli-from-an-older-protocol");
            var cut = new FakeDownloader(
                Options(),
                releaseInfoFailure: new InvalidOperationException("network down"),
                protocolSupported: false);

            Assert.True(cut.IsCliDownloadNeeded(installedCli));
            Assert.Equal(1, cut.ProtocolProbes);
        }

        [Fact]
        public void IsCliDownloadNeeded_ReturnsFalse_WhenTheBytesOnDiskMatchTheLatestRelease()
        {
            // The happy path, and the whole point of the change: decided on the bytes, not on the
            // recorded version. CurrentCliVersion is deliberately stale here to prove it is no longer
            // consulted for the decision.
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "the-current-cli");
            var cut = new FakeDownloader(
                Options(currentCliVersion: "v1.1000.0"),
                sha: Sha256.Checksum(installedCli));

            Assert.False(cut.IsCliDownloadNeeded(installedCli));

            // One version lookup, one checksum lookup, and no process launch: matching bytes already
            // prove the protocol, because the release URL is keyed on it.
            Assert.Equal(1, cut.ReleaseInfoFetches);
            Assert.Equal(1, cut.ShaFetches);
            Assert.Equal(0, cut.ProtocolProbes);
        }

        [Fact]
        public void IsCliDownloadNeeded_HashesTheBinaryOnce_HoweverManyTimesItIsAsked()
        {
            // Startup asks three times: package init, the language client's load, and the update. The
            // comparison reads the whole binary, so on a real ~175MB CLI three passes cost about a
            // second and a half — and far more when the path is a network share. Counting the reads
            // through an injected checksum function, because the cost is invisible from the verdict.
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "the-current-cli");
            var realSha = Sha256.Checksum(installedCli);

            var checksumReads = 0;
            var cut = new FakeDownloader(Options(), sha: realSha, computeChecksum: path =>
            {
                checksumReads++;

                return Sha256.Checksum(path);
            });

            Assert.False(cut.IsCliDownloadNeeded(installedCli));
            Assert.False(cut.IsCliDownloadNeeded(installedCli));
            Assert.False(cut.IsCliDownloadNeeded(installedCli));

            Assert.Equal(1, checksumReads);
        }

        [Fact]
        public void IsCliDownloadNeeded_HashesAgain_WhenTheBinaryOnDiskChanges()
        {
            // The memo is keyed on length and last-write-time, so replacing the binary must invalidate
            // it. Without that, an install during the same episode would be answered from a verdict
            // measured against the file it replaced.
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "a-stale-cli");

            var checksumReads = 0;
            var cut = new FakeDownloader(Options(), sha: "not-the-sha-of-either-file", computeChecksum: path =>
            {
                checksumReads++;

                return Sha256.Checksum(path);
            });

            Assert.True(cut.IsCliDownloadNeeded(installedCli));
            Assert.Equal(1, checksumReads);

            // A different length, so the key changes even if the clock resolution hides the write time.
            File.WriteAllText(installedCli, "a-replacement-cli-of-a-different-length");
            File.SetLastWriteTimeUtc(installedCli, DateTime.UtcNow.AddSeconds(5));

            Assert.True(cut.IsCliDownloadNeeded(installedCli));
            Assert.Equal(2, checksumReads);
        }

        [Fact]
        public void IsCliDownloadNeeded_ReturnsTrue_WhenTheBytesOnDiskAreNotTheLatestRelease()
        {
            // Covers the corrupt-binary case too: a truncated file has the wrong checksum, so it is
            // replaced. Under the old version-string comparison a recorded version that happened to
            // match the latest release meant a corrupt CLI was never re-downloaded.
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "a-truncated-or-stale-cli");
            var cut = new FakeDownloader(
                Options(currentCliVersion: "v1.1292.0"),
                sha: "0000000000000000000000000000000000000000000000000000000000000000");

            Assert.True(cut.IsCliDownloadNeeded(installedCli));
            Assert.Equal(0, cut.ProtocolProbes);
        }

        [Fact]
        public async Task AutoUpdateCliAsync_RaisesDownloadFinishedWithoutRunningInstallCallbacks_WhenNoDownloadIsNeededAsync()
        {
            // DownloadFinished is what moves the tool window off "Snyk Security is loading...", so the
            // nothing-to-do path must still raise it. It must NOT run the finished-callback list: those
            // callbacks mean "record what was installed", and one of them re-fetches the release name
            // over the network — which broke offline startup and wrote a version for a non-install.
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "an-up-to-date-cli");

            var finishedCallbackRan = false;

            // The checksum is what makes this the no-download path now; the recorded version is not
            // consulted. Deliberately stale to prove that.
            var cut = new FakeDownloader(
                Options(currentCliVersion: "v1.1000.0"),
                sha: Sha256.Checksum(installedCli));

            await cut.AutoUpdateCliAsync(
                this.progressWorkerMock.Object,
                installedCli,
                new List<SnykCliDownloader.CliDownloadFinishedCallback> { () => finishedCallbackRan = true });

            Assert.False(finishedCallbackRan, "install callbacks must not run when nothing was installed");
            this.progressWorkerMock.VerifySet(w => w.IsWorkFinished = true, Times.Once());

            // binaryWasDownloaded:false — subscribers that report progress must not announce a download
            // that did not happen, and no DownloadStarted preceded this.
            this.progressWorkerMock.Verify(w => w.DownloadFinished(false), Times.Once);
            this.progressWorkerMock.Verify(w => w.DownloadFinished(true), Times.Never);

            // Nothing was fetched: the binary already on disk is the one we want.
            Assert.Equal("an-up-to-date-cli", File.ReadAllText(installedCli));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsExistingCliUsable_RequiresTheProtocolProbe_NotJustPresence(bool protocolSupported)
        {
            // The fallback after a failed or cancelled download. Presence alone was the old test, which
            // is how the language server came to be restarted against a binary that could not run.
            var installedCli = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(installedCli, "an-existing-cli");
            var cut = new FakeDownloader(Options(), protocolSupported: protocolSupported);

            Assert.Equal(protocolSupported, cut.IsExistingCliUsable(installedCli));
            Assert.Equal(1, cut.ProtocolProbes);
        }

        [Fact]
        public void IsExistingCliUsable_ReturnsFalse_WithoutProbing_WhenThereIsNoCli()
        {
            var missingCli = Path.Combine(this.workDir, "not-installed.exe");
            var cut = new FakeDownloader(Options(), protocolSupported: true);

            Assert.False(cut.IsExistingCliUsable(missingCli));
            Assert.Equal(0, cut.ProtocolProbes);
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
        public void IsCliDownloadNeeded_ThenASecondConsumer_ShareOneReleaseInfoFetch()
        {
            var destination = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(destination, "an-existing-cli");
            var cut = new FakeDownloader(Options(currentCliVersion: "v1.1000.0"), sha: "a-sha-that-does-not-match");

            Assert.True(cut.IsCliDownloadNeeded(destination));
            Assert.Equal(1, cut.ReleaseInfoFetches);
            Assert.Equal(1, cut.ShaFetches);

            // The second consumer of the same information must not re-ask.
            Assert.Equal("v1.1292.0", cut.GetLatestReleaseInfoOnce().Name);
            Assert.Equal(1, cut.ReleaseInfoFetches);
        }

        [Fact]
        public async Task IsCliDownloadNeededThenDownloadAsync_ShareOneChecksumFetchAsync()
        {
            // Two round trips for a startup that installs nothing: the release version and its checksum.
            // The checksum is now consulted by the decision as well as by verification, so without the
            // memo the pair of them would ask twice — and a release landing between the two answers
            // would have them disagree.
            var destination = Path.Combine(this.workDir, "snyk-win.exe");
            File.WriteAllText(destination, "the-current-cli");
            var cut = new FakeDownloader(Options(), sha: Sha256.Checksum(destination));

            Assert.False(cut.IsCliDownloadNeeded(destination));
            Assert.Equal(1, cut.ShaFetches);

            await cut.DownloadAsync(
                this.progressWorkerMock.Object,
                destination,
                "https://downloads.snyk.io/cli/v1.1292.0/snyk-win.exe",
                new List<SnykCliDownloader.CliDownloadFinishedCallback>());

            Assert.Equal(1, cut.ReleaseInfoFetches);
            Assert.Equal(1, cut.ShaFetches);
            Assert.Equal(0, cut.ProtocolProbes);
        }
    }
}
