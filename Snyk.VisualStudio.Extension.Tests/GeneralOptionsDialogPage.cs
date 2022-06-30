namespace Snyk.VisualStudio.Extension.Tests
{
    using Moq;
    using Snyk.VisualStudio.Extension.Shared.CLI;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Snyk.VisualStudio.Extension.Shared.Settings;
    using Xunit;

    public class GeneralOptionsDialogPage
    {
        [Fact]
        public void ApiEndpointChanged_InvalidatesCliToken()
        {
            // Arrange
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            var optionsDialogPage = new SnykGeneralOptionsDialogPage();
            var cliMock = new Mock<ICli>();
            serviceProviderMock.Setup(provider => provider.NewCli())
                .Returns(cliMock.Object);
            optionsDialogPage.Initialize(serviceProviderMock.Object);
            cliMock.VerifyNoOtherCalls();

            // Act
            optionsDialogPage.CustomEndpoint = "https://app.some.mock.address.snyk.io/api";

            // Assert
            cliMock.Verify(mock => mock.UnsetApiToken());
        }
    }
}