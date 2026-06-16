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
            // The only override we need is the shared scrollbar styling, which overrides the
            // thin dark scrollbar the LS tree HTML ships so the tree matches the summary and
            // description panels (the page CSP only allows nonce'd inline styles).
            var css = "<style nonce=\"${nonce}\">" + GetScrollbarCss() + "</style>";
            html = html.Replace("${ideStyle}", css);

            // base fills ${nonce}/ideNonce and clears ${ideStyle}'s sibling ${ideScript}
            // (the __ideExecuteCommand__ bridge is injected via WebView2 init scripts instead).
            return base.ReplaceCssVariables(html);
        }
    }
}
