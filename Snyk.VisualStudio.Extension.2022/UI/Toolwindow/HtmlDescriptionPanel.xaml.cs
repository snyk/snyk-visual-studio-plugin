using System.Windows.Forms;
using Snyk.VisualStudio.Extension.UI.Html;
using UserControl = System.Windows.Controls.UserControl;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    public partial class HtmlDescriptionPanel : UserControl
    {
        private IHtmlProvider htmlProvider;
        private WebBrowser HtmlViewer;

        public HtmlDescriptionPanel()
        {
            this.InitializeComponent();

            HtmlViewer = new WebBrowser
            {
                IsWebBrowserContextMenuEnabled = false,
                ScriptErrorsSuppressed = true
            };

            HtmlViewer.ObjectForScripting = new SnykScriptManager();
            HtmlViewer.Navigated += HtmlViewerOnLoadCompleted;
            windowsFormsHost.Child = HtmlViewer;
        }

        private void HtmlViewerOnLoadCompleted(object sender, WebBrowserNavigatedEventArgs e)
        {
            try
            {
                if (htmlProvider == null)
                    return;
                HtmlViewer.Document?.InvokeScript("eval", new string[] { htmlProvider.GetInitScript() });
            }
            catch
            {

            }
        }

        public void SetContent(string html, string product)
        {
            if (string.IsNullOrEmpty(html))
                return;
            this.htmlProvider = HtmlProviderFactory.GetHtmlProvider(product);
            if (this.htmlProvider == null)
                return;

            html = htmlProvider.ReplaceCssVariables(html);
            HtmlViewer.DocumentText = html;
        }
    }
}

