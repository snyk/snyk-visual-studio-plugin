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
        public override string GetInitScript()
        {
            var initScript = base.GetInitScript();
            return initScript + @"
             // Hide scrollbar
             document.body.style.overflow = 'hidden';
            ";
        }
      
        public override string ReplaceCssVariables(string html)
        {
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