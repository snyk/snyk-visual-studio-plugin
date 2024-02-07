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
        [InlineData("https://example.org", "https://app.snyk.io/manage/snyk-code")]
        public void SnykCodeSettingsUrl(string endpoint, string expected)
        {
            var optionsDialogPage = new SnykGeneralOptionsDialogPage();
            optionsDialogPage.CustomEndpoint = endpoint;
            Assert.Equal(expected, optionsDialogPage.SnykCodeSettingsUrl);
        }
    }
}