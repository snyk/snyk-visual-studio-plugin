using System.Text.RegularExpressions;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    /// <summary>
    /// Covers the tree provider's two IDE-injection responsibilities without the full
    /// <see cref="BaseHtmlProvider.ReplaceCssVariables"/> path (which reaches VSColorTheme and so
    /// needs a live VS shell): the <c>${ideStyle}</c> hook substitution and the <c>${nonce}</c>
    /// interpolation that makes the injected style pass the page CSP.
    /// </summary>
    public class TreeHtmlProviderTest
    {
        private readonly TreeHtmlProvider _provider = TreeHtmlProvider.Instance;

        [Fact]
        public void InjectIdeStyle_ReplacesIdeStyleHookWithNoncedScrollbarStyle()
        {
            var result = _provider.InjectIdeStyle("<head>${ideStyle}</head>");

            Assert.DoesNotContain("${ideStyle}", result);
            // Nonce'd inline style block, with the nonce still a placeholder for the base provider.
            Assert.Contains("<style nonce=\"${nonce}\">", result);
            // The shared scrollbar styling the tree needs to match the other panels.
            Assert.Contains("::-webkit-scrollbar", result);
        }

        [Fact]
        public void InjectIdeStyle_IsNoOp_WhenHookAbsent()
        {
            const string html = "<head></head>";

            Assert.Equal(html, _provider.InjectIdeStyle(html));
        }

        [Fact]
        public void ReplacePlaceholders_InterpolatesA32CharNonceIntoInjectedStyle()
        {
            var injected = _provider.InjectIdeStyle("<head>${ideStyle}</head>");

            var result = _provider.ReplacePlaceholders(injected);

            Assert.DoesNotContain("${nonce}", result);
            var match = Regex.Match(result, "<style nonce=\"(?<nonce>[A-Za-z0-9]{32})\">");
            Assert.True(match.Success, "expected the injected style to carry a 32-char interpolated nonce");
        }
    }
}
