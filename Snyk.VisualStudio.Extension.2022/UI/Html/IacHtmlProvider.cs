using Microsoft.VisualStudio.PlatformUI;
using System;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    public class IacHtmlProvider : BaseHtmlProvider
    {
        private static IacHtmlProvider _instance;

        public static IacHtmlProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new IacHtmlProvider();
                }
                return _instance;
            }
        }

        public override string GetCss()
        {
            return base.GetCss() + Environment.NewLine + @"
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
                  overflow-x: auto;
                  border-radius: 4px;
                }
				.vulnerability-overview table {
				background-color: var(--container-background-color);
				}
                p {
                margin-top: 0;
                margin-bottom: 2rem;
                }
                ";
        }

        public override string ReplaceCssVariables(string html)
        {
            html = base.ReplaceCssVariables(html);
            html = html.Replace("var(--container-background-color)", VSColorTheme.GetThemedColor(EnvironmentColors.ClassDesignerConnectionRouteBorderBrushKey).ToHex());

            return html;
        }
    }
}