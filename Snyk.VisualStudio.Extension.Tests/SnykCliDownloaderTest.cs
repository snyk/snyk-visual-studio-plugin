using System;
using System.IO;
using System.Threading.Tasks;
using Moq;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Service;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class SnykCliDownloaderTest
    {
        private Mock<ISnykProgressWorker> progressWorkerMock;
        private Mock<ISnykOptions> optionsMock;

        public SnykCliDownloaderTest()
        {
            this.progressWorkerMock = new Mock<ISnykProgressWorker>();
            this.optionsMock = new Mock<ISnykOptions>();
            this.optionsMock.Setup(x => x.CliDownloadUrl).Returns(SnykCliDownloader.DefaultBaseDownloadUrl);
            this.optionsMock.Setup(x => x.CliReleaseChannel).Returns(SnykCliDownloader.DefaultReleaseChannel);
        }

        [Fact]
        public async Task SnykCliDownloader_ExistingCliFilesNotCorruptedWhenDownloadFails_SuccessAsync()
        {
            
            var cliDownloader = new SnykCliDownloader(optionsMock.Object);

            var tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

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
            var cliDownloader = new SnykCliDownloader(optionsMock.Object);
            const string url = "https://static.snyk.io/cli/latest/snyk-win.exe";
            cliDownloader.SaveLatestCliSha(url);

            var tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

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
            var cliDownloader = new SnykCliDownloader(optionsMock.Object);

            var tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

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
            var cliDownloader = new SnykCliDownloader(optionsMock.Object);
            const string url = "https://static.snyk.io/cli/latest/snyk-win.exe";
            Assert.False(string.IsNullOrWhiteSpace(cliDownloader.GetLatestCliSha(url)));
        }

        [Fact]
        public void SnykCliDownloader_CorrectInformationProvided_LatestReleaseInfoCorrect()
        {
            var cliDownloader = new SnykCliDownloader(optionsMock.Object);

            LatestReleaseInfo latestReleaseInfo = cliDownloader.GetLatestReleaseInfo();

            Assert.False(string.IsNullOrWhiteSpace(latestReleaseInfo.Version));
        }

        [Fact]
        public async Task SnykCliDownloader_CorrectInformationProvided_DownloadSuccessfulAsync()
        {
            var cliDownloader = new SnykCliDownloader(optionsMock.Object);

            var tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            await cliDownloader.DownloadAsync(this.progressWorkerMock.Object, tempCliPath);

            Assert.True(File.Exists(tempCliPath));

            File.Delete(tempCliPath);
        }

        [Fact]
        public async Task SnykCliDownloader_CliFileNotExists_CliDownloadSuccessfulAsync()
        {
            var tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            var cliDownloader = new SnykCliDownloader(optionsMock.Object);

            await cliDownloader.AutoUpdateCliAsync(this.progressWorkerMock.Object, tempCliPath);

            Assert.True(File.Exists(tempCliPath));

            File.Delete(tempCliPath);
        }

        [Fact]
        public async Task SnykCliDownloader_WrongPreviousVersionProvided_CliDownloadSuccessfulAsync()
        {
            var tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            var cliDownloader = new SnykCliDownloader(optionsMock.Object);

            await cliDownloader.AutoUpdateCliAsync(this.progressWorkerMock.Object, tempCliPath);

            var newCliVersion = cliDownloader.GetLatestReleaseInfo().Version;

            Assert.True(File.Exists(tempCliPath));

            File.Delete(tempCliPath);

            Assert.False(string.IsNullOrEmpty(newCliVersion));
            Assert.True(cliDownloader.IsCliDownloadNeeded(tempCliPath));
        }

        [Fact]
        public async Task SnykCliDownloader_PreviousVersionOlderProvided_CliDownloadSuccessfulAsync()
        {
            var tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            optionsMock.Object.CurrentCliVersion = "v1.234.2";
            var cliDownloader = new SnykCliDownloader(optionsMock.Object);

            await cliDownloader.AutoUpdateCliAsync(this.progressWorkerMock.Object, tempCliPath);

            Assert.True(File.Exists(tempCliPath));

            File.Delete(tempCliPath);
            Assert.True(cliDownloader.IsCliDownloadNeeded(tempCliPath));
        }
    }
}
