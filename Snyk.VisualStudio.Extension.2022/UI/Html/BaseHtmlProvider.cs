using System.Linq;
using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.PlatformUI;
using Snyk.VisualStudio.Extension.Theme;
using System.Windows;
using System.Windows.Media;

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

            // Primary buttons - use hyperlink color for a prominent blue appearance
            var primaryButtonBackground = linkColor;
            var primaryButtonForeground = "#FFFFFF";
            var primaryButtonHoverBackground = AdjustBrightness(primaryButtonBackground, 1.15f);

            // Secondary buttons - use input background for visible contrast with main background
            var secondaryButtonBackground = inputBackground;
            var secondaryButtonForeground = textColor;
            var secondaryButtonHoverBackground = AdjustBrightness(secondaryButtonBackground, 1.15f);

            // Legacy vscode- prefixed button variables (kept for compatibility)
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

            // Get IDE font size dynamically
            var fontSize = GetEditorFontSize();
            var dpiScale = GetDpiScale();

            // Build variable map for regex-based replacement (like IntelliJ plugin)
            var varMap = new System.Collections.Generic.Dictionary<string, string>
            {
                // VS Code style variables (from LS HTML)
                { "vscode-font-family", "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif" },
                { "vscode-editor-font-family", "'Consolas', 'Courier New', monospace" },
                { "vscode-font-size", $"{fontSize}px" },
                { "vscode-editor-background", editorBackground },
                { "vscode-foreground", textColor },
                { "vscode-input-foreground", textColor },
                { "vscode-editor-foreground", editorForeground },
                { "vscode-disabledForeground", disabledForeground },
                { "vscode-errorForeground", errorForeground },
                { "vscode-input-background", inputBackground },
                { "vscode-editor-inactiveSelectionBackground", inactiveSelectionBackground },
                { "vscode-list-hoverBackground", listHoverBackground },
                { "vscode-input-border", inputBorder },
                { "vscode-panel-border", borderColor },
                { "vscode-focusBorder", linkColor },
                { "vscode-scrollbarSlider-background", scrollbarThumb },
                { "vscode-scrollbarSlider-hoverBackground", scrollbarThumbHover },
                { "vscode-scrollbarSlider-activeBackground", scrollbarThumbHover },
                // Button variables (vscode- prefix for legacy compatibility)
                { "vscode-button-background", buttonBackground },
                { "vscode-button-foreground", buttonText },
                { "vscode-button-hoverBackground", buttonHoverBackground },
                { "vscode-button-secondaryBackground", ColorToRgba(buttonBackground, 0.6) },
                { "vscode-button-secondaryForeground", buttonText },
                { "vscode-button-secondaryHoverBackground", ColorToRgba(buttonHoverBackground, 0.7) },
                // Button variables (LS HTML uses these without vscode- prefix)
                { "button-background-color", primaryButtonBackground },
                { "button-foreground", primaryButtonForeground },
                { "button-hover-background", primaryButtonHoverBackground },
                { "button-secondary-background", secondaryButtonBackground },
                { "button-secondary-foreground", secondaryButtonForeground },
                { "button-secondary-hover-background", secondaryButtonHoverBackground },
                // Legacy variables (for fallback HTML)
                { "default-font", "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif" },
                { "text-color", textColor },
                { "background-color", backgroundColor },
                { "border-color", borderColor },
                { "link-color", linkColor },
                { "horizontal-border-color", borderColor },
                { "code-background-color", inputBackground },
                { "input-border", inputBorder },
                { "main-font-size", $"{fontSize * 15.0 / 13.0:F2}px" },
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

        /// <summary>
        /// Adjusts the brightness of a hex color by a given factor.
        /// </summary>
        /// <param name="hexColor">The hex color string (e.g., "#0e639c")</param>
        /// <param name="factor">Brightness multiplier (> 1.0 for lighter, < 1.0 for darker)</param>
        /// <returns>Adjusted hex color string</returns>
        private string AdjustBrightness(string hexColor, float factor)
        {
            if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith("#"))
            {
                return hexColor;
            }

            try
            {
                var hex = hexColor.Substring(1);
                int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);

                r = Math.Min(255, Math.Max(0, (int)(r * factor)));
                g = Math.Min(255, Math.Max(0, (int)(g * factor)));
                b = Math.Min(255, Math.Max(0, (int)(b * factor)));

                return $"#{r:X2}{g:X2}{b:X2}";
            }
            catch
            {
                return hexColor;
            }
        }

        /// <summary>
        /// Gets the current editor font size from Visual Studio environment settings.
        /// Uses WPF SystemFonts with DPI scaling applied for WebBrowser control.
        /// </summary>
        /// <returns>Font size in pixels suitable for CSS px units, scaled for DPI</returns>
        private double GetEditorFontSize()
        {
            // Base font size - this is what looks good at 100% DPI
            double baseFontSize = 13.0;

            // Get DPI scale factor
            // The WebBrowser control is not DPI-aware, so we need to manually scale
            var dpiScale = GetDpiScale();

            // Scale the font size by DPI
            // At 100% (scale 1.0): 13px
            // At 175% (scale 1.75): 13 * 1.75 = 22.75px
            // At 250% (scale 2.5): 13 * 2.5 = 32.5px

            // return baseFontSize * dpiScale;
            return baseFontSize; // TODO - Use DPI scale or delete it.
        }

        /// <summary>
        /// Gets the current DPI scale factor for the system.
        /// </summary>
        /// <returns>DPI scale factor (1.0 = 100%, 1.5 = 150%, 2.0 = 200%, etc.)</returns>
        private double GetDpiScale()
        {
            try
            {
                // Try to get DPI from the main window
                var mainWindow = System.Windows.Application.Current?.MainWindow;
                if (mainWindow != null)
                {
                    var source = PresentationSource.FromVisual(mainWindow);
                    if (source?.CompositionTarget != null)
                    {
                        return source.CompositionTarget.TransformToDevice.M11;
                    }
                }

                // Fallback: Get system DPI
                using (var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                {
                    return graphics.DpiX / 96.0;
                }
            }
            catch
            {
                // Default to 100% scaling if we can't determine DPI
                return 1.0;
            }
        }
    }
}
