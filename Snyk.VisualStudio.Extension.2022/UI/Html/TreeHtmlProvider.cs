namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// HTML provider for the LS-rendered issue tree view (the <c>$/snyk.treeView</c> payload).
    /// The tree HTML ships its own styles/script inline; this provider only fills the
    /// IDE-injection placeholders (<c>${ideStyle}</c>, <c>${nonce}</c>, <c>${ideScript}</c>)
    /// and themes the <c>--vscode-*</c> CSS variables to the active VS theme — mirroring the
    /// description and summary panel providers.
    /// </summary>
    public class TreeHtmlProvider : BaseHtmlProvider
    {
        private static TreeHtmlProvider _instance;

        public static TreeHtmlProvider Instance => _instance ??= new TreeHtmlProvider();

        public override string ReplaceCssVariables(string html)
        {
            // The tree template uses ${ideStyle} as a hook for IDE-specific style overrides.
            // We have none beyond the themed CSS variables base handles, so emit an empty
            // nonce'd style block (the page CSP only allows nonce'd inline styles).
            var css = "<style nonce=\"${nonce}\"></style>";
            html = html.Replace("${ideStyle}", css);

            // base fills ${nonce}/ideNonce and clears ${ideStyle}'s sibling ${ideScript}
            // (the __ideExecuteCommand__ bridge is injected via WebView2 init scripts instead).
            return base.ReplaceCssVariables(html);
        }
    }
}
