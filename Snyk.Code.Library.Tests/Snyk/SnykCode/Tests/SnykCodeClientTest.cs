namespace Snyk.SnykCode.Tests
{
    using System;
    using SnykCode;    
    using Xunit;

    public class SnykCodeClientTest
    {
        private const string TestUserAgent = "Test-VisualStudio";

        [Fact]
        public void SnykCodeClient_ProperLoginDataProvided_ChecksPass()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);
            
            LoginResponse response = snykCodeClient.LoginAsync(TestUserAgent).Result;

            Assert.NotNull(response);
            Assert.NotEmpty(response.SessionToken);
        }

        [Fact]
        public void SnykCodeClient_WrongPayloadProvided_ChecksFailed()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, string.Empty);

            Assert.Throws<AggregateException>(() => snykCodeClient.LoginAsync("\\{").Result);            
        }

        [Fact]
        public void SnykCodeClient_ChessSessionProperApiTokenProvided_CheckPass()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            _ = snykCodeClient.LoginAsync(TestUserAgent).Result;

            LoginStatus status = snykCodeClient.CheckSessionAsync().Result;

            Assert.True(status.IsSucccess);
        }
    }
}
