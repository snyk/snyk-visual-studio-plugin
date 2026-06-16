using Snyk.VisualStudio.Extension.Extension;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Extension
{
    public class UriExtensionsTest
    {
        [Theory]
        [InlineData("https://api.snyk.io", true)]
        [InlineData("https://api.eu.snyk.io/rest", true)]
        [InlineData("http://localhost:8080", true)]
        [InlineData("ftp://example.com", false)]
        [InlineData("file:///C:/x.html", false)]
        [InlineData("javascript:alert(1)", false)]
        [InlineData("httpx://evil", false)]
        [InlineData("not a url", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidWebUrl_AllowsOnlyAbsoluteHttpAndHttps(string value, bool expected)
        {
            Assert.Equal(expected, UriExtensions.IsValidWebUrl(value));
        }
    }
}
