namespace Snyk.Common.Tests
{
    using Snyk.Common.Authentication;
    using System.Linq;
    using Xunit;

    public class HttpClientFactoryTest
    {
        [Fact]
        public void NewHttpClient_SetsTokenAuthorizationHeader()
        {
            var token = new AuthenticationToken(AuthenticationType.Token, "default-token");
            var httpClient = HttpClientFactory.NewHttpClient(token);

            var authorizationHeader = httpClient.DefaultRequestHeaders.GetValues("Authorization");
            
            Assert.Equal("token default-token", authorizationHeader.First());
        }

        [Fact]
        public void NewHttpClient_SetsOAuthTokenAuthorizationHeader()
        {
            var token = new AuthenticationToken(AuthenticationType.OAuth, "{\"access_token\":\"at\",\"token_type\":\"Bearer\",\"refresh_token\":\"rt\",\"expiry\":\"2023-04-13T19:07:08.8871+02:00\"}");
            var httpClient = HttpClientFactory.NewHttpClient(token);

            var authorizationHeader = httpClient.DefaultRequestHeaders.GetValues("Authorization");

            Assert.Equal("bearer at", authorizationHeader.First());
        }
    }
}
