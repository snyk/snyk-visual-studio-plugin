using Snyk.VisualStudio.Extension.Authentication;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Authentication
{
    public class AuthenticationTokenTests
    {
        [Theory]
        [InlineData("snyk_uat.12ab34cd.abcDEF-_123.XYZ-xyz_789")]
        [InlineData("snyk_sat.abcdefgh.a.b")]
        public void IsValid_WellFormedPat_ReturnsTrue(string value)
        {
            Assert.True(new AuthenticationToken(AuthenticationType.Pat, value).IsValid());
        }

        [Theory]
        [InlineData("")]
        [InlineData("snyk_uat.short.a.b")]
        [InlineData("snyk_xxx.12ab34cd.a.b")]
        [InlineData("12345678-1234-1234-1234-123456789012")]
        public void IsValid_MalformedPat_ReturnsFalse(string value)
        {
            Assert.False(new AuthenticationToken(AuthenticationType.Pat, value).IsValid());
        }

        [Fact]
        public void IsValid_ExpiredOAuthToken_ReturnsFalse()
        {
            var expired = "{\"access_token\":\"a\",\"token_type\":\"Bearer\",\"refresh_token\":\"r\",\"expiry\":\"2000-01-01T00:00:00Z\"}";
            Assert.False(new AuthenticationToken(AuthenticationType.OAuth, expired).IsValid());
        }

        [Fact]
        public void IsValid_UnexpiredOAuthToken_ReturnsTrue()
        {
            var valid = "{\"access_token\":\"a\",\"token_type\":\"Bearer\",\"refresh_token\":\"r\",\"expiry\":\"2099-01-01T00:00:00Z\"}";
            Assert.True(new AuthenticationToken(AuthenticationType.OAuth, valid).IsValid());
        }
    }
}
