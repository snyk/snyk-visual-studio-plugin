namespace Snyk.Common.Tests.Service
{
    using Moq;
    using Xunit;
    using Snyk.Common.Authentication;
    using Snyk.Common.Settings;
    using Snyk.Common.Service;

    /// <summary>
    /// Tests for <see cref="ApiEndpointResolver"/>.
    /// </summary>
    public class ApiEndpointResolverTest
    {
        [Fact]
        public void ApiEndpointResolver_GetSnykCodeApiUrl_SaaSProduction()
        {
            var optionsMock = new Mock<ISnykOptions>();
            var apiEndpointResolver = new ApiEndpointResolver(optionsMock.Object);

            var snykCodeApiUrl = apiEndpointResolver.GetSnykCodeApiUrl();

            Assert.Equal("https://deeproxy.snyk.io/", snykCodeApiUrl);
        }

        [Fact]
        public void ApiEndpointResolver_GetSnykCodeApiUrl_SaaSDevelopment()
        {
            var optionsMock = new Mock<ISnykOptions>();
            optionsMock
                .Setup(options => options.CustomEndpoint)
                .Returns("https://dev.snyk.io/api");
            var apiEndpointResolver = new ApiEndpointResolver(optionsMock.Object);

            var snykCodeApiUrl = apiEndpointResolver.GetSnykCodeApiUrl();

            Assert.Equal("https://deeproxy.dev.snyk.io/", snykCodeApiUrl);
        }

        [Fact]
        public void ApiEndpointResolver_GetSnykCodeApiUrl_SingleTenant()
        {
            var optionsMock = new Mock<ISnykOptions>();
            optionsMock
                 .Setup(options => options.CustomEndpoint)
                 .Returns("https://app.random-uuid.polaris.snyk.io/api");
            var apiEndpointResolver = new ApiEndpointResolver(optionsMock.Object);

            var snykCodeApiUrl = apiEndpointResolver.GetSnykCodeApiUrl();

            Assert.Equal("https://deeproxy.random-uuid.polaris.snyk.io/", snykCodeApiUrl);
        }

        [Fact]
        public void ApiEndpointResolver_GetSnykCodeApiUrl_Snykgov()
        {
            var optionsMock = new Mock<ISnykOptions>();
            optionsMock
                 .Setup(options => options.CustomEndpoint)
                 .Returns("https://app.random-uuid.polaris.snykgov.io/api");
            var apiEndpointResolver = new ApiEndpointResolver(optionsMock.Object);

            var snykCodeApiUrl = apiEndpointResolver.GetSnykCodeApiUrl();

            Assert.Equal("https://deeproxy.random-uuid.polaris.snykgov.io/", snykCodeApiUrl);
        }

        [Fact]
        public void AuthenticationMethod()
        {
            // Arrange
            var optionsMock = new Mock<ISnykOptions>();
            var apiEndpointResolver = new ApiEndpointResolver(optionsMock.Object);

            // Assert
            Assert.Equal(AuthenticationType.Token, apiEndpointResolver.AuthenticationMethod);

            optionsMock
                 .Setup(options => options.CustomEndpoint)
                 .Returns("https://app.snykgov.io/api");

            // Assert
            Assert.Equal(AuthenticationType.OAuth, apiEndpointResolver.AuthenticationMethod);
        }
    }
}
