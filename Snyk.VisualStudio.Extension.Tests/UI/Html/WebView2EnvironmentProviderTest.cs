using System;
using System.Diagnostics;
using System.IO;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    public class WebView2EnvironmentProviderTest
    {
        [Fact]
        public void GetScratchDirectory_ReturnsPidScopedPathPerPanel()
        {
            var settingsPath = WebView2EnvironmentProvider.GetScratchDirectory("settings");
            var descriptionPath = WebView2EnvironmentProvider.GetScratchDirectory("description");

            Assert.NotEqual(settingsPath, descriptionPath);
            Assert.EndsWith(Path.Combine("settings"), settingsPath);
            Assert.EndsWith(Path.Combine("description"), descriptionPath);
        }

        [Fact]
        public void GetScratchDirectory_IncludesCurrentProcessId()
        {
            var pid = Process.GetCurrentProcess().Id.ToString();

            var path = WebView2EnvironmentProvider.GetScratchDirectory("settings");

            Assert.Contains(Path.DirectorySeparatorChar + pid + Path.DirectorySeparatorChar, path);
        }

        [Fact]
        public void GetScratchDirectory_RootsUnderLocalAppDataSnykWebView2()
        {
            var expectedRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Snyk", "WebView2");

            var path = WebView2EnvironmentProvider.GetScratchDirectory("settings");

            Assert.StartsWith(expectedRoot, path);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetScratchDirectory_EmptyPanelKey_Throws(string panelKey)
        {
            Assert.Throws<ArgumentException>(() => WebView2EnvironmentProvider.GetScratchDirectory(panelKey));
        }
    }
}
