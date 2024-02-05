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

        [Fact]
        public void IsAnalyticsPermitted_True()
        {
            var optionsDialogPage = new SnykGeneralOptionsDialogPage();

            string[] endpoints = {
                "https://app.snyk.io/api",
                "https://snyk.io/api",
                "https://app.us.snyk.io/api",
            };

            foreach (var endpoint in endpoints)
            {
                optionsDialogPage.CustomEndpoint = endpoint;
                Assert.True(optionsDialogPage.IsAnalyticsPermitted());
            }
        }

        [Fact]
        public void IsAnalyticsPermitted_False()
        {
            var optionsDialogPage = new SnykGeneralOptionsDialogPage();

            string[] endpoints = {
                "https://app.eu.snyk.io/api",
                "https://app.au.snyk.io/api",
                "https://app.snykgov.io/api",
            };

            foreach (var endpoint in endpoints)
            {
                optionsDialogPage.CustomEndpoint = endpoint;
                Assert.False(optionsDialogPage.IsAnalyticsPermitted());
            }
        }
    }
}