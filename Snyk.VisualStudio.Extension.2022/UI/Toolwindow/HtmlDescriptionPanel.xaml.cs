using System.Windows;
using System.Windows.Navigation;
using Snyk.VisualStudio.Extension.UI.Html;
using UserControl = System.Windows.Controls.UserControl;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    public partial class HtmlDescriptionPanel : UserControl
    {
        private IHtmlProvider htmlProvider;
        private WebBrowserHostUIHandler _wbHandler;

        public HtmlDescriptionPanel()
        {
            this.InitializeComponent();

            _wbHandler = new WebBrowserHostUIHandler(HtmlViewer)
            {
                IsWebBrowserContextMenuEnabled = false,
                ScriptErrorsSuppressed = true,
            };

            HtmlViewer.ObjectForScripting = new SnykScriptManager(SnykVSPackage.ServiceProvider);
            _wbHandler.LoadCompleted += HtmlViewerOnLoadCompleted;
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
            HtmlViewer.Visibility = Visibility.Visible;

            this.htmlProvider = HtmlProviderFactory.GetHtmlProvider(product);
            if (this.htmlProvider == null)
                return;
            HtmlViewer.InvalidateVisual();
            HtmlViewer.UpdateLayout();
            html = htmlProvider.ReplaceCssVariables(html);
            HtmlViewer.NavigateToString(html);
        }

        public void Init()
        {
            HtmlViewer.Visibility = Visibility.Collapsed;
            HtmlViewer.NavigateToString("<html><body style='margin:0;padding:0;'>Loading...</body></html>");
            HtmlViewer.InvalidateVisual();
            HtmlViewer.UpdateLayout();
        }
    }
}

