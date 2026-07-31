using System.Text.RegularExpressions;
using Moq;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    /// <summary>
    /// Rendering of the CLI settings into the fallback form. Values are read out of the markup rather
    /// than matched literally, because that file is synced verbatim from snyk-ls.
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
            // Blank so the field shows its placeholder; the default rendered here would be
            // indistinguishable from a configured value.
            var html = HtmlResourceLoader.LoadFallbackHtml(Options(unset, unset), forceLight: true);

            Assert.Equal(string.Empty, RenderedBaseDownloadUrl(html));
            Assert.DoesNotContain("{{CLI_BASE_DOWNLOAD_URL}}", html);
        }

        [Fact]
        public void LoadFallbackHtml_RendersTheConfiguredValue_EvenWhenItIsNotUsable()
        {
            // A mis-typed mirror must stay visible so it can be corrected.
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
