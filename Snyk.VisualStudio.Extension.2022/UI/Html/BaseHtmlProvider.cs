using System.Linq;
using System;
using System.Text.RegularExpressions;
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

            // Use proper tool window colors for consistent theming
            var backgroundColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey).ToHex();
            var textColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey).ToHex();
            var borderColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBorderColorKey).ToHex();

            // Links should use the standard hyperlink color
            var linkColor = VSColorTheme.GetThemedColor(EnvironmentColors.PanelHyperlinkBrushKey).ToHex();

            // Input fields - use ComboBox colors as they're designed for input controls
            var inputBackground = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxBackgroundColorKey).ToHex();
            var inputBorder = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxBorderColorKey).ToHex();

            // Editor/main content area
            var editorBackground = backgroundColor;
            var editorForeground = textColor;

            // Buttons - use command bar colors which are designed for interactive elements
            var buttonBackground = VSColorTheme.GetThemedColor(EnvironmentColors.CommandBarMenuBackgroundGradientBeginColorKey).ToHex();
            var buttonText = textColor;
            var buttonHoverBackground = VSColorTheme.GetThemedColor(EnvironmentColors.CommandBarMouseOverBackgroundBeginColorKey).ToHex();

            // Disabled and error states
            var disabledForeground = VSColorTheme.GetThemedColor(EnvironmentColors.SystemGrayTextColorKey).ToHex();
            var errorForeground = VSColorTheme.GetThemedColor(EnvironmentColors.VizSurfaceRedMediumBrushKey).ToHex();

            // Section backgrounds - use grid colors which are designed for content separation
            var inactiveSelectionBackground = VSColorTheme.GetThemedColor(EnvironmentColors.GridHeadingBackgroundColorKey).ToHex();

            // Hover and interaction states
            var listHoverBackground = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxMouseOverBackgroundBeginColorKey).ToHex();

            // Scrollbar colors
            var scrollbarBackground = VSColorTheme.GetThemedColor(EnvironmentColors.ScrollBarBackgroundColorKey).ToHex();
            var scrollbarThumb = VSColorTheme.GetThemedColor(EnvironmentColors.ScrollBarThumbBackgroundColorKey).ToHex();
            var scrollbarThumbHover = VSColorTheme.GetThemedColor(EnvironmentColors.ScrollBarThumbMouseOverBackgroundColorKey).ToHex();

            // Build variable map for regex-based replacement (like IntelliJ plugin)
            var varMap = new System.Collections.Generic.Dictionary<string, string>
            {
                // VS Code style variables (from LS HTML)
                { "vscode-font-family", "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif" },
                { "vscode-editor-font-family", "'Consolas', 'Courier New', monospace" },
                { "vscode-font-size", "13px" },
                { "vscode-editor-background", editorBackground },
                { "vscode-foreground", textColor },
                { "vscode-input-foreground", textColor },
                { "vscode-editor-foreground", editorForeground },
                { "vscode-disabledForeground", disabledForeground },
                { "vscode-errorForeground", errorForeground },
                { "vscode-input-background", inputBackground },
                { "vscode-editor-inactiveSelectionBackground", inactiveSelectionBackground },
                { "vscode-button-background", buttonBackground },
                { "vscode-button-foreground", buttonText },
                { "vscode-button-hoverBackground", buttonHoverBackground },
                { "vscode-button-secondaryBackground", ColorToRgba(buttonBackground, 0.6) },
                { "vscode-button-secondaryForeground", buttonText },
                { "vscode-button-secondaryHoverBackground", ColorToRgba(buttonHoverBackground, 0.7) },
                { "vscode-list-hoverBackground", listHoverBackground },
                { "vscode-input-border", inputBorder },
                { "vscode-panel-border", borderColor },
                { "vscode-focusBorder", linkColor },
                { "vscode-scrollbarSlider-background", scrollbarThumb },
                { "vscode-scrollbarSlider-hoverBackground", scrollbarThumbHover },
                { "vscode-scrollbarSlider-activeBackground", scrollbarThumbHover },
                // Legacy variables (for fallback HTML)
                { "default-font", "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif" },
                { "text-color", textColor },
                { "background-color", backgroundColor },
                { "border-color", borderColor },
                { "link-color", linkColor },
                { "horizontal-border-color", borderColor },
                { "code-background-color", inputBackground },
                { "input-border", inputBorder },
                { "main-font-size", "15px" },
                { "ide-background-color", backgroundColor },
                { "dimmed-text-color", disabledForeground }
            };

            // Regex pattern to match var(--varName) or var(--varName, fallback)
            // Matches var() usage in CSS
            var cssVarPattern = new Regex(@"var\(--([a-zA-Z0-9_-]+)(?:,\s*[^)]+)?\)");

            html = cssVarPattern.Replace(html, match =>
            {
                var varName = match.Groups[1].Value;
                return varMap.ContainsKey(varName) ? varMap[varName] : match.Value;
            });

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
