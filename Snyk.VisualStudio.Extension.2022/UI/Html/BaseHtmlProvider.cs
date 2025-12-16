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
            return @"
            .sn-issue-title { margin-left: 10px; }
            ";
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
            var borderColor = VSColorTheme.GetThemedColor(EnvironmentColors.AccessKeyToolTipColorKey).ToHex();
            var linkColor = VSColorTheme.GetThemedColor(EnvironmentColors.PanelHyperlinkBrushKey).ToHex();
            var textColor = VSColorTheme.GetThemedColor(EnvironmentColors.BrandedUITextBrushKey).ToHex();
            var backgroundColor = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxPopupBackgroundEndBrushKey).ToHex();
            var inputBackground = VSColorTheme.GetThemedColor(EnvironmentColors.EditorExpansionFillBrushKey).ToHex();

            // CSS variable replacements (for Language Server HTML)
            html = html.Replace("var(--default-font)", " ui-sans-serif, \"SF Pro Text\", \"Segoe UI\", \"Ubuntu\", Tahoma, Geneva, Verdana, sans-serif;");
            html = html.Replace("var(--text-color)", textColor);
            html = html.Replace("var(--background-color)", backgroundColor);
            html = html.Replace("var(--border-color)", borderColor);
            html = html.Replace("var(--link-color)", linkColor);
            html = html.Replace("var(--horizontal-border-color)", VSColorTheme.GetThemedColor(EnvironmentColors.ClassDesignerDefaultShapeTextBrushKey).ToHex());
            html = html.Replace("var(--code-background-color)", inputBackground);
            html = html.Replace("var(--input-border)", borderColor);
            html = html.Replace("var(--main-font-size)", "15px");
            html = html.Replace("var(--ide-background-color)", isDarkTheme ? "#242424" : "#FBFBFB");
            html = html.Replace("var(--dimmed-text-color)", VSColorTheme.GetThemedColor(EnvironmentColors.ScrollBarThumbPressedBackgroundBrushKey).ToHex());

            // Template placeholder replacements (for embedded/fallback HTML)
            html = html.Replace("{{TEXT_COLOR}}", textColor);
            html = html.Replace("{{BACKGROUND_COLOR}}", backgroundColor);
            html = html.Replace("{{BORDER_COLOR}}", borderColor);
            html = html.Replace("{{INPUT_BACKGROUND}}", inputBackground);
            html = html.Replace("{{INPUT_FOREGROUND}}", textColor);
            html = html.Replace("{{INPUT_BORDER}}", borderColor);
            html = html.Replace("{{FOCUS_BORDER}}", linkColor);
            html = html.Replace("{{INFO_BG_COLOR}}", ColorToRgba(linkColor, 0.1));
            html = html.Replace("{{INFO_BORDER_COLOR}}", linkColor);
            html = html.Replace("{{ERROR_BG_COLOR}}", "rgba(244, 67, 54, 0.1)");
            html = html.Replace("{{ERROR_BORDER_COLOR}}", "#f44336");
            html = html.Replace("{{ERROR_TEXT_COLOR}}", "#f44336");

            html = html.Replace("${headerEnd}", "");
            var nonce = GetNonce();
            html = html.Replace("${nonce}", nonce);
            html = html.Replace("ideNonce", nonce);
            html = html.Replace("${ideScript}", "");

            return html;
        }

        private string ColorToRgba(string hexColor, double alpha)
        {
            hexColor = hexColor.TrimStart('#');
            int r = Convert.ToInt32(hexColor.Substring(0, 2), 16);
            int g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
            int b = Convert.ToInt32(hexColor.Substring(4, 2), 16);
            return $"rgba({r}, {g}, {b}, {alpha:F2})";
        }
    }
}