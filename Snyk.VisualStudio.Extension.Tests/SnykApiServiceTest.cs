namespace Snyk.VisualStudio.Extension.Shared.Tests
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Moq;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Snyk.VisualStudio.Extension.Shared.Settings;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="SnykApiService"/>.
    /// </summary>
    public class SnykApiServiceTest
    {
        [Fact]
        public async Task SnykApiService_CallSastEnabled_TrueAsync()
        {
            var optionsMock = new Mock<ISnykOptions>();

            optionsMock
                .Setup(options => options.CustomEndpoint)
                .Returns("https://dev.snyk.io/api");

            optionsMock
                .Setup(options => options.ApiToken)
                .Returns(Environment.GetEnvironmentVariable("TEST_API_TOKEN"));

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
                .Returns(Environment.GetEnvironmentVariable("TEST_API_TOKEN"));
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
                .Returns("https://dev.snyk.io/api");

            optionsMock
                .Setup(options => options.ApiToken)
                .Returns("bad api token");

            var apiService = new SnykApiService(optionsMock.Object);

            var sastSettings = await apiService.GetSastSettingsAsync();

            Assert.Null(sastSettings);
        }
    }
}
