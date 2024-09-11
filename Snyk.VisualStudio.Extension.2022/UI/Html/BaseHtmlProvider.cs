using System.Linq;
using System;
using Microsoft.VisualStudio.PlatformUI;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    public class BaseHtmlProvider : IHtmlProvider
    {
        public virtual string GetCss()
        {
            return @"
                    html, body {
                       height: 100%;
                       font-size: 1.15rem;
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

                     section {
                       padding: 20px;
                     }
                     
                     .font-light {
                       font-weight: bold;
                     }

                     a,
                     .link {
                       color: var(--link-color);
                     }

                    .delimiter-top {
                           border-top: 1px solid var(--horizontal-border-color);
                    }
                    code {
                        background-color: var(--code-background-color);
                        padding: 1px 3px;
                        border-radius: 4px;
                    }
                ";
        }

        public virtual string GetJs()
        {
            return string.Empty;
        }

        public virtual string GetInitScript()
        {
            return @"
                    var links = document.querySelectorAll('a');
                    for(var i = 0; i < links.length; i++) {
                        links[i].onclick = function() {
                            window.external.OpenLink(this.href);
                            return false;
                        };
                    }
                ";
        }
        public string GetNonce()
        {
            var allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
            var random = new Random();
            return new string(Enumerable.Repeat(allowedChars, 32)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
        }

        public virtual string ReplaceCssVariables(string html)
        {
            var css = "<style nonce=\"${nonce}\">";
            css += GetCss();

            css += "</style>";
            html = html.Replace("${ideStyle}", css);
            html = html.Replace("<style nonce=\"ideNonce\" data-ide-style></style>", css);
            html = html.Replace("var(--default-font)", " ui-sans-serif, \"SF Pro Text\", \"Segoe UI\", \"Ubuntu\", Tahoma, Geneva, Verdana, sans-serif;");
            html = html.Replace("var(--text-color)", VSColorTheme.GetThemedColor(EnvironmentColors.BrandedUITextBrushKey).ToHex());
            html = html.Replace("var(--background-color)", VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxPopupBackgroundEndBrushKey).ToHex());
            html = html.Replace("var(--border-color)", VSColorTheme.GetThemedColor(EnvironmentColors.AccessKeyToolTipColorKey).ToHex()); 
            html = html.Replace("var(--link-color)", VSColorTheme.GetThemedColor(EnvironmentColors.PanelHyperlinkBrushKey).ToHex());
            html = html.Replace("var(--horizontal-border-color)", VSColorTheme.GetThemedColor(EnvironmentColors.ClassDesignerDefaultShapeTextBrushKey).ToHex());
            html = html.Replace("var(--code-background-color)", VSColorTheme.GetThemedColor(EnvironmentColors.ClassDesignerConnectionRouteBorderBrushKey).ToHex());
            
            var ideHeaders = """
                             <head>
                             <meta http-equiv='Content-Type' content='text/html; charset=unicode' />
                             <meta http-equiv='X-UA-Compatible' content='IE=edge' /> 
                             """;
            html = html.Replace("<head>", ideHeaders);
            html = html.Replace("${headerEnd}", "");
            var nonce = GetNonce();
            html = html.Replace("${nonce}", nonce);
            html = html.Replace("ideNonce", nonce);
            html = html.Replace("${ideScript}", "");

            return html;
        }
    }
}