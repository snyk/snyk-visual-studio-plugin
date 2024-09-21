using System;
using Microsoft.VisualStudio.PlatformUI;

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

        public override string GetCss()
        {
            return base.GetCss() + Environment.NewLine + @"
                     .suggestion--header {
                       padding-top: 10px;
                     }

                     .suggestion .suggestion-text {
                       font-size: 1.5rem;
                       position: relative;
                       top: -5%;
                     }
                     .code-issue-container {
                       margin-top: 20px;
                     }
                     .summary .summary-item {
                       margin-bottom: 0.8em;
                     }

                     .summary .label {
                       font-size: 0.8rem;
                     }

                     .suggestion--header > h2,
                     .summary > h2 {
                       font-size: 0.9rem;
                       margin-bottom: 1.5em;
                     }

                     .identifiers {
                       padding-bottom: 20px;
                     }
                     .data-flow-table {
                       background-color: var(--container-background-color);
                       border: 1px solid transparent;
                     }
                     .tabs-nav {
                       margin: 21px 0 -21px;
                     }
                     
                     .tab-item {
                       cursor: pointer;
                       display: inline-block;
                       padding: 5px 10px;
                       border-bottom: 1px solid transparent;
                       font-size: 0.8rem;
                       color: var(--text-color);
                       text-transform: uppercase;
                     }
                     
                     .tab-item:hover {
                     
                     }
                     
                     .tab-item.is-selected {
                       border-bottom: 3px solid var(--link-color);
                     }
                     
                     .tab-content {
                       display: none;
                     }
                     
                     .tab-content.is-selected {
                       display: block;
                     }
                     .removed {
                      background-color: var(--line-removed);
                      color: #fff;
                     }
                    .lesson-link {
                     margin-left: 3px;
                    }
                    .added {
                      background-color: var(--line-added);
                      color: #fff;
                    }
                    .arrow {
                        cursor: pointer;
                        width: 20px;
                        height: 20px;
                        padding: 4px;
                        border-radius: 4px;
                        text-align: center;
                        line-height: 1;
                    }
                    .example {
                        background-color: var(--container-background-color);
                    }
                ";
        }

        public override string GetInitScript()
        {
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
                    // disable Autofix and ignores
                    if(document.getElementById('ai-fix-wrapper') && document.getElementById('no-ai-fix-wrapper')){
                        document.getElementById('ai-fix-wrapper').className = 'hidden';
                        document.getElementById('no-ai-fix-wrapper').className = '';
                     }
                    if(document.getElementsByClassName('ignore-action-container') && document.getElementsByClassName('ignore-action-container')[0]){
                        document.getElementsByClassName('ignore-action-container')[0].className = 'hidden';
                     }
                ";
        }

        public override string ReplaceCssVariables(string html)
        {
            html = base.ReplaceCssVariables(html);

            html = html.Replace("var(--container-background-color)", VSColorTheme.GetThemedColor(EnvironmentColors.BrandedUIBackgroundBrushKey).ToHex());
            html = html.Replace("var(--line-removed)", VSColorTheme.GetThemedColor(EnvironmentColors.VizSurfaceRedDarkBrushKey).ToHex());
            html = html.Replace("var(--line-added)", VSColorTheme.GetThemedColor(EnvironmentColors.VizSurfaceGreenDarkBrushKey).ToHex());

            return html;
        }
    }
}