using Moq;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    /// <summary>
    /// Rendering of the CLI settings into the fallback settings form.
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

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void LoadFallbackHtml_RendersTheEffectiveCliSettings_WhenOptionsAreUnset(string unset)
        {
            // Options can still hold an empty value — a $/snyk.configuration echo lands before the next
            // save normalises it. Rendering that as a blank field, while every other consumer resolves to
            // the default, tells the user something untrue about their own configuration.
            var html = HtmlResourceLoader.LoadFallbackHtml(Options(unset, unset), forceLight: true);

            Assert.Contains($"name=\"binary_base_url\" value=\"{SnykCliDownloader.DefaultBaseDownloadUrl}\"", html);
            Assert.Contains($"<option value=\"{SnykCliDownloader.DefaultReleaseChannel}\" selected>", html);
        }

        [Fact]
        public void LoadFallbackHtml_RendersConfiguredCliSettings()
        {
            var html = HtmlResourceLoader.LoadFallbackHtml(Options("https://downloads.snyk.io/fips", "preview"), forceLight: true);

            Assert.Contains("name=\"binary_base_url\" value=\"https://downloads.snyk.io/fips\"", html);
            Assert.Contains("<option value=\"preview\" selected>", html);
        }
    }
}
