using Microsoft.VisualStudio.PlatformUI;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// The Visual Studio bound half of <see cref="OssHtmlProvider"/>. Excluded from the
    /// cross-platform build (see docs/cross-platform-testing.md).
    /// </summary>
    public partial class OssHtmlProvider
    {
        static partial void ApplyIdeThemeColors(ref string html) =>
            html = html.Replace("var(--container-background-color)", VSColorTheme.GetThemedColor(EnvironmentColors.EditorExpansionFillBrushKey).ToHex());
    }
}
