using System.IO;
using System.Threading.Tasks;
using Moq;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    /// <summary>
    /// Test case for <see cref="SnykOptionsManagerTest"/>.
    /// </summary>
    public class SnykOptionsManagerTest
    {
        [Fact]
        public async Task ProjectNameNotExists_SaveAdditionalOptionsSuccessfullAsync()
        {
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            var solutionServiceMock = new Mock<ISolutionService>();

            serviceProviderMock
                .Setup(serviceProvider => serviceProvider.SolutionService)
                .Returns(solutionServiceMock.Object);

            solutionServiceMock
                .Setup(solutionService => solutionService.GetSolutionFolderAsync())
                .ReturnsAsync("C:\\Projects\\TestProj");

            string settingsFilePath = Path.GetTempFileName();

            var userStorageSettingsService = new SnykOptionsManager(settingsFilePath, serviceProviderMock.Object);

            await userStorageSettingsService.SaveAdditionalOptionsAsync("--test-command");

            Assert.Equal("--test-command", await userStorageSettingsService.GetAdditionalOptionsAsync());
        }
    }
}
