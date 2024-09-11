using Snyk.VisualStudio.Extension.Language;
using System.Windows.Controls;
using System.Windows.Navigation;
using Snyk.VisualStudio.Extension.UI.Html;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    public partial class HtmlDescriptionPanel : UserControl
    {
        private IHtmlProvider htmlProvider;
        public HtmlDescriptionPanel()
        {
            this.InitializeComponent();
            if (HtmlViewer.ContextMenu != null)
            {
                HtmlViewer.ContextMenu.IsEnabled = false;
#if DEBUG
                HtmlViewer.ContextMenu.IsEnabled = true;
#endif
            }

            HtmlViewer.ObjectForScripting = new SnykScriptManager();
            HtmlViewer.LoadCompleted += HtmlViewerOnLoadCompleted;
        }

        private void HtmlViewerOnLoadCompleted(object sender, NavigationEventArgs e)
        {
            try
            {
                if (htmlProvider == null)
                    return;
                HtmlViewer.InvokeScript("eval", new string[] { htmlProvider.GetInitScript() });
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

            HtmlViewer.NavigateToString(html);
        }
    }
}

