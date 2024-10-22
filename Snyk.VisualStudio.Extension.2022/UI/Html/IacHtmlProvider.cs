using Microsoft.VisualStudio.PlatformUI;
using System;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    public class IacHtmlProvider : BaseHtmlProvider
    {
        private static IacHtmlProvider _instance;

        public static IacHtmlProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new IacHtmlProvider();
                }
                return _instance;
            }
        }

        public override string ReplaceCssVariables(string html)
        {
            html = base.ReplaceCssVariables(html);
            html = html.Replace("var(--container-background-color)", VSColorTheme.GetThemedColor(EnvironmentColors.ClassDesignerConnectionRouteBorderBrushKey).ToHex());

            return html;
        }
    }
}