using Microsoft.VisualStudio.PlatformUI;
using System;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    public class OssHtmlProvider : BaseHtmlProvider
    {
        private static OssHtmlProvider _instance;

        public static OssHtmlProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new OssHtmlProvider();
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
            html =  base.ReplaceCssVariables(html);
            html = html.Replace("var(--container-background-color)", VSColorTheme.GetThemedColor(EnvironmentColors.EditorExpansionFillBrushKey).ToHex());

            return html;
        }
    }
}