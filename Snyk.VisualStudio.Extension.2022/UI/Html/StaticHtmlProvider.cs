namespace Snyk.VisualStudio.Extension.UI.Html
{
    public partial class StaticHtmlProvider : BaseHtmlProvider
    {
        private static StaticHtmlProvider _instance;

        public static StaticHtmlProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new StaticHtmlProvider();
                }
                return _instance;
            }
        }
        public override string GetInitScript()
        {
          return @"";
        }

        public override string ReplaceCssVariables(string html)
        {
            // The shared LS HTML uses --main-font-size as the root font-size hook, deliberately
            // leaving it undefined so each IDE supplies a value through ${ideStyle}. JCEF/CEF-based
            // IDEs scale CSS values internally, so an unset variable falls back to a renderer
            // default that looks right after their scaling. WebView2 renders at face value with no
            // IDE-applied scale factor, so we have to anchor the root size explicitly.
            // The page applies `font-size: 1.3rem` to <p>, so root at 10px renders the loader
            // text at ~13px — matching the body font-size we used to hard-code in this file.
            // GetScrollbarCss() themes the loader's scrollbar to match the summary/tree/description
            // panels, so the "Snyk Security is loading" banner doesn't briefly flash the default
            // Chromium scrollbar before the real summary content swaps in.
            var ideStyleOverride =
                "<style nonce=\"ideNonce\">:root { --main-font-size: 10px; }" + GetScrollbarCss() + "</style>";
            html = html.Replace("${ideStyle}", ideStyleOverride);
            return base.ReplaceCssVariables(html);
        }
    }
}