namespace Snyk.SnykCode.Tests
{
    using SnykCode;
    using Xunit;

    public class SnykCodeClientTest
    {
        private const string TestUserAgent = "Test-VisualStudio";

        [Fact]
        public void LoginSuccess()
        {
            var snykCodeClient = new SnykCodeClient();

            LoginResponse response = snykCodeClient.LoginAsync(TestUserAgent).Result;

            Assert.NotNull(response);
            Assert.NotEmpty(response.SessionToken);
        }

        [Fact]
        public void LoginFailed()
        {
            var snykCodeClient = new SnykCodeClient();

            LoginResponse response = snykCodeClient.LoginAsync("\\{").Result;

            Assert.False(response.IsSuccess);
        }

        [Fact]
        public void CheckSessionSuccess()
        {
            var snykCodeClient = new SnykCodeClient();

            _ = snykCodeClient.LoginAsync(TestUserAgent).Result;

            LoginStatus status = snykCodeClient.CheckSessionAsync(Settings.Instance.ApiToken).Result;

            Assert.True(status.IsSucccess);
        }
    }
}
