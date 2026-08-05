using Microsoft.VisualStudio.PlatformUI;
using Snyk.VisualStudio.Extension.Theme;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// The Visual Studio bound half of <see cref="CodeHtmlProvider"/>: the themed colours and
    /// theme flags the Code description panel needs. Excluded from the cross-platform build (see
    /// docs/cross-platform-testing.md).
    /// </summary>
    public partial class CodeHtmlProvider
    {
        static partial void ResolveIdeThemeFlags(ref bool isDarkTheme, ref bool isHighContrast)
        {
            isDarkTheme = ThemeInfo.IsDarkTheme();
            isHighContrast = ThemeInfo.IsHighContrast();
        }

        static partial void ApplyIdeThemeColors(ref string html)
        {
            html = html.Replace("var(--example-line-removed-color)", VSColorTheme.GetThemedColor(EnvironmentColors.VizSurfaceRedDarkBrushKey).ToHex());
            html = html.Replace("var(--example-line-added-color)", VSColorTheme.GetThemedColor(EnvironmentColors.VizSurfaceGreenDarkBrushKey).ToHex());
            html = html.Replace("var(--button-background-color)", VSColorTheme.GetThemedColor(EnvironmentColors.StartPageButtonPinHoverColorKey).ToHex());
            html = html.Replace("var(--button-text-color)", VSColorTheme.GetThemedColor(EnvironmentColors.BrandedUITextBrushKey).ToHex());
            html = html.Replace("var(--circle-color)", VSColorTheme.GetThemedColor(EnvironmentColors.StartPageButtonPinHoverColorKey).ToHex());
            html = html.Replace("var(--warning-background)", VSColorTheme.GetThemedColor(EnvironmentColors.SmartTagHoverFillBrushKey).ToHex());
            html = html.Replace("var(--warning-text)", VSColorTheme.GetThemedColor(EnvironmentColors.SmartTagHoverTextBrushKey).ToHex());
        }
    }
}
