using Microsoft.Web.WebView2.Core;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    public class WebView2PackageSmokeTest
    {
        [Fact]
        public void CoreWebView2EnvironmentOptions_CanBeConstructed()
        {
            var options = new CoreWebView2EnvironmentOptions();

            Assert.NotNull(options);
        }

        [Fact]
        public void WpfWebView2_TypeIsAvailable()
        {
            var wpfType = typeof(Microsoft.Web.WebView2.Wpf.WebView2);

            Assert.Equal("Microsoft.Web.WebView2.Wpf", wpfType.Namespace);
        }
    }
}
