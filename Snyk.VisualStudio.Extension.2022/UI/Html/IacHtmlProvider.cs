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
    }
}