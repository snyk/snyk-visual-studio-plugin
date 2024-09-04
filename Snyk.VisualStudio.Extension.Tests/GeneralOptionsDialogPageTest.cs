using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    [Collection(MockedVS.Collection)]
    public class GeneralOptionsDialogPageTest
    {
        public GeneralOptionsDialogPageTest(GlobalServiceProvider sp)
        {
            sp.Reset();
        }

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

        [Theory]
        [InlineData(null, "https://api.snyk.io")]
        [InlineData("", "https://api.snyk.io")]
        [InlineData("https://snyk.io/api", "https://api.snyk.io")]
        [InlineData("https://app.snyk.io/api", "https://api.snyk.io")]
        [InlineData("https://app.snykgov.io/api", "https://api.snykgov.io")]
        [InlineData("https://app.eu.snyk.io/api", "https://api.eu.snyk.io")]
        [InlineData("https://api.snyk.io", "https://api.snyk.io")]
        [InlineData("https://api.snykgov.io", "https://api.snykgov.io")]
        [InlineData("https://api.eu.snyk.io", "https://api.eu.snyk.io")]
        public void TransformApiToNewSchema(string endpoint, string expected)
        {
            var optionsDialogPage = new SnykGeneralOptionsDialogPage();
            optionsDialogPage.CustomEndpoint = endpoint;
            Assert.Equal(expected, optionsDialogPage.GetCustomApiEndpoint());
        }

        [Theory]
        [InlineData(null, "https://app.snyk.io")]
        [InlineData("", "https://app.snyk.io")]
        [InlineData("https://app.snyk.io/api", "https://app.snyk.io")]
        [InlineData("https://app.snykgov.io/api", "https://app.snykgov.io")]
        [InlineData("https://app.eu.snyk.io/api", "https://app.eu.snyk.io")]
        [InlineData("https://api.snyk.io", "https://app.snyk.io")]
        [InlineData("https://api.snykgov.io", "https://app.snykgov.io")]
        [InlineData("https://api.eu.snyk.io", "https://app.eu.snyk.io")]
        public void GetBaseAppUrl(string endpoint, string expected)
        {
            var optionsDialogPage = new SnykGeneralOptionsDialogPage();
            optionsDialogPage.CustomEndpoint = endpoint;
            Assert.Equal(expected, optionsDialogPage.GetBaseAppUrl());
        }
    }
}