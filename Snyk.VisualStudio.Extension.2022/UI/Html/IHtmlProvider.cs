namespace Snyk.VisualStudio.Extension.UI.Html
{
    public interface IHtmlProvider
    {
        string GetCss();
        string GetJs();
        string GetInitScript();
        string GetNonce();
        string ReplaceCssVariables(string html);
    }
}