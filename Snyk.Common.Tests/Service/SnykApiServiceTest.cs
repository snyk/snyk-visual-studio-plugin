namespace Snyk.VisualStudio.Extension.Shared.Tests
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Moq;
    using Snyk.Common.Authentication;
    using Snyk.Common.Service;
    using Snyk.Common.Settings;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="SnykApiService"/>.
    /// </summary>
    public class SnykApiServiceTest
    {
        private static readonly AuthenticationToken TestToken = new AuthenticationToken(AuthenticationType.Token, Environment.GetEnvironmentVariable("TEST_API_TOKEN"));

        [Fact]
        public async Task SnykApiService_GetUserAsync_ReturnsUserAsync()
        {
            var optionsMock = new Mock<ISnykOptions>();

            optionsMock
                .Setup(options => options.CustomEndpoint)
                .Returns("https://snyk.io/api");
            optionsMock
                .Setup(options => options.ApiToken)
                .Returns(TestToken);

            var apiService = new SnykApiService(optionsMock.Object);

            var snykUser = await apiService.GetUserAsync();

            Assert.NotNull(snykUser);
            Assert.NotNull(snykUser.Id);
            Assert.NotEqual("", snykUser.Id);
        }

        [Fact]
        public async Task SnykApiService_CallSastEnabled_TrueAsync()
        {
            var optionsMock = new Mock<ISnykOptions>();

            optionsMock
                .Setup(options => options.CustomEndpoint)
                .Returns("https://snyk.io/api");

            optionsMock
                .Setup(options => options.ApiToken)
                .Returns(TestToken);

            var apiService = new SnykApiService(optionsMock.Object);

            var sastSettings = await apiService.GetSastSettingsAsync();

            Assert.NotNull(sastSettings);
            Assert.True(sastSettings.SastEnabled);

            Assert.NotNull(sastSettings.LocalCodeEngine);
        }

        [Fact]
        public async Task SnykApiService_SendSastSettingsRequestAsync_OrgIsInQueryParamAsync()
        {
            var optionsMock = new Mock<ISnykOptions>();

            optionsMock
                .Setup(options => options.CustomEndpoint)
                .Returns("https://dev.snyk.io/api");
            optionsMock
                .Setup(options => options.ApiToken)
                .Returns(TestToken);
            optionsMock
                .Setup(options => options.Organization)
                .Returns("my-super-org");

            var apiService = new SnykApiService(optionsMock.Object);

            HttpResponseMessage response = await apiService.SendSastSettingsRequestAsync();

            Assert.Contains("?org=my-super-org", response.RequestMessage.RequestUri.Query);
        }

        [Fact]
        public async Task SnykApiService_InvalidAuthTokenProvided_ReturnNullAsync()
        {
            var optionsMock = new Mock<ISnykOptions>();

            optionsMock
                .Setup(options => options.CustomEndpoint)
                .Returns("https://snyk.io/api");

            optionsMock
                .Setup(options => options.ApiToken)
                .Returns(AuthenticationToken.EmptyToken);

            var apiService = new SnykApiService(optionsMock.Object);

            var sastSettings = await apiService.GetSastSettingsAsync();

            Assert.Null(sastSettings);
        }

        [Fact]
        public async Task SnykApiService_InvalidUrlProvided_ReturnNullAsync()
        {
            var optionsMock = new Mock<ISnykOptions>();

            optionsMock
                .Setup(options => options.CustomEndpoint)
                .Returns("https://snyk.io/");

            optionsMock
                .Setup(options => options.ApiToken)
                .Returns(TestToken);

            var apiService = new SnykApiService(optionsMock.Object);

            var sastSettings = await apiService.GetSastSettingsAsync();

            Assert.Null(sastSettings);
        }
    }
}
