// ABOUTME: Loads HTML resources and applies Visual Studio theme colors
// ABOUTME: Replaces template placeholders with actual settings values

using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    public static class HtmlResourceLoader
    {
        /// <summary>
        /// Loads fallback HTML with theme colors and settings values applied.
        /// </summary>
        public static string LoadFallbackHtml(ISnykOptions options)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Snyk.VisualStudio.Extension.Resources.settings-fallback.html";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new FileNotFoundException($"Embedded resource not found: {resourceName}");

                using (var reader = new StreamReader(stream))
                {
                    var template = reader.ReadToEnd();
                    return ApplyThemeAndSettings(template, options);
                }
            }
        }

        /// <summary>
        /// Applies LS HTML with theme colors.
        /// </summary>
        public static string ApplyTheme(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            return ReplaceThemeColors(html);
        }

        private static string ApplyThemeAndSettings(string template, ISnykOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            // Apply settings values (using actual ISnykOptions property names)
            template = template
                .Replace("{{MANAGE_BINARIES_CHECKED}}", options.BinariesAutoUpdate ? "checked" : "")
                .Replace("{{CLI_PATH}}", options.CliCustomPath ?? "")
                .Replace("{{CLI_BASE_DOWNLOAD_URL}}", options.CliDownloadUrl ?? "")
                .Replace("{{CHANNEL_STABLE_SELECTED}}", options.CliReleaseChannel == "stable" ? "selected" : "")
                .Replace("{{CHANNEL_RC_SELECTED}}", options.CliReleaseChannel == "rc" ? "selected" : "")
                .Replace("{{CHANNEL_PREVIEW_SELECTED}}", options.CliReleaseChannel == "preview" ? "selected" : "");

            // Apply theme colors
            return ReplaceThemeColors(template);
        }

        private static string ReplaceThemeColors(string html)
        {
            var theme = GetThemeColors();

            return html
                .Replace("{{TEXT_COLOR}}", theme.TextColor)
                .Replace("{{BACKGROUND_COLOR}}", theme.BackgroundColor)
                .Replace("{{BORDER_COLOR}}", theme.BorderColor)
                .Replace("{{INPUT_BACKGROUND}}", theme.InputBackground)
                .Replace("{{INPUT_FOREGROUND}}", theme.InputForeground)
                .Replace("{{INPUT_BORDER}}", theme.InputBorder)
                .Replace("{{FOCUS_BORDER}}", theme.FocusBorder)
                .Replace("{{INFO_BG_COLOR}}", theme.InfoBgColor)
                .Replace("{{INFO_BORDER_COLOR}}", theme.InfoBorderColor)
                .Replace("{{ERROR_BG_COLOR}}", theme.ErrorBgColor)
                .Replace("{{ERROR_BORDER_COLOR}}", theme.ErrorBorderColor)
                .Replace("{{ERROR_TEXT_COLOR}}", theme.ErrorTextColor);
        }

        private static ThemeColors GetThemeColors()
        {
            try
            {
                var bgColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
                var fgColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);
                var borderColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBorderColorKey);
                var inputBg = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxBackgroundColorKey);
                var inputFg = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxTextColorKey);
                var linkColor = VSColorTheme.GetThemedColor(EnvironmentColors.ControlLinkTextColorKey);

                return new ThemeColors
                {
                    TextColor = ColorToHex(fgColor),
                    BackgroundColor = ColorToHex(bgColor),
                    BorderColor = ColorToHex(borderColor),
                    InputBackground = ColorToHex(inputBg),
                    InputForeground = ColorToHex(inputFg),
                    InputBorder = ColorToHex(borderColor),
                    FocusBorder = ColorToHex(linkColor),
                    InfoBgColor = ColorWithAlpha(linkColor, 26), // ~10% opacity
                    InfoBorderColor = ColorToHex(linkColor),
                    ErrorBgColor = "rgba(244, 67, 54, 0.1)",
                    ErrorBorderColor = "#f44336",
                    ErrorTextColor = "#f44336"
                };
            }
            catch
            {
                // Fallback to default colors if theme service unavailable
                return GetDefaultColors();
            }
        }

        private static ThemeColors GetDefaultColors()
        {
            return new ThemeColors
            {
                TextColor = "#000000",
                BackgroundColor = "#ffffff",
                BorderColor = "#cccccc",
                InputBackground = "#ffffff",
                InputForeground = "#000000",
                InputBorder = "#cccccc",
                FocusBorder = "#007acc",
                InfoBgColor = "rgba(0, 122, 204, 0.1)",
                InfoBorderColor = "#007acc",
                ErrorBgColor = "rgba(244, 67, 54, 0.1)",
                ErrorBorderColor = "#f44336",
                ErrorTextColor = "#f44336"
            };
        }

        private static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static string ColorWithAlpha(Color color, int alpha)
        {
            return $"rgba({color.R}, {color.G}, {color.B}, {alpha / 255.0:F2})";
        }

        private class ThemeColors
        {
            public string TextColor { get; set; }
            public string BackgroundColor { get; set; }
            public string BorderColor { get; set; }
            public string InputBackground { get; set; }
            public string InputForeground { get; set; }
            public string InputBorder { get; set; }
            public string FocusBorder { get; set; }
            public string InfoBgColor { get; set; }
            public string InfoBorderColor { get; set; }
            public string ErrorBgColor { get; set; }
            public string ErrorBorderColor { get; set; }
            public string ErrorTextColor { get; set; }
        }
    }
}
