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

            var varMap = ExtractRootCssVariables(html);

            // IDE custom theme variables. These take priority over the extracted variables.
            var ideVarOverridesMap = new System.Collections.Generic.Dictionary<string, string>
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
                // Legacy variables (for fallback HTML theme colors)
                { "default-font", "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif" },
                { "text-color", textColor },
                { "background-color", backgroundColor },
                { "border-color", borderColor },
                { "link-color", linkColor },
                { "horizontal-border-color", borderColor },
                { "code-background-color", inputBackground },
                { "input-border", inputBorder },
                { "input-background-color", inputBackground },
                { "section-background-color", inactiveSelectionBackground },
                { "focus-color", linkColor },
                { "main-font-size", "15px" },
                { "ide-background-color", backgroundColor },
                { "dimmed-text-color", disabledForeground }
            };

            // Merge IDE variables into varMap (IDE values override :root values)
            foreach (var ideVarOverride in ideVarOverridesMap)
            {
                varMap[ideVarOverride.Key] = ideVarOverride.Value;
            }

            html = ReplaceCssVarUsages(html, varMap, maxIterations: 10);

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

            // Must not have blank template replacements, as they could be used to skip the "${nonce}" injection check
            // and still end up with the nonce injected, e.g. "${nonce${resolvesToEmpty}}" becomes "${nonce}" - See IDE-1050.
            html = html.Replace("${headerEnd}", " ");
            var nonce = GetNonce();
            html = html.Replace("${nonce}", nonce);
            html = html.Replace("ideNonce", nonce);
            html = html.Replace("${ideScript}", " ");

            return html;
        }

        /// <summary>
        /// Extracts CSS variable definitions from :root blocks in HTML.
        /// This is needed because IE11 doesn't support CSS custom properties natively.
        /// </summary>
        internal System.Collections.Generic.Dictionary<string, string> ExtractRootCssVariables(string html)
        {
            var variables = new System.Collections.Generic.Dictionary<string, string>();

            // Match :root { ... } blocks (handles multi-line)
            var rootPattern = new Regex(@"^\s*:root\s*\{(?<content>[^}]+)\}\s*$", RegexOptions.Multiline);
            // Strip block comments /* ... */ (Singleline makes . match newlines)
            var blockCommentPattern = new Regex(@"/\*.*?\*/", RegexOptions.Singleline);
            // Match variable definitions: --name: value;
            var varDefPattern = new Regex(@"^\s*--(?<name>[a-zA-Z0-9_-]+)\s*:\s*(?<value>[^;]+);", RegexOptions.Multiline);

            foreach (Match rootMatch in rootPattern.Matches(html))
            {
                var rootBlock = rootMatch.Groups["content"].Value;
                // Remove block comments before extracting variables
                rootBlock = blockCommentPattern.Replace(rootBlock, "");

                foreach (Match varMatch in varDefPattern.Matches(rootBlock))
                {
                    var name = varMatch.Groups["name"].Value;
                    var value = varMatch.Groups["value"].Value.Trim();
                    // Last definition wins (CSS cascade behavior)
                    variables[name] = value;
                }
            }

            return variables;
        }

        /// <summary>
        /// Replaces var(--name) and var(--name, fallback) usages with actual values.
        /// Loops until content stabilizes or max iterations reached (handles nested variables).
        /// </summary>
        internal string ReplaceCssVarUsages(string html, System.Collections.Generic.Dictionary<string, string> varMap, int maxIterations)
        {
            var cssVarPattern = new Regex(@"var\(--(?<name>[a-zA-Z0-9_-]+)(?:,\s*(?<fallback>[^)]+))?\)");

            for (int i = 0; i < maxIterations; i++)
            {
                var previousHtml = html;

                html = cssVarPattern.Replace(html, match =>
                {
                    var varName = match.Groups["name"].Value;
                    var fallbackGroup = match.Groups["fallback"];

                    // Priority: varMap value > CSS fallback > leave unchanged
                    if (varMap.ContainsKey(varName))
                    {
                        return varMap[varName];
                    }
                    else if (fallbackGroup.Success)
                    {
                        return fallbackGroup.Value.Trim();
                    }
                    else
                    {
                        return match.Value;
                    }
                });

                // Stop if content hasn't changed (no more replacements possible)
                if (html == previousHtml)
                {
                    break;
                }
            }

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
    }
}
