using System;
using Microsoft.VisualStudio.PlatformUI;
using Snyk.VisualStudio.Extension.Theme;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    public class CodeHtmlProvider : BaseHtmlProvider
    {
        private static CodeHtmlProvider _instance;

        public static CodeHtmlProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CodeHtmlProvider();
                }

                return _instance;
            }
        }

        public override string GetInitScript()
        {
            var themeScript = GetThemeScript();
            var initScript = base.GetInitScript();
            return initScript + Environment.NewLine + @"
                    function navigateToIssue(e, target) {
                        e.preventDefault();
                        var filePath = target.getAttribute('file-path');
                        var startLine = target.getAttribute('start-line');
                        var endLine = target.getAttribute('end-line');
                        var startCharacter = target.getAttribute('start-character');
                        var endCharacter = target.getAttribute('end-character');
                        window.external.OpenFileInEditor(filePath, startLine, endLine, startCharacter, endCharacter);
                    }
                    var navigatableLines = document.getElementsByClassName('data-flow-clickable-row');
                    for(var i = 0; i < navigatableLines.length; i++) {
                        navigatableLines[i].onclick = function(e) {
                            navigateToIssue(e, this);
                            return false;
                        };
                    }
                    if(document.getElementById('position-line')) {
                        document.getElementById('position-line').onclick = function(e) {
                            var target = navigatableLines[0];
                            if(target) { 
                                navigateToIssue(e, target);
                            }
                        }
                    }

                    // Below fixes a VS bug where when clicking a web view, the focus will not switch to the web view.
                    // Which, among other things, caused issues where pressing backspace would delete code in the editor and not the focused HTML form.
                    document.addEventListener('mousedown', function (e) {
                        window.external.FocusToolWindow();
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

            html = html.Replace("var(--example-line-removed-color)", VSColorTheme.GetThemedColor(EnvironmentColors.VizSurfaceRedDarkBrushKey).ToHex());
            html = html.Replace("var(--example-line-added-color)", VSColorTheme.GetThemedColor(EnvironmentColors.VizSurfaceGreenDarkBrushKey).ToHex());
            html = html.Replace("var(--button-background-color)", VSColorTheme.GetThemedColor(EnvironmentColors.StartPageButtonPinHoverColorKey).ToHex());
            html = html.Replace("var(--button-text-color)", VSColorTheme.GetThemedColor(EnvironmentColors.BrandedUITextBrushKey).ToHex());
            html = html.Replace("var(--circle-color)", VSColorTheme.GetThemedColor(EnvironmentColors.StartPageButtonPinHoverColorKey).ToHex());
            html = html.Replace("var(--warning-background)", VSColorTheme.GetThemedColor(EnvironmentColors.SmartTagHoverFillBrushKey).ToHex());
            html = html.Replace("var(--warning-text)", VSColorTheme.GetThemedColor(EnvironmentColors.SmartTagHoverTextBrushKey).ToHex());

            html = html.Replace("${ideGenerateAIFix}", "window.external.GenerateFixes(generateFixQueryString)");
            html = html.Replace("${ideApplyAIFix}", "window.external.ApplyFixDiff(fixId)");
            html = html.Replace("${ideSubmitIgnoreRequest}", "window.external.SubmitIgnoreRequest(issueId, ignoreType, ignoreReason, ignoreExpirationDate)");
            return html;
        }
    }
}