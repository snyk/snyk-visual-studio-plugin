namespace Snyk.VisualStudio.Extension.Tests
{
    using System.Collections.Generic;
    using System.Net;
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

        [Theory]
        [InlineData("https://snyk.io/api", true)]
        [InlineData("https://app.snyk.io/api", true)]
        [InlineData("https://app.us.snyk.io/api", true)]
        [InlineData("https://app.eu.snyk.io/api", false)]
        [InlineData("https://app.au.snyk.io/api", false)]
        [InlineData("https://app.snykgov.io/api", false)]

        [InlineData("https://api.snyk.io", true)]
        [InlineData("https://api.us.snyk.io", true)]
        [InlineData("https://api.eu.snyk.io", false)]
        [InlineData("https://api.au.snyk.io", false)]
        [InlineData("https://api.snykgov.io", false)]
        public void IsAnalyticsPermitted(string endpoint, bool expected)
        {
            var optionsDialogPage = new SnykGeneralOptionsDialogPage();
            optionsDialogPage.CustomEndpoint = endpoint;
            Assert.Equal(expected, optionsDialogPage.IsAnalyticsPermitted());
        }

        [Theory]
        [InlineData(null, "https://app.snyk.io/manage/snyk-code")]
        [InlineData("", "https://app.snyk.io/manage/snyk-code")]
        [InlineData("https://snyk.io", "https://app.snyk.io/manage/snyk-code")]
        [InlineData("https://snyk.io/api", "https://app.snyk.io/manage/snyk-code")]
        [InlineData("https://snyk.io/api/", "https://app.snyk.io/manage/snyk-code")]
        [InlineData("https://snykgov.io/api", "https://app.snykgov.io/manage/snyk-code")]
        [InlineData("https://app.snyk.io/api", "https://app.snyk.io/manage/snyk-code")]
        [InlineData("https://app.eu.snyk.io/api", "https://app.eu.snyk.io/manage/snyk-code")]
        [InlineData("https://app.snykgov.io/api", "https://app.snykgov.io/manage/snyk-code")]
        [InlineData("https://api.snyk.io", "https://app.snyk.io/manage/snyk-code")]
        [InlineData("https://api.snyk.io/", "https://app.snyk.io/manage/snyk-code")]
        [InlineData("https://api.snykgov.io", "https://app.snykgov.io/manage/snyk-code")]
        [InlineData("https://api.eu.snyk.io", "https://app.eu.snyk.io/manage/snyk-code")]
        public void SnykCodeSettingsUrl(string endpoint, string expected)
        {
            var optionsDialogPage = new SnykGeneralOptionsDialogPage();
            optionsDialogPage.CustomEndpoint = endpoint;
            Assert.Equal(expected, optionsDialogPage.SnykCodeSettingsUrl);
        }
    }
}