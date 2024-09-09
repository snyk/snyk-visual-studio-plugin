namespace Snyk.VisualStudio.Extension.UI.Html
{
    public static class HtmlProviderFactory
    {
        public static IHtmlProvider GetHtmlProvider(string product)
        {
            switch (product)
            {
                case Product.Code:
                    return CodeHtmlProvider.Instance;
                case Product.Oss:
                    return OssHtmlProvider.Instance;
                case Product.Iac:
                    return IacHtmlProvider.Instance;
            }

            return null;
        }
    }
}