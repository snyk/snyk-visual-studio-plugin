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
             if(document.getElementById('totalIssues')){
                document.getElementById('totalIssues').onclick = function(e) {
                    window.external.ToggleDelta(false);
                    };
                }
            if(document.getElementById('newIssues')){
                document.getElementById('newIssues').onclick = function(e) {
                    window.external.ToggleDelta(true);
                    };
            }
            ";
        }

        public override string ReplaceCssVariables(string html)
        {
            html = base.ReplaceCssVariables(html);

            return html;
        }

    }
}