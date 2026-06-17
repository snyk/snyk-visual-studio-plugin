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
            html = InjectIdeStyle(html);

            // base fills ${nonce}/ideNonce and clears ${ideStyle}'s sibling ${ideScript}
            // (the __ideExecuteCommand__ bridge is injected via WebView2 init scripts instead).
            return base.ReplaceCssVariables(html);
        }

        // internal for testability (InternalsVisibleTo): substitutes the tree template's
        // ${ideStyle} hook with the shared scrollbar styling, which overrides the thin dark
        // scrollbar the LS tree HTML ships so the tree matches the summary and description panels.
        // The block is nonce'd (${nonce} is filled later by the base provider) because the page CSP
        // only allows nonce'd inline styles.
        internal string InjectIdeStyle(string html)
        {
            var css = "<style nonce=\"${nonce}\">" + GetScrollbarCss() + "</style>";
            return html.Replace("${ideStyle}", css);
        }
    }
}
