using Moq;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Settings
{
    public class SnykOptionsManagerSecretsTests
    {
        private readonly Mock<ISnykServiceProvider> serviceProviderMock;
        private readonly SnykOptionsManager cut;
        private readonly string testSettingsPath;

        public SnykOptionsManagerSecretsTests()
        {
            this.testSettingsPath = System.IO.Path.GetTempFileName();
            this.serviceProviderMock = new Mock<ISnykServiceProvider>();
            var solutionServiceMock = new Mock<ISolutionService>();

            this.serviceProviderMock.Setup(x => x.SolutionService).Returns(solutionServiceMock.Object);
            solutionServiceMock.Setup(x => x.GetSolutionFolderAsync()).ReturnsAsync("/test/solution");

            var optionsMock = new Mock<ISnykOptions>();
            this.serviceProviderMock.Setup(x => x.Options).Returns(optionsMock.Object);

            this.cut = new SnykOptionsManager(this.testSettingsPath, this.serviceProviderMock.Object);
        }

        [Fact]
        public void Load_SecretsEnabled_DefaultsToFalse()
        {
            // Act
            var options = this.cut.Load();

            // Assert
            Assert.False(options.SecretsEnabled);
        }

        [Fact]
        public void Save_And_Load_SecretsEnabled_True_RoundTrips()
        {
            // Arrange
            var options = this.cut.Load();
            options.SecretsEnabled = true;

            // Act
            this.cut.Save(options, triggerSettingsChangedEvent: false);
            var reloaded = this.cut.Load();

            // Assert
            Assert.True(reloaded.SecretsEnabled);
        }

        [Fact]
        public void Save_And_Load_SecretsEnabled_False_RoundTrips()
        {
            // Arrange — first enable it, then disable it
            var options = this.cut.Load();
            options.SecretsEnabled = true;
            this.cut.Save(options, triggerSettingsChangedEvent: false);

            var options2 = this.cut.Load();
            options2.SecretsEnabled = false;

            // Act
            this.cut.Save(options2, triggerSettingsChangedEvent: false);
            var reloaded = this.cut.Load();

            // Assert
            Assert.False(reloaded.SecretsEnabled);
        }
    }
}
