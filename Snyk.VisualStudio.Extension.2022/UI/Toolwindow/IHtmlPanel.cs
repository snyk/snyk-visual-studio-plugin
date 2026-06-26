namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Seam for a WebView2-backed HTML panel that renders LS-provided HTML for a given product
    /// (<c>summary</c>, <c>code</c>, <c>oss</c>, <c>iac</c>). Lets the Language Server message
    /// handlers be unit-tested without constructing the WPF control.
    /// </summary>
    public interface IHtmlPanel
    {
        void SetContent(string html, string product);
    }
}
