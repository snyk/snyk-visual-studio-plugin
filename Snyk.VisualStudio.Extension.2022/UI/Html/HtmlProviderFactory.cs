namespace Snyk.VisualStudio.Extension.UI.Html
{
    public static class HtmlProviderFactory
    {
        public static IHtmlProvider GetHtmlProvider(string provider)
        {
            switch (provider)
            {
                case Product.Code:
                    return CodeHtmlProvider.Instance;
                case Product.Oss:
                    return OssHtmlProvider.Instance;
                case Product.Iac:
                    return IacHtmlProvider.Instance;
                case "summary":
                    return SummaryHtmlProvider.Instance;
                case "static":
                    return StaticHtmlProvider.Instance;
                default:
                    return null;
            }
        }
    }
}