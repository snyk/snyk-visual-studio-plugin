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
                 .Returns("https://registry-web.random-uuid.polaris.snyk-internal.net/api");
            var apiEndpointResolver = new ApiEndpointResolver(optionsMock.Object);
            
            var snykCodeApiUrl = apiEndpointResolver.GetSnykCodeApiUrl();

            Assert.Equal("https://deeproxy.random-uuid.polaris.snyk-internal.net/", snykCodeApiUrl);
        }
    }
}
