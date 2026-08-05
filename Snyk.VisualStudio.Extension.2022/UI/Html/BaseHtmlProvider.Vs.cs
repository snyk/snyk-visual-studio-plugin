using Microsoft.VisualStudio.PlatformUI;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// The Visual Studio bound half of <see cref="BaseHtmlProvider"/>: reading the active IDE
    /// colour theme. Excluded from the cross-platform build (see
    /// docs/cross-platform-testing.md).
    /// </summary>
    public partial class BaseHtmlProvider
    {
        static partial void ResolveIdeThemePalette(ref HtmlThemePalette palette)
        {
            palette = new HtmlThemePalette
            {
                // Use proper tool window colors for consistent theming
                BackgroundColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey).ToHex(),
                TextColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey).ToHex(),
                BorderColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBorderColorKey).ToHex(),

                // Links should use the standard hyperlink color
                LinkColor = VSColorTheme.GetThemedColor(EnvironmentColors.PanelHyperlinkBrushKey).ToHex(),

                // Input fields - use ComboBox colors as they're designed for input controls
                InputBackground = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxBackgroundColorKey).ToHex(),
                InputBorder = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxBorderColorKey).ToHex(),

                // Legacy vscode- prefixed button variables (kept for compatibility)
                ButtonBackground = VSColorTheme.GetThemedColor(EnvironmentColors.CommandBarMenuBackgroundGradientBeginColorKey).ToHex(),
                ButtonHoverBackground = VSColorTheme.GetThemedColor(EnvironmentColors.CommandBarMouseOverBackgroundBeginColorKey).ToHex(),

                // Disabled and error states
                DisabledForeground = VSColorTheme.GetThemedColor(EnvironmentColors.SystemGrayTextColorKey).ToHex(),
                ErrorForeground = VSColorTheme.GetThemedColor(EnvironmentColors.VizSurfaceRedMediumBrushKey).ToHex(),

                // Section backgrounds - use grid colors which are designed for content separation
                InactiveSelectionBackground = VSColorTheme.GetThemedColor(EnvironmentColors.GridHeadingBackgroundColorKey).ToHex(),

                // Hover and interaction states
                ListHoverBackground = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxMouseOverBackgroundBeginColorKey).ToHex(),

                // Scrollbar colors
                ScrollbarBackground = VSColorTheme.GetThemedColor(EnvironmentColors.ScrollBarBackgroundColorKey).ToHex(),
                ScrollbarThumb = VSColorTheme.GetThemedColor(EnvironmentColors.ScrollBarThumbBackgroundColorKey).ToHex(),
                ScrollbarThumbHover = VSColorTheme.GetThemedColor(EnvironmentColors.ScrollBarThumbMouseOverBackgroundColorKey).ToHex(),
            };
        }
    }
}
