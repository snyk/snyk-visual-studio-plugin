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
            var css = "<style nonce=\"${nonce}\">";
            css += GetCss();
            css += "</style>";
            html = html.Replace("${ideStyle}", css);
            return base.ReplaceCssVariables(html);
        }
    }
}