using Snyk.VisualStudio.Extension.Language;
using System.Windows.Controls;
using System.Windows.Navigation;
using Snyk.VisualStudio.Extension.UI.Html;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Interaction logic for DescriptionPanel.xaml.
    /// </summary>
    public partial class HtmlDescriptionPanel : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptionPanel"/> class. For OSS scan result.
        /// </summary>
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


        public void SetContent(Issue issue, string product)
        {
            this.htmlProvider = HtmlProviderFactory.GetHtmlProvider(product);
            var html = issue.AdditionalData.Details;

            html = htmlProvider.ReplaceCssVariables(html);

            HtmlViewer.NavigateToString(html);
        }
    }
}

