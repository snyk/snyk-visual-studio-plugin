namespace Snyk.VisualStudio.Extension.Tests
{
    using System;
    using System.Threading.Tasks;
    using Moq;
    using Snyk.VisualStudio.Extension.Service;
    using Snyk.VisualStudio.Extension.Settings;
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
        }
    }
}
