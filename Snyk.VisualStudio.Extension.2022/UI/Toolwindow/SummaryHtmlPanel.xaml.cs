using System.Windows;
using System.Windows.Navigation;
using Snyk.VisualStudio.Extension.UI.Html;
using UserControl = System.Windows.Controls.UserControl;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    public partial class SummaryHtmlPanel : UserControl
    {
        private IHtmlProvider htmlProvider;
        private WebBrowserHostUIHandler _wbHandler;

        public SummaryHtmlPanel()
        {
            this.InitializeComponent();

            _wbHandler = new WebBrowserHostUIHandler(SummaryHtmlViewer)
            {
                IsWebBrowserContextMenuEnabled = false,
                ScriptErrorsSuppressed = true,
            };

            SummaryHtmlViewer.ObjectForScripting = new SnykScriptManager(SnykVSPackage.ServiceProvider);
            _wbHandler.LoadCompleted += HtmlViewerOnLoadCompleted;
        }

        private void HtmlViewerOnLoadCompleted(object sender, NavigationEventArgs e)
        {
            try
            {
                if (htmlProvider == null)
                    return;
                SummaryHtmlViewer.InvokeScript("eval", new string[] { htmlProvider.GetInitScript() });
            }
            catch
            {

            }
        }
        public void SetContent(string html, string product)
        {
            if (string.IsNullOrEmpty(html))
                return;

            this.htmlProvider = HtmlProviderFactory.GetHtmlProvider("summary");
            if (this.htmlProvider == null)
                return;
            html = htmlProvider.ReplaceCssVariables(html);
            SummaryHtmlViewer.NavigateToString(html);
            SummaryHtmlViewer.InvalidateVisual();
            SummaryHtmlViewer.UpdateLayout();
        }

        public void Init()
        {
            var provider = (StaticHtmlProvider) HtmlProviderFactory.GetHtmlProvider("static");
            SummaryHtmlViewer.NavigateToString(provider.ReplaceCssVariables(provider.GetInitHtml()));
            
            SummaryHtmlViewer.InvalidateVisual();
            SummaryHtmlViewer.UpdateLayout();
        }

        
    }
}

