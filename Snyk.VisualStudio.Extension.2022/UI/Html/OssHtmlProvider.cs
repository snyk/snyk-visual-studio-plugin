using System;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    public partial class OssHtmlProvider : BaseHtmlProvider
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

        /// <summary>
        /// Substitutes the Open Source panel's themed container colour. Implemented in
        /// OssHtmlProvider.Vs.cs; the variable is left in place where there is no IDE theme.
        /// </summary>
        static partial void ApplyIdeThemeColors(ref string html);

        public override string ReplaceCssVariables(string html)
        {
            var css = "<style nonce=\"${nonce}\">";
            css += GetCss();
            css += "</style>"; 
            html = html.Replace("${ideStyle}", css);
            html =  base.ReplaceCssVariables(html);
            ApplyIdeThemeColors(ref html);

            return html;
        }
    }
}