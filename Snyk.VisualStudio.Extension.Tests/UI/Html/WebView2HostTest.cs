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
        public void BuildUserDataFolder_ReturnsDistinctPathsPerContextKey()
        {
            // Production keys: "toolwindow" (shared by description + summary panels) and
            // "settings" (the modal dialog, isolated because of DPI-awareness mismatch).
            var toolwindow = WebView2Host.BuildUserDataFolder("toolwindow");
            var settings = WebView2Host.BuildUserDataFolder("settings");

            Assert.NotEqual(toolwindow, settings);
            Assert.EndsWith(Path.DirectorySeparatorChar + "toolwindow", toolwindow);
            Assert.EndsWith(Path.DirectorySeparatorChar + "settings", settings);
        }

        [Fact]
        public void BuildUserDataFolder_SameKeyReturnsSamePath()
        {
            // Two panels using the same key (e.g. description and summary both passing
            // "toolwindow") must land on the exact same path so they share a CoreWebView2Environment.
            Assert.Equal(
                WebView2Host.BuildUserDataFolder("toolwindow"),
                WebView2Host.BuildUserDataFolder("toolwindow"));
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

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("about:blank")]
        [InlineData("data:text/html,<p>hi</p>")]
        public void IsAllowedDocumentUri_AllowsEmptyAboutAndData(string uri)
        {
            Assert.True(WebView2Host.IsAllowedDocumentUri(uri, @"C:\some\webview2\folder"));
        }

        [Theory]
        [InlineData("https://evil.example.com")]
        [InlineData("http://localhost:8080")]
        [InlineData("ftp://example.com/x")]
        [InlineData("javascript:alert(1)")]
        public void IsAllowedDocumentUri_BlocksOffOriginNavigation(string uri)
        {
            Assert.False(WebView2Host.IsAllowedDocumentUri(uri, @"C:\some\webview2\folder"));
        }

        [Fact]
        public void IsAllowedDocumentUri_AllowsFileUnderUserDataFolder_ButNotOutsideIt()
        {
            var folder = Path.Combine(Path.GetTempPath(), "snyk-webview2-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var inside = new Uri(Path.Combine(folder, "index.html")).AbsoluteUri;
                var outside = new Uri(Path.Combine(Path.GetTempPath(), "outside-" + Guid.NewGuid().ToString("N") + ".html")).AbsoluteUri;

                Assert.True(WebView2Host.IsAllowedDocumentUri(inside, folder));
                Assert.False(WebView2Host.IsAllowedDocumentUri(outside, folder));
            }
            finally
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
