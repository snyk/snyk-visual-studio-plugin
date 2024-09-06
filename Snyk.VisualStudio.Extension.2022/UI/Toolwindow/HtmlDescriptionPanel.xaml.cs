using Snyk.VisualStudio.Extension.Language;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.VisualStudio.Shell;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;
using Snyk.Common;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    public class HtmlResourcesProvider
    {
        public const string CodeCss = """"
                                     ::-webkit-scrollbar-thumb {
                                       background: var(--scrollbar-thumb-color);
                                     }

                                     ::-webkit-scrollbar-thumb:hover {
                                       background: #595a5c;
                                     }

                                     html, body {
                                       height: 100%;
                                       font-size: 16px;
                                       display: flex;
                                       flex-direction: column;
                                       margin: 0;
                                       padding: 0;
                                     }

                                     body {
                                       background-color: var(--background-color);
                                       color: var(--text-color);
                                       font-weight: 400;
                                       font-size: 0.875rem;
                                     }

                                     .font-light {
                                       font-weight: bold;
                                     }

                                     a,
                                     .link {
                                       color: var(--link-color);
                                     }

                                     .delimiter {
                                       border-right: 1px solid var(--border-color);
                                     }

                                     .suggestion--header {
                                       padding-top: 10px;
                                     }

                                     .suggestion .suggestion-text {
                                       font-size: 1.5rem;
                                       position: relative;
                                       top: -5%;
                                     }

                                     .summary .summary-item {
                                       margin-bottom: 0.8em;
                                     }

                                     .summary .label {
                                       font-size: 0.8rem;
                                     }

                                     .suggestion--header > h2,
                                     .summary > h2,
                                     .vulnerability-overview > h2 {
                                       font-size: 0.9rem;
                                       margin-bottom: 1.5em;
                                     }

                                     .identifiers {
                                       padding-bottom: 20px;
                                     }

                                     .vulnerability-overview pre {
                                       background-color: var(--container-background-color);
                                       border: 1px solid transparent;
                                     }
                                     """";

        public const string InitColorJsScript = """
                                          (function(){
                                          if (window.themeApplied) {
                                              return;
                                          }
                                          window.themeApplied = true;
                                          const style = getComputedStyle(document.documentElement);
                                              var properties = {
                                                  '--text-color': "#dfe1e5",
                                                  '--link-color': "#6b9bfa",
                                                  '--data-flow-body-color': "#2b2d30",
                                                  '--example-line-added-color': "#202d24",
                                                  '--example-line-removed-color': "#352628",
                                                  '--tab-item-github-icon-color': "#dfe1e5",
                                                  '--tab-item-hover-color': "#313438",
                                                  '--scrollbar-thumb-color': "#555555",
                                                  '--tabs-bottom-color': "#555555",
                                                  '--border-color': "#393b40",
                                                  '--editor-color': "#2b2d30",
                                                  '--label-color': "'#dfe1e5'",
                                                  '--container-background-color': "#313335",
                                                  '--generated-ai-fix-button-background-color': "#3376CD", // TODO: From Figma. Find the correct JetBrains API to get this color
                                              };
                                              for (let [property, value] of Object.entries(properties)) {
                                                  document.documentElement.style.setProperty(property, value);
                                              }
                                          
                                              // Add theme class to body
                                              const isDarkTheme = true;
                                              const isHighContrast = false;
                                              document.body.classList.add(isHighContrast ? 'high-contrast' : (isDarkTheme ? 'dark' : 'light'));
                                          })();
                                          """;
    }
    /// <summary>
    /// Interaction logic for DescriptionPanel.xaml.
    /// </summary>
    public partial class HtmlDescriptionPanel : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptionPanel"/> class. For OSS scan result.
        /// </summary>
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

            //HtmlViewer.Obj = new SnykScriptManager();
            //HtmlViewer.LoadCompleted += HtmlViewerOnLoadCompleted;
            //HtmlViewer.InvokeScript()
            //ThreadHelper.JoinableTaskFactory.Run(async () => await InitializeWebViewAsync());
        }

        private void HtmlViewerOnLoadCompleted(object sender, NavigationEventArgs e)
        {
            try
            {
                ///ThreadHelper.JoinableTaskFactory.Run(async () => await HtmlViewer.ExecuteScriptAsync(HtmlResourcesProvider.InitColorJsScript));
            }
            catch
            {

            }
        }
        private string GetNonce()
        {
            var allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
            var random = new Random();
            return new string(Enumerable.Repeat(allowedChars, 32)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
        }

        public void SetContent(Issue issue, string product)
        {
            var html = issue.AdditionalData.Details;
            
            var css = "<style nonce=\"${nonce}\">";
            string js;
            switch (product)
            {
                case Product.Oss:
                    break;
                case Product.Code:
                    css += HtmlResourcesProvider.CodeCss;
                    break;
                case Product.Iac:
                    break;
            }

            css += "</style>";
            html = html.Replace("${ideStyle}", css);

            html = html.Replace("var(--text-color)", "#dfe1e5");
            html = html.Replace("var(--background-color)", "#333");
            
            html = html.Replace("var(--link-color)", "#6b9bfa");
            html = html.Replace("var(--data-flow-body-color)", "#2b2d30");
            html = html.Replace("var(--example-line-added-color)", "#202d24");
            html = html.Replace("var(--example-line-removed-color)", "#352628");
            html = html.Replace("var(--tab-item-github-icon-color)", "#dfe1e5");
            html = html.Replace("var(--tab-item-hover-color)", "#313438");
            html = html.Replace("var(--scrollbar-thumb-color)", "#555555");
            html = html.Replace("var(--tabs-bottom-color)", "#555555");
            html = html.Replace("var(--border-color)", "#393b40");
            html = html.Replace("var(--editor-color)", "#2b2d30");
            html = html.Replace("var(--label-color)", "#dfe1e5");
            html = html.Replace("var(--container-background-color)", "#313335");
            html = html.Replace("var(--generated-ai-fix-button-background-color)", "#3376CD");

            var polyFills = """

                            """;
            var ideHeaders = """
                             <head>
                             <meta http-equiv='Content-Type' content='text/html; charset=unicode' />
                             <meta http-equiv='X-UA-Compatible' content='IE=edge' /> 
                             <script src="https://cdnjs.cloudflare.com/ajax/libs/html5shiv/3.7.3/html5shiv.min.js"></script>
                             <script src="https://cdnjs.cloudflare.com/ajax/libs/respond.js/1.4.2/respond.min.js"></script>
                             <script src="https://cdnjs.cloudflare.com/ajax/libs/modernizr/2.8.3/modernizr.min.js"></script>
                             """;
            html = html.Replace("<head>", ideHeaders);
            html = html.Replace("${nonce}", GetNonce());
            html = html.Replace("${ideScript}", "");
            HtmlViewer.NavigateToString(html);

        }
    }
}
