using System;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    public partial class CodeHtmlProvider : BaseHtmlProvider
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
                        window.OpenFileInEditor(filePath, startLine, endLine, startCharacter, endCharacter);
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
        /// Reports the IDE's dark / high-contrast state. Implemented in CodeHtmlProvider.Vs.cs.
        /// </summary>
        static partial void ResolveIdeThemeFlags(ref bool isDarkTheme, ref bool isHighContrast);

        /// <summary>
        /// Substitutes the Code panel's themed colour variables. Implemented in
        /// CodeHtmlProvider.Vs.cs; the variables are left in place where there is no IDE theme.
        /// </summary>
        static partial void ApplyIdeThemeColors(ref string html);

        public override string ReplaceCssVariables(string html)
        {
            var css = GetCss();
            html = html.Replace("${ideStyle}", css);

            html = base.ReplaceCssVariables(html);

            ApplyIdeThemeColors(ref html);

            html = html.Replace("${ideGenerateAIFix}", "window.GenerateFixes(issueId)");
            html = html.Replace("${ideApplyAIFix}", "window.ApplyFixDiff(fixId)");
            html = html.Replace("${ideSubmitIgnoreRequest}", "window.SubmitIgnoreRequest(issueId, ignoreType, ignoreReason, ignoreExpirationDate)");
            return html;
        }
    }
}