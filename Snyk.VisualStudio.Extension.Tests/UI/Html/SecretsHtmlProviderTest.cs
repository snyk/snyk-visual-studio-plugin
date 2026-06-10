using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    public class SecretsHtmlProviderTest
    {
        [Fact]
        public void HtmlProviderFactory_ReturnsSecretsHtmlProvider_ForSecretsProduct()
        {
            // Arrange & Act
            var provider = HtmlProviderFactory.GetHtmlProvider(Product.Secrets);

            // Assert
            Assert.IsType<SecretsHtmlProvider>(provider);
        }

        [Fact]
        public void ReplaceCssVariables_SubstitutesSubmitIgnoreRequestBridge()
        {
            // Arrange
            var html = "<button onclick=\"${ideSubmitIgnoreRequest}\">Ignore</button>";
            var provider = SecretsHtmlProvider.Instance;

            // Act
            var result = provider.ReplaceCssVariables(html);

            // Assert
            Assert.Contains("window.SubmitIgnoreRequest(issueId, ignoreType, ignoreReason, ignoreExpirationDate)", result);
            Assert.DoesNotContain("${ideSubmitIgnoreRequest}", result);
        }
    }
}
