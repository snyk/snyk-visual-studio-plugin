namespace Snyk.VisualStudio.Extension.Shared.Tests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Moq;
    using Snyk.VisualStudio.Extension.Shared.CLI;
    using Snyk.VisualStudio.Extension.Shared.CLI.Download;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Xunit;

    public class SnykCliDownloaderTest
    {
        private Mock<ISnykProgressWorker> progressWorkerMock;

        public SnykCliDownloaderTest()
        {
            this.progressWorkerMock = new Mock<ISnykProgressWorker>();
        }

        [Fact]
        public async Task SnykCliDownloader_ExistingCliFilesNotCorruptedWhenDownloadFails_SuccessAsync()
        {
            var cliDownloader = new SnykCliDownloader(null);

            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            var progressWorkerWithErrorMock = new Mock<ISnykProgressWorker>();

            await cliDownloader.DownloadAsync(this.progressWorkerMock.Object, tempCliPath);

            progressWorkerWithErrorMock
                .Setup(progressWorker => progressWorker.CancelIfCancellationRequested())
                .Throws(new Exception("Some error for test previous cli not deleted"));

            await Assert.ThrowsAsync<Exception>(async ()
                => await cliDownloader.DownloadAsync(progressWorkerWithErrorMock.Object, tempCliPath));

            Assert.True(File.Exists(tempCliPath));

            cliDownloader.VerifyCliFile(tempCliPath);

            File.Delete(tempCliPath);
        }

        [Fact]
        public void SnykCliDownloader_VerifyCliFile_Failed()
        {
            var cliDownloader = new SnykCliDownloader(null);

            cliDownloader.SaveLatestCliSha();

            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            File.WriteAllText(tempCliPath, "Testing verify chechsum");

            Assert.True(File.Exists(tempCliPath));

            Assert.Throws<ChecksumVerificationException>(() => cliDownloader.VerifyCliFile(tempCliPath));

            File.Delete(tempCliPath);
        }

        [Fact]
        public async Task SnykCliDownloader_VerifyCliFile_SuccessfulAsync()
        {
            var cliDownloader = new SnykCliDownloader(null);

            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            await cliDownloader.DownloadAsync(this.progressWorkerMock.Object, tempCliPath);

            Assert.True(File.Exists(tempCliPath));

            cliDownloader.VerifyCliFile(tempCliPath);

            File.Delete(tempCliPath);
        }

        [Fact]
        public void SnykCliDownloader_GetLatestSha_SuccessfulRequest()
        {
            var cliDownloader = new SnykCliDownloader(null);

            Assert.False(string.IsNullOrWhiteSpace(cliDownloader.GetLatestCliSha()));
        }

        [Fact]
        public void SnykCliDownloader_CorrectInformationProvided_LatestReleaseInfoCorrect()
        {
            var cliDownloader = new SnykCliDownloader(null);

            LatestReleaseInfo latestReleaseInfo = cliDownloader.GetLatestReleaseInfo();

            Assert.False(string.IsNullOrWhiteSpace(latestReleaseInfo.Version));
        }

        [Fact]
        public async Task SnykCliDownloader_CorrectInformationProvided_DownloadSuccessfulAsync()
        {
            var cliDownloader = new SnykCliDownloader(null);

            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            await cliDownloader.DownloadAsync(this.progressWorkerMock.Object, tempCliPath);

            Assert.True(File.Exists(tempCliPath));

            File.Delete(tempCliPath);
        }

        [Fact]
        public void SnykCliDownloader_WrongNewVersionProvided_ChecksFail()
        {
            var cliDownloader = new SnykCliDownloader(null);

            Assert.False(cliDownloader.IsNewVersionAvailable("1.342.1", string.Empty));
            Assert.False(cliDownloader.IsNewVersionAvailable(string.Empty, "test"));
            Assert.False(cliDownloader.IsNewVersionAvailable("2.342.2", "1.342.1"));
            Assert.False(cliDownloader.IsNewVersionAvailable("1.345.2", "1.342.9"));
            Assert.False(cliDownloader.IsNewVersionAvailable("1.345.2", "1.345.1"));

            Assert.True(cliDownloader.IsNewVersionAvailable(string.Empty, "1.342.1"));
            Assert.True(cliDownloader.IsNewVersionAvailable("1.342.2", "1.345.1"));
            Assert.True(cliDownloader.IsNewVersionAvailable("1.342.2", "2.345.1"));
            Assert.True(cliDownloader.IsNewVersionAvailable("1.345.2", "1.345.9"));
        }

        [Fact]
        public void SnykCliDownloader_CorrectNewVersionProvided_ChecksPass()
        {
            var cliDownloader = new SnykCliDownloader(null);

            Assert.True(cliDownloader.IsNewVersionAvailable(string.Empty, "1.342.1"));
            Assert.True(cliDownloader.IsNewVersionAvailable("1.342.2", "1.345.1"));
            Assert.True(cliDownloader.IsNewVersionAvailable("1.342.2", "2.345.1"));
            Assert.True(cliDownloader.IsNewVersionAvailable("1.345.2", "1.345.9"));
        }

        [Fact]
        public void SnykCliDownloader_CorrectLastCheckDatePassed_CheckPass()
        {
            var cliDownloader = new SnykCliDownloader(null);

            Assert.True(cliDownloader.IsFourDaysPassedAfterLastCheck(DateTime.Now.AddDays(-5)));
        }

        [Fact]
        public void SnykCliDownloader_WrongLastCheckDatePassed_CheckFail()
        {
            var cliDownloader = new SnykCliDownloader(null);

            Assert.False(cliDownloader.IsFourDaysPassedAfterLastCheck(DateTime.Now.AddDays(-3)));
        }

        [Fact]
        public async Task SnykCliDownloader_CliFileNotExists_CliDownloadSuccessfulAsync()
        {
            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            var cliDownloader = new SnykCliDownloader(null);

            await cliDownloader.AutoUpdateCliAsync(this.progressWorkerMock.Object, DateTime.Now.AddDays(-5), tempCliPath);

            string newCliVersion = cliDownloader.GetLatestReleaseInfo().Name;

            Assert.True(File.Exists(tempCliPath));

            File.Delete(tempCliPath);
        }

        [Fact]
        public async Task SnykCliDownloader_WrongPreviousVersionProvided_CliDownloadSuccessfulAsync()
        {
            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            var cliDownloader = new SnykCliDownloader(null);

            await cliDownloader.AutoUpdateCliAsync(this.progressWorkerMock.Object, DateTime.Now.AddDays(-5), tempCliPath);

            string newCliVersion = cliDownloader.GetLatestReleaseInfo().Version;

            Assert.True(File.Exists(tempCliPath));

            File.Delete(tempCliPath);

            Assert.False(string.IsNullOrEmpty(newCliVersion));
            Assert.True(cliDownloader.IsNewVersionAvailable(string.Empty, newCliVersion));
        }

        [Fact]
        public async Task SnykCliDownloader_PreviousVersionOlderProvided_CliDownloadSuccessfulAsync()
        {
            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            string currentCliVersion = "1.234.2";

            var cliDownloader = new SnykCliDownloader(currentCliVersion);

            await cliDownloader.AutoUpdateCliAsync(this.progressWorkerMock.Object, DateTime.Now.AddDays(-5), tempCliPath);

            string newCliVersion = cliDownloader.GetLatestReleaseInfo().Version;

            Assert.True(File.Exists(tempCliPath));

            File.Delete(tempCliPath);

            Assert.True(cliDownloader.IsNewVersionAvailable(currentCliVersion, newCliVersion));
        }

        [Fact]
        public async Task SnykCliDownloader_PreviousVersionOlderAndFourDaysPassedProvided_CliDownloadSuccessfulAsync()
        {
            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            string currentCliVersion = "1.234.2";

            var cliDownloader = new SnykCliDownloader(currentCliVersion);

            await cliDownloader.AutoUpdateCliAsync(this.progressWorkerMock.Object, DateTime.Now.AddDays(-5), tempCliPath);

            string newCliVersion = cliDownloader.GetLatestReleaseInfo().Version;

            Assert.True(File.Exists(tempCliPath));

            File.Delete(tempCliPath);

            Assert.True(cliDownloader.IsNewVersionAvailable(currentCliVersion, newCliVersion));
        }
    }
}
