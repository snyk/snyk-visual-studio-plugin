using System;
using Microsoft.VisualStudio.PlatformUI;
using Snyk.VisualStudio.Extension.Theme;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    // Secrets shares Code's consistent-ignores workflow but has no AI-fix or data-flow UI.
    // The snyk-ls secrets description bundle (infrastructure/secrets/template + the shared
    // internal/html/ignore templates) emits only: ${ideStyle}, ${ideScript}, the ignore form's
    // ${ideSubmitIgnoreRequest}, and theme classes. It does NOT emit ${ideGenerateAIFix},
    // ${ideApplyAIFix}, or data-flow-clickable-row rows — so we wire neither here.
    public class SecretsHtmlProvider : BaseHtmlProvider
    {
        private static SecretsHtmlProvider _instance;

        public static SecretsHtmlProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SecretsHtmlProvider();
                }

                return _instance;
            }
        }

        public override string GetInitScript()
        {
            var themeScript = GetThemeScript();
            var initScript = base.GetInitScript();
            return initScript + Environment.NewLine + @"
                    // Below fixes a VS bug where when clicking a web view, the focus will not switch to the web view.
                    // Which, among other things, caused issues where pressing backspace would delete code in the editor and not the focused HTML form.
                    document.addEventListener('mousedown', function (e) {
                        window.FocusToolWindow();
                    });
                " + themeScript;
        }

        private string GetThemeScript()
        {
            var isDarkTheme = ThemeInfo.IsDarkTheme();
            var isHighContrast = ThemeInfo.IsHighContrast();
            var themeScript = $"var isDarkTheme = {isDarkTheme.ToString().ToLowerInvariant()};\n" +
                              $"var isHighContrast = {isHighContrast.ToString().ToLowerInvariant()};\n" +
                              "document.body.classList.add(isHighContrast ? 'high-contrast' : (isDarkTheme ? 'dark' : 'light'));";
            return themeScript;
        }

        public override string ReplaceCssVariables(string html)
        {
            var css = GetCss();
            html = html.Replace("${ideStyle}", css);

            html = base.ReplaceCssVariables(html);

            html = html.Replace("var(--button-background-color)", VSColorTheme.GetThemedColor(EnvironmentColors.StartPageButtonPinHoverColorKey).ToHex());
            html = html.Replace("var(--button-text-color)", VSColorTheme.GetThemedColor(EnvironmentColors.BrandedUITextBrushKey).ToHex());
            html = html.Replace("var(--circle-color)", VSColorTheme.GetThemedColor(EnvironmentColors.StartPageButtonPinHoverColorKey).ToHex());
            html = html.Replace("var(--warning-background)", VSColorTheme.GetThemedColor(EnvironmentColors.SmartTagHoverFillBrushKey).ToHex());
            html = html.Replace("var(--warning-text)", VSColorTheme.GetThemedColor(EnvironmentColors.SmartTagHoverTextBrushKey).ToHex());

            html = html.Replace("${ideSubmitIgnoreRequest}", "window.SubmitIgnoreRequest(issueId, ignoreType, ignoreReason, ignoreExpirationDate)");
            return html;
        }
    }
}
