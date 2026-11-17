using System.Text.RegularExpressions;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Removes Snyk Learn UI from language-server HTML before it reaches WebView2.
    /// </summary>
    internal static class SnykLearnHtmlFilter
    {
        private const RegexOptions Options =
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline;

        // Matches the Learn component currently emitted by both the Code and Open Source
        // issue-description templates. The Code variant contains an icon div before the anchor;
        // matching through the lesson anchor before the component's closing tag handles both shapes.
        private static readonly Regex LearnComponentPattern = new Regex(
            @"<(?<tag>[a-z][a-z0-9]*)\b" +
            @"(?=[^>]*\bclass\s*=\s*(?<quote>[""'])[^""']*\bsn-learn\b[^""']*\k<quote>)" +
            @"[^>]*>.*?</a\s*>\s*</\k<tag>\s*>",
            Options);

        // Defense in depth for older or future templates that render the lesson link without the
        // known wrapper. Restrict the match to the exact Learn host so all other external links stay.
        private static readonly Regex LearnAnchorPattern = new Regex(
            @"<a\b" +
            @"(?=[^>]*\bhref\s*=\s*(?<quote>[""'])\s*https?://learn\.snyk\.io" +
            @"(?=[/?:#]|\s*\k<quote>)[^""']*\k<quote>)" +
            @"[^>]*>.*?</a\s*>",
            Options);

        internal static string RemoveSnykLearn(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }

            html = LearnComponentPattern.Replace(html, string.Empty);
            return LearnAnchorPattern.Replace(html, string.Empty);
        }
    }
}
