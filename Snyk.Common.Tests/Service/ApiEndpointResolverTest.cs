using System;
using Moq;
using Xunit;
using Snyk.Common.Authentication;
using Snyk.Common.Settings;
using Snyk.Common.Service;

namespace Snyk.Common.Tests.Service
{
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
        public void ApiEndpointResolver_GetSnykCodeApiUrl_Snykgov_NoOrg()
        {
            var optionsMock = new Mock<ISnykOptions>();
            optionsMock
                 .Setup(options => options.CustomEndpoint)
                 .Returns("https://app.random-uuid.polaris.snykgov.io/api");
            optionsMock
                .Setup(options => options.IsFedramp())
                .Returns(true);

            var apiEndpointResolver = new ApiEndpointResolver(optionsMock.Object);
            
            Assert.Throws<InvalidOperationException>(() => apiEndpointResolver.GetSnykCodeApiUrl());
        }

        [Fact]
        public void ApiEndpointResolver_GetSnykCodeApiUrl_Snykgov()
        {
            var optionsMock = new Mock<ISnykOptions>();
            optionsMock
                .Setup(options => options.CustomEndpoint)
                .Returns("https://app.random-uuid.polaris.snykgov.io/api");
            optionsMock
                .Setup(options => options.IsFedramp())
                .Returns(true);
            optionsMock
                .Setup(options => options.Organization)
                .Returns("dummy-org-name");

            var apiEndpointResolver = new ApiEndpointResolver(optionsMock.Object);

            var snykCodeApiUrl = apiEndpointResolver.GetSnykCodeApiUrl();

            Assert.Equal("https://api.random-uuid.polaris.snykgov.io/hidden/orgs/dummy-org-name/code/", snykCodeApiUrl);
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
            optionsMock
                .Setup(option => option.AuthenticationMethod)
                .Returns(AuthenticationType.OAuth);

            // Assert
            Assert.Equal(AuthenticationType.OAuth, apiEndpointResolver.AuthenticationMethod);
        }

        [Fact]
        public void ApiEndpointResolver_GetSnykCodeApiUrl_LocalEngine()
        {
            SastSettings mockedSettings = new SastSettings
            {
                SastEnabled = true,
                LocalCodeEngine = new LocalCodeEngine
                {
                    Enabled = true,
                    Url = "http://foo.bar/api"
                }
            };


            var optionsMock = new Mock<ISnykOptions>();
            optionsMock
                .Setup(options => options.SastSettings)
                .Returns(mockedSettings);

            var apiEndpointResolver = new ApiEndpointResolver(optionsMock.Object);

            var snykCodeApiUrl = apiEndpointResolver.GetSnykCodeApiUrl();

            Assert.Equal("http://foo.bar/api/", snykCodeApiUrl);
        }
    }
}
