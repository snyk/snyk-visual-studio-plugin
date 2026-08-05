using System;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    // Secrets shares Code's consistent-ignores workflow but has no AI-fix or data-flow UI.
    // The snyk-ls secrets description bundle (infrastructure/secrets/template + the shared
    // internal/html/ignore templates) emits only: ${ideStyle}, ${ideScript}, the ignore form's
    // ${ideSubmitIgnoreRequest}, and theme classes. It does NOT emit ${ideGenerateAIFix},
    // ${ideApplyAIFix}, or data-flow-clickable-row rows — so we wire neither here.
    public partial class SecretsHtmlProvider : BaseHtmlProvider
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
            var isDarkTheme = false;
            var isHighContrast = false;
            ResolveIdeThemeFlags(ref isDarkTheme, ref isHighContrast);

            var themeScript = $"var isDarkTheme = {isDarkTheme.ToString().ToLowerInvariant()};\n" +
                              $"var isHighContrast = {isHighContrast.ToString().ToLowerInvariant()};\n" +
                              "document.body.classList.add(isHighContrast ? 'high-contrast' : (isDarkTheme ? 'dark' : 'light'));";
            return themeScript;
        }

        /// <summary>
        /// Reports the IDE's dark / high-contrast state. Implemented in SecretsHtmlProvider.Vs.cs.
        /// </summary>
        static partial void ResolveIdeThemeFlags(ref bool isDarkTheme, ref bool isHighContrast);

        /// <summary>
        /// Substitutes the Secrets panel's themed colour variables. Implemented in
        /// SecretsHtmlProvider.Vs.cs; the variables are left in place where there is no IDE theme.
        /// </summary>
        static partial void ApplyIdeThemeColors(ref string html);

        public override string ReplaceCssVariables(string html)
        {
            var css = GetCss();
            html = html.Replace("${ideStyle}", css);

            html = base.ReplaceCssVariables(html);

            ApplyIdeThemeColors(ref html);

            html = html.Replace("${ideSubmitIgnoreRequest}", "window.SubmitIgnoreRequest(issueId, ignoreType, ignoreReason, ignoreExpirationDate)");
            return html;
        }
    }
}
