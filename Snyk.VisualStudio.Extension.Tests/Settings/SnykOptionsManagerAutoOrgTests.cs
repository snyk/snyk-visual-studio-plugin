using System.Threading.Tasks;
using Moq;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Settings
{
    public class SnykOptionsManagerAutoOrgTests
    {
        private readonly Mock<ISnykServiceProvider> serviceProviderMock;
        private readonly Mock<ISolutionService> solutionServiceMock;
        private readonly SnykOptionsManager cut;
        private readonly string testSettingsPath;

        public SnykOptionsManagerAutoOrgTests()
        {
            this.testSettingsPath = System.IO.Path.GetTempFileName();
            this.serviceProviderMock = new Mock<ISnykServiceProvider>();
            this.solutionServiceMock = new Mock<ISolutionService>();
            
            this.serviceProviderMock.Setup(x => x.SolutionService).Returns(this.solutionServiceMock.Object);
            this.solutionServiceMock.Setup(x => x.GetSolutionFolderAsync()).ReturnsAsync("/test/solution");
            
            this.cut = new SnykOptionsManager(this.testSettingsPath, this.serviceProviderMock.Object);
        }

        [Fact]
        public async Task GetEffectiveOrganizationAsync_ShouldReturnAutoDeterminedOrg_WhenOrgSetByUserIsFalse()
        {
            // Arrange
            await this.cut.SaveAutoDeterminedOrgAsync("auto-org");
            await this.cut.SaveOrgSetByUserAsync(false);

            // Act
            var result = await this.cut.GetEffectiveOrganizationAsync();

            // Assert
            Assert.Equal("auto-org", result);
        }

        [Fact]
        public async Task GetEffectiveOrganizationAsync_ShouldReturnPreferredOrg_WhenOrgSetByUserIsTrue()
        {
            // Arrange
            await this.cut.SavePreferredOrgAsync("preferred-org");
            await this.cut.SaveOrgSetByUserAsync(true);

            // Act
            var result = await this.cut.GetEffectiveOrganizationAsync();

            // Assert
            Assert.Equal("preferred-org", result);
        }

        [Fact]
        public async Task GetEffectiveOrganizationAsync_ShouldReturnGlobalOrg_WhenNoSolutionSpecificSettings()
        {
            // Arrange
            var globalOptions = this.cut.Load();
            globalOptions.Organization = "global-org";
            this.cut.Save(globalOptions);

            // Act
            var result = await this.cut.GetEffectiveOrganizationAsync();

            // Assert
            Assert.Equal("global-org", result);
        }

        [Fact]
        public async Task GetEffectiveOrganizationAsync_ShouldReturnEmptyString_WhenNoSettingsExist()
        {
            // Act
            var result = await this.cut.GetEffectiveOrganizationAsync();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetAutoDeterminedOrgAsync_ShouldReturnEmptyString_WhenNoSettingsExist()
        {
            // Act
            var result = await this.cut.GetAutoDeterminedOrgAsync();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetAutoDeterminedOrgAsync_ShouldReturnSavedValue_WhenSettingsExist()
        {
            // Arrange
            await this.cut.SaveAutoDeterminedOrgAsync("test-auto-org");

            // Act
            var result = await this.cut.GetAutoDeterminedOrgAsync();

            // Assert
            Assert.Equal("test-auto-org", result);
        }

        [Fact]
        public async Task GetPreferredOrgAsync_ShouldReturnEmptyString_WhenNoSettingsExist()
        {
            // Act
            var result = await this.cut.GetPreferredOrgAsync();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetPreferredOrgAsync_ShouldReturnSavedValue_WhenSettingsExist()
        {
            // Arrange
            await this.cut.SavePreferredOrgAsync("test-preferred-org");

            // Act
            var result = await this.cut.GetPreferredOrgAsync();

            // Assert
            Assert.Equal("test-preferred-org", result);
        }

        [Fact]
        public async Task GetOrgSetByUserAsync_ShouldReturnFalse_WhenNoSettingsExist()
        {
            // Act
            var result = await this.cut.GetOrgSetByUserAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetOrgSetByUserAsync_ShouldReturnSavedValue_WhenSettingsExist()
        {
            // Arrange
            await this.cut.SaveOrgSetByUserAsync(true);

            // Act
            var result = await this.cut.GetOrgSetByUserAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task SaveAutoDeterminedOrgAsync_ShouldPersistValue()
        {
            // Act
            await this.cut.SaveAutoDeterminedOrgAsync("persisted-auto-org");

            // Assert
            var result = await this.cut.GetAutoDeterminedOrgAsync();
            Assert.Equal("persisted-auto-org", result);
        }

        [Fact]
        public async Task SavePreferredOrgAsync_ShouldPersistValue()
        {
            // Act
            await this.cut.SavePreferredOrgAsync("persisted-preferred-org");

            // Assert
            var result = await this.cut.GetPreferredOrgAsync();
            Assert.Equal("persisted-preferred-org", result);
        }

        [Fact]
        public async Task SaveOrgSetByUserAsync_ShouldPersistValue()
        {
            // Act
            await this.cut.SaveOrgSetByUserAsync(true);

            // Assert
            var result = await this.cut.GetOrgSetByUserAsync();
            Assert.True(result);
        }
    }
}
