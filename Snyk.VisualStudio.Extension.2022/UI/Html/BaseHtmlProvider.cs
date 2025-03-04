using System.Linq;
using System;
using Microsoft.VisualStudio.PlatformUI;
using Snyk.VisualStudio.Extension.Theme;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    public class BaseHtmlProvider : IHtmlProvider
    {
        public virtual string GetCss()
        {
            return "";
        }

        public virtual string GetJs()
        {
            return string.Empty;
        }

        public virtual string GetInitScript()
        {
            return @"
                    window.onerror = function(msg,url,line){return true;}
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
            var isDarkTheme = ThemeInfo.IsDarkTheme();
            var css = "<style nonce=\"${nonce}\">";
            css += GetCss();

            css += "</style>";
            var borderColor = VSColorTheme.GetThemedColor(EnvironmentColors.AccessKeyToolTipColorKey).ToHex();

            html = html.Replace("${ideStyle}", css);
            html = html.Replace("<style nonce=\"ideNonce\" data-ide-style></style>", css);
            html = html.Replace("var(--default-font)", " ui-sans-serif, \"SF Pro Text\", \"Segoe UI\", \"Ubuntu\", Tahoma, Geneva, Verdana, sans-serif;");
            html = html.Replace("var(--text-color)", VSColorTheme.GetThemedColor(EnvironmentColors.BrandedUITextBrushKey).ToHex());
            html = html.Replace("var(--background-color)", VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxPopupBackgroundEndBrushKey).ToHex());
            html = html.Replace("var(--border-color)", borderColor); 
            html = html.Replace("var(--link-color)", VSColorTheme.GetThemedColor(EnvironmentColors.PanelHyperlinkBrushKey).ToHex());
            html = html.Replace("var(--horizontal-border-color)", VSColorTheme.GetThemedColor(EnvironmentColors.ClassDesignerDefaultShapeTextBrushKey).ToHex());
            html = html.Replace("var(--code-background-color)", VSColorTheme.GetThemedColor(EnvironmentColors.EditorExpansionFillBrushKey).ToHex());
            html = html.Replace("var(--input-border)", borderColor);
            html = html.Replace("var(--main-font-size)", "15px");
            html = html.Replace("var(--ide-background-color)", isDarkTheme ? "#242424" : "#FBFBFB");
            html = html.Replace("${headerEnd}", "");
            var nonce = GetNonce();
            html = html.Replace("${nonce}", nonce);
            html = html.Replace("ideNonce", nonce);
            html = html.Replace("${ideScript}", "");

            return html;
        }
    }
}