using System;
using System.Diagnostics;
using System.IO;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    /// <summary>
    /// Path-shape tests for <see cref="WebView2Host.BuildUserDataFolder"/>. The orchestrator
    /// itself is hard to unit-test (requires WPF + a real WebView2 runtime), so only the
    /// pure helper is covered here.
    /// </summary>
    public class WebView2HostTest
    {
        [Fact]
        public void BuildUserDataFolder_ReturnsPidNestedPathPerPanel()
        {
            var settings = WebView2Host.BuildUserDataFolder("settings");
            var description = WebView2Host.BuildUserDataFolder("description");

            Assert.NotEqual(settings, description);
            Assert.EndsWith(Path.DirectorySeparatorChar + "settings", settings);
            Assert.EndsWith(Path.DirectorySeparatorChar + "description", description);
        }

        [Fact]
        public void BuildUserDataFolder_PutsPidBetweenWebView2RootAndPanelKey()
        {
            var pid = Process.GetCurrentProcess().Id.ToString();

            var path = WebView2Host.BuildUserDataFolder("settings");

            Assert.Contains(
                Path.DirectorySeparatorChar + "WebView2" + Path.DirectorySeparatorChar + pid + Path.DirectorySeparatorChar,
                path);
        }

        [Fact]
        public void BuildUserDataFolder_RootsUnderLocalAppDataSnykWebView2()
        {
            var expectedRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Snyk", "WebView2");

            var path = WebView2Host.BuildUserDataFolder("settings");

            Assert.StartsWith(expectedRoot, path);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void BuildUserDataFolder_EmptyPanelKey_Throws(string panelKey)
        {
            Assert.Throws<ArgumentException>(() => WebView2Host.BuildUserDataFolder(panelKey));
        }
    }
}
