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

            html = html.Replace("${ideFunc}", "window.external.EnableDelta(isEnabled);");
            html = base.ReplaceCssVariables(html);

            return html;
        }

        public override string GetCss()
        {
            return @"
            body { overflow: hidden; }
            .body-padding { padding: 0px 4px 8px 4px; }
            ";
        }
    }
}