using System.Threading;
using System.Threading.Tasks;
using Moq;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Service;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    /// <summary>
    /// Unit tests for <see cref="OssService"/>.
    /// </summary>
    public class OssServiceTest
    {
        [Fact]
        public void OssServiceTest_NoCachedValuesExists_ReturnNewScanResult()
        {
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            var cliMock = new Mock<ICli>();
            var optionsMock = new Mock<ISnykOptions>();

            serviceProviderMock
                .Setup(serviceProvider => serviceProvider.Options)
                .Returns(optionsMock.Object);

            serviceProviderMock
                .Setup(serviceProvider => serviceProvider.NewCli())
                .Returns(cliMock.Object);

            var fakeCliResult = new CliResult();

            cliMock
                .Setup(cli => cli.ScanAsync(It.IsAny<string>()))
                .ReturnsAsync(fakeCliResult);

            var ossService = new OssService(serviceProviderMock.Object);

            var tokenSource = new CancellationTokenSource();

            var cliResult = ossService.ScanAsync(string.Empty, tokenSource.Token);

            Assert.NotNull(cliResult);
        }

        [Fact]
        public async System.Threading.Tasks.Task OssServiceTest_CachedValuesExists_ReturnCachedResultAsync()
        {
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            var cliMock = new Mock<ICli>();
            var optionsMock = new Mock<ISnykOptions>();

            serviceProviderMock
                .Setup(serviceProvider => serviceProvider.Options)
                .Returns(optionsMock.Object);

            serviceProviderMock
                .Setup(serviceProvider => serviceProvider.NewCli())
                .Returns(cliMock.Object);

            var fakeCliResult = new CliResult();

            cliMock
                .Setup(cli => cli.ScanAsync(It.IsAny<string>()))
                .ReturnsAsync(fakeCliResult);

            var ossService = new OssService(serviceProviderMock.Object);

            var tokenSource = new CancellationTokenSource();

            // Run scan first time will setup cache value.
            await ossService.ScanAsync(string.Empty, tokenSource.Token);

            // Get cached value.
            var cliResult = ossService.ScanAsync(string.Empty, tokenSource.Token);

            Assert.NotNull(cliResult);

            cliMock
                .Verify(cli => cli.ScanAsync(It.IsAny<string>()), Times.Exactly(1));
        }

        [Fact]
        public async Task OssServiceTest_ClearCache_ReturnNewValueAsync()
        {
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            var cliMock = new Mock<ICli>();
            var optionsMock = new Mock<ISnykOptions>();

            serviceProviderMock
                .Setup(serviceProvider => serviceProvider.Options)
                .Returns(optionsMock.Object);

            serviceProviderMock
                .Setup(serviceProvider => serviceProvider.NewCli())
                .Returns(cliMock.Object);

            var fakeCliResult = new CliResult();

            cliMock
                .Setup(cli => cli.ScanAsync(It.IsAny<string>()))
                .ReturnsAsync(fakeCliResult);

            var ossService = new OssService(serviceProviderMock.Object);

            var tokenSource = new CancellationTokenSource();

            await ossService.ScanAsync(string.Empty, tokenSource.Token);

            ossService.ClearCache();

            var cliResult = ossService.ScanAsync(string.Empty, tokenSource.Token);

            Assert.NotNull(cliResult);

            cliMock
                .Verify(cli => cli.ScanAsync(It.IsAny<string>()), Times.Exactly(2));
        }
    }
}
