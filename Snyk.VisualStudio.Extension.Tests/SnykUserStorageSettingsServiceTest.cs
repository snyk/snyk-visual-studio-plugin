namespace Snyk.VisualStudio.Extension.Tests
{
    using System.IO;
    using Moq;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Snyk.VisualStudio.Extension.Shared.Settings;
    using Xunit;

    /// <summary>
    /// Test case for <see cref="SnykUserStorageSettingsService"/>.
    /// </summary>
    public class SnykUserStorageSettingsServiceTest
    {
        [Fact]
        public void SnykUserStorageSettingsService_ProjectNameNotExists_SaveAdditionalOptionsSuccessfull()
        {
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            var solutionServiceMock = new Mock<ISolutionService>();

            serviceProviderMock
                .Setup(serviceProvider => serviceProvider.SolutionService)
                .Returns(solutionServiceMock.Object);

            solutionServiceMock
                .Setup(solutionService => solutionService.GetPath())
                .Returns("C:\\Projects\\TestProj");

            string settingsFilePath = Path.GetTempFileName();

            var userStorageSettingsService = new SnykUserStorageSettingsService(settingsFilePath, serviceProviderMock.Object);

            userStorageSettingsService.SaveAdditionalOptions("--test-command");

            Assert.Equal("--test-command", userStorageSettingsService.GetAdditionalOptions());
        }
    }
}
