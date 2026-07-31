using System.Text.RegularExpressions;
using Moq;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    /// <summary>
    /// Rendering of the CLI settings into the fallback settings form. Assertions read the rendered
    /// values out of the markup rather than matching it literally: settings-fallback.html is synced
    /// verbatim from snyk-ls, so an upstream attribute reorder must not fail these tests.
    /// </summary>
    public class HtmlResourceLoaderTest
    {
        private static ISnykOptions Options(string baseDownloadUrl, string releaseChannel)
        {
            var options = new Mock<ISnykOptions>();
            options.SetupAllProperties();
            options.Object.CliBaseDownloadURL = baseDownloadUrl;
            options.Object.CliReleaseChannel = releaseChannel;
            return options.Object;
        }

        private static string RenderedBaseDownloadUrl(string html)
        {
            var input = Regex.Match(html, "<input[^>]*id=\"binary_base_url\"[^>]*>");
            Assert.True(input.Success, "the binary_base_url input must be present");

            return Regex.Match(input.Value, "value=\"([^\"]*)\"").Groups[1].Value;
        }

        private static string SelectedReleaseChannel(string html) =>
            Regex.Match(html, "<option value=\"([^\"]*)\"[^>]*selected").Groups[1].Value;

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void LoadFallbackHtml_RendersAnEmptyBaseUrl_WhenUnset(string unset)
        {
            // Unset must render blank so the field falls back to its placeholder, which already states
            // the default. Substituting the default here would be indistinguishable from the user
            // having configured it.
            var html = HtmlResourceLoader.LoadFallbackHtml(Options(unset, unset), forceLight: true);

            Assert.Equal(string.Empty, RenderedBaseDownloadUrl(html));
            Assert.DoesNotContain("{{CLI_BASE_DOWNLOAD_URL}}", html);
        }

        [Fact]
        public void LoadFallbackHtml_RendersTheConfiguredValue_EvenWhenItIsNotUsable()
        {
            // The user has to be able to see and correct a mis-typed mirror. Showing them
            // https://downloads.snyk.io instead would hide the mistake from the only person who can fix
            // it, while the download quietly used the public host.
            var html = HtmlResourceLoader.LoadFallbackHtml(Options("downlods.snyk.io", "stable"), forceLight: true);

            Assert.Equal("downlods.snyk.io", RenderedBaseDownloadUrl(html));
            Assert.NotEqual(SnykCliDownloader.DefaultBaseDownloadUrl, RenderedBaseDownloadUrl(html));
        }

        [Fact]
        public void LoadFallbackHtml_RendersConfiguredCliSettings()
        {
            var html = HtmlResourceLoader.LoadFallbackHtml(Options("https://downloads.snyk.io/fips", "preview"), forceLight: true);

            Assert.Equal("https://downloads.snyk.io/fips", RenderedBaseDownloadUrl(html));
            Assert.Equal("preview", SelectedReleaseChannel(html));
        }
    }
}
