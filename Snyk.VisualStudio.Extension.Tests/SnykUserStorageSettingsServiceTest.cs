using System.IO;
using System.Threading.Tasks;
using Moq;
using Snyk.VisualStudio.Extension.Authentication;
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
        private SnykOptionsManager cut;
        private readonly string settingsFilePath;
        private readonly Mock<ISnykServiceProvider> serviceProviderMock;
        private readonly Mock<ISolutionService> solutionServiceMock;

        public SnykOptionsManagerTest()
        {
            this.settingsFilePath = Path.GetTempFileName();
            this.serviceProviderMock = new Mock<ISnykServiceProvider>();
            this.solutionServiceMock = new Mock<ISolutionService>();
            cut = new SnykOptionsManager(settingsFilePath, serviceProviderMock.Object);
            serviceProviderMock
                .Setup(serviceProvider => serviceProvider.SolutionService)
                .Returns(solutionServiceMock.Object);
        }

        [Fact]
        public async Task ProjectNameNotExists_SaveAdditionalOptionsSuccessfullAsync()
        {
            solutionServiceMock
                .Setup(solutionService => solutionService.GetSolutionFolderAsync())
                .ReturnsAsync("C:\\Projects\\TestProj");


            await cut.SaveAdditionalOptionsAsync("--test-command");

            Assert.Equal("--test-command", await cut.GetAdditionalOptionsAsync());
        }

        [Fact]
        public void LoadAndSaveGeneralOptions_PersistsChanges()
        {
            // Load default
            var options = cut.Load() as SnykOptions;
            Assert.NotNull(options);
            // Set some values
            options.AutoScan = true;
            options.IgnoreUnknownCA = true;
            options.Organization = "my-org";
            options.CustomEndpoint = "https://custom.endpoint";
            options.AuthenticationMethod = AuthenticationType.OAuth;
            options.ApiToken = new AuthenticationToken(AuthenticationType.OAuth, "dummy-token");
            options.BinariesAutoUpdate = true;
            options.CliCustomPath = "C:\\cli\\snyk.exe";
            options.CliDownloadUrl = "https://cli.download.url";
            options.CliReleaseChannel = "stable";
            options.CurrentCliVersion = "1.2.3";
            options.IacEnabled = true;
            options.SnykCodeSecurityEnabled = true;
            options.SnykCodeQualityEnabled = true;
            options.OssEnabled = true;

            cut.Save(options);

            // Reload to confirm persistence
            var reloadedOptions = cut.Load() as SnykOptions;
            Assert.NotNull(reloadedOptions);

            Assert.True(reloadedOptions.AutoScan);
            Assert.True(reloadedOptions.IgnoreUnknownCA);
            Assert.Equal("my-org", reloadedOptions.Organization);
            Assert.Equal("https://custom.endpoint", reloadedOptions.CustomEndpoint);
            Assert.Equal(AuthenticationType.OAuth, reloadedOptions.AuthenticationMethod);
            Assert.Equal("dummy-token", reloadedOptions.ApiToken.ToString());
            Assert.True(reloadedOptions.BinariesAutoUpdate);
            Assert.Equal("C:\\cli\\snyk.exe", reloadedOptions.CliCustomPath);
            Assert.Equal("https://cli.download.url", reloadedOptions.CliDownloadUrl);
            Assert.Equal("stable", reloadedOptions.CliReleaseChannel);
            Assert.Equal("1.2.3", reloadedOptions.CurrentCliVersion);
            Assert.True(reloadedOptions.IacEnabled);
            Assert.True(reloadedOptions.SnykCodeSecurityEnabled);
            Assert.True(reloadedOptions.SnykCodeQualityEnabled);
            Assert.True(reloadedOptions.OssEnabled);
        }

        [Fact]
        public async Task GetAdditionalOptions_EmptyIfNoExistingSettings()
        {
            solutionServiceMock
                .Setup(solutionService => solutionService.GetSolutionFolderAsync())
                .ReturnsAsync("C:\\Projects\\NonExistentProj");

            Assert.Equal(string.Empty, await cut.GetAdditionalOptionsAsync());
        }

        [Fact]
        public async Task OverwriteAdditionalOptionsSuccessfully()
        {
            solutionServiceMock
                .Setup(solutionService => solutionService.GetSolutionFolderAsync())
                .ReturnsAsync("C:\\Projects\\NonExistentProj");

            // Save initial options
            await cut.SaveAdditionalOptionsAsync("--first-command");
            Assert.Equal("--first-command", await cut.GetAdditionalOptionsAsync());

            // Overwrite with new options
            await cut.SaveAdditionalOptionsAsync("--second-command");
            Assert.Equal("--second-command", await cut.GetAdditionalOptionsAsync());
        }

        [Fact]
        public async Task GetIsAllProjectsEnabledAsync_DefaultTrueIfNotSet()
        {
            solutionServiceMock
                .Setup(solutionService => solutionService.GetSolutionFolderAsync())
                .ReturnsAsync("C:\\Projects\\NonExistentProj");
            Assert.True(await cut.GetIsAllProjectsEnabledAsync());
        }

        [Fact]
        public async Task SaveIsAllProjectsScanEnabledAsync_ChangesValue()
        {
            solutionServiceMock
                .Setup(solutionService => solutionService.GetSolutionFolderAsync())
                .ReturnsAsync("C:\\Projects\\NonExistentProj");
            // Default is true
            Assert.True(await cut.GetIsAllProjectsEnabledAsync());

            await cut.SaveIsAllProjectsScanEnabledAsync(false);
            Assert.False(await cut.GetIsAllProjectsEnabledAsync());

            // Change back to true
            await cut.SaveIsAllProjectsScanEnabledAsync(true);
            Assert.True(await cut.GetIsAllProjectsEnabledAsync());
        }


    }
}
