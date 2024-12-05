using System.Threading;
using System.Threading.Tasks;
using Moq;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Service
{
    public class SnykFeatureFlagServiceTests
    {
        private readonly Mock<ILanguageClientManager> languageClientManagerMock;
        private readonly Mock<ISnykOptions> settingsMock;
        private readonly SnykFeatureFlagService cut;

        public SnykFeatureFlagServiceTests()
        {
            languageClientManagerMock = new Mock<ILanguageClientManager>();
            settingsMock = new Mock<ISnykOptions>();
            cut = new SnykFeatureFlagService(languageClientManagerMock.Object, settingsMock.Object);
        }

        [Fact]
        public async Task RefreshAsync_ShouldSetConsistentIgnoresEnabledToFalse_WhenResultIsNull()
        {
            // Arrange
            languageClientManagerMock
                .Setup(lc => lc.InvokeGetFeatureFlagStatusAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((FeatureFlagResponse)null);

            // Act
            await cut.RefreshAsync(new CancellationToken());

            // Assert
            settingsMock.VerifySet(s => s.ConsistentIgnoresEnabled = false, Times.Once);
        }

        [Fact]
        public async Task RefreshAsync_ShouldSetConsistentIgnoresEnabledToTrue_WhenResultOkIsTrue()
        {
            // Arrange
            var response = new FeatureFlagResponse { Ok = true };
            languageClientManagerMock
                .Setup(lc => lc.InvokeGetFeatureFlagStatusAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            await cut.RefreshAsync(new CancellationToken());

            // Assert
            settingsMock.VerifySet(s => s.ConsistentIgnoresEnabled = true, Times.Once);
        }

        [Fact]
        public async Task RefreshAsync_ShouldSetConsistentIgnoresEnabledToFalse_WhenResultOkIsFalse()
        {
            // Arrange
            var response = new FeatureFlagResponse { Ok = false };
            languageClientManagerMock
                .Setup(lc => lc.InvokeGetFeatureFlagStatusAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            await cut.RefreshAsync(new CancellationToken());

            // Assert
            settingsMock.VerifySet(s => s.ConsistentIgnoresEnabled = false, Times.Once);
        }
    }
}
