using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    [Collection(MockedVS.Collection)]
    public class SnykCliDownloaderTest
    {
        private Mock<ISnykProgressWorker> progressWorkerMock;
        private Mock<ISnykOptions> optionsMock;
        private readonly Mock<ISnykOptions> mockOptions;
        private readonly SnykCliDownloader downloader;

        public SnykCliDownloaderTest(GlobalServiceProvider sp)
        {
            sp.Reset();
            this.progressWorkerMock = new Mock<ISnykProgressWorker>();
            this.optionsMock = new Mock<ISnykOptions>();
            this.optionsMock.Setup(x => x.CliDownloadUrl).Returns(SnykCliDownloader.DefaultBaseDownloadUrl);
            this.optionsMock.Setup(x => x.CliReleaseChannel).Returns("preview");
            mockOptions = new Mock<ISnykOptions>();
            downloader = new SnykCliDownloader(mockOptions.Object);
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

        [Fact]
        public void SnykWebClient_Should_Respect_IgnoreUnknownCA_Setting()
        {
            // Arrange
            mockOptions.Setup(o => o.IgnoreUnknownCA).Returns(true);

            // Act
            using (var webClient = new SnykWebClient(mockOptions.Object))
            {
                // Assert
                Assert.NotNull(webClient);
                // The ServerCertificateValidationCallback should be set when IgnoreUnknownCA is true
                Assert.NotNull(ServicePointManager.ServerCertificateValidationCallback);
            }
        }

        [Fact]
        public void SnykWebClient_Should_Handle_Null_Options()
        {
            // Act & Assert - should not throw
            using (var webClient = new SnykWebClient(null))
            {
                Assert.NotNull(webClient);
            }
        }

        [Fact]
        public void CreateHttpClientHandler_Should_Respect_IgnoreUnknownCA_Setting()
        {
            // Arrange
            mockOptions.Setup(o => o.IgnoreUnknownCA).Returns(true);
            var downloader = new SnykCliDownloader(mockOptions.Object);

            // Act
            var handler = GetHttpClientHandler(downloader);

            // Assert
            Assert.NotNull(handler);
            Assert.NotNull(handler.ServerCertificateCustomValidationCallback);
            
            // Test that the callback returns true (accepts all certificates)
            var result = handler.ServerCertificateCustomValidationCallback(
                null, null, null, System.Net.Security.SslPolicyErrors.None);
            Assert.True(result);
        }

        [Fact]
        public void CreateHttpClientHandler_Should_Handle_Secure_Connection()
        {
            // Arrange
            mockOptions.Setup(o => o.IgnoreUnknownCA).Returns(false);
            var downloader = new SnykCliDownloader(mockOptions.Object);

            // Act
            var handler = GetHttpClientHandler(downloader);

            // Assert
            Assert.NotNull(handler);
            Assert.Null(handler.ServerCertificateCustomValidationCallback);
            // UseProxy should be true by default (system proxy)
            Assert.True(handler.UseProxy);
        }

        [Fact]
        public void HttpClient_Should_Use_System_Proxy_By_Default()
        {
            // Test to verify HttpClient uses system proxy by default
            using (var defaultHandler = new HttpClientHandler())
            {
                // Assert default behavior - HttpClient should use system proxy by default
                Assert.True(defaultHandler.UseProxy);
            }
        }

        // Helper method to access the private CreateHttpClientHandler method
        private HttpClientHandler GetHttpClientHandler(SnykCliDownloader downloader)
        {
            var method = typeof(SnykCliDownloader).GetMethod("CreateHttpClientHandler", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (HttpClientHandler)method.Invoke(downloader, null);
        }
    }
}
