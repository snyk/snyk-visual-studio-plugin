using Microsoft.VisualStudio.PlatformUI;
using System;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    public class SummaryHtmlProvider : BaseHtmlProvider
    {
        private static SummaryHtmlProvider _instance;

        public static SummaryHtmlProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SummaryHtmlProvider();
                }
                return _instance;
            }
        }
        public override string ReplaceCssVariables(string html)
        {
            var css = "<style nonce=\"${nonce}\">";
            css += GetCss();
            css += "</style>";
            html = html.Replace("${ideStyle}", css);

            html = html.Replace("${ideFunc}", "window.EnableDelta(isEnabled);");
            html = base.ReplaceCssVariables(html);

            return html;
        }

        public override string GetCss()
        {
            // overflow-y: auto shows a vertical scrollbar only when the summary content is taller
            // than the (resizable) panel; overflow-x: hidden avoids a spurious horizontal bar.
            return @"
            body { overflow-x: hidden; overflow-y: auto; }
            .body-padding { padding: 0px 4px 8px 4px; }
            ";
        }
    }
}