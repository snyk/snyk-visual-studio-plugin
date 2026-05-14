using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Builds the JS shim that publishes the bridge surface directly on <c>window</c>
    /// (matching the convention the Language Server's HTML expects). Each call is
    /// forwarded to C# via <c>chrome.webview.postMessage({ method, args })</c>, where
    /// <see cref="WebView2MessageDispatcher"/> routes by <c>method</c>. Registered via
    /// <c>CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync</c> so it runs before
    /// any LS-authored page script.
    /// </summary>
    public static class WebView2BridgeBindings
    {
        private sealed class Method
        {
            public Method(string name, params string[] parameters)
            {
                Name = name;
                Parameters = parameters;
            }

            public string Name { get; }
            public IReadOnlyList<string> Parameters { get; }
        }

        private static readonly IReadOnlyList<Method> Definitions = new[]
        {
            new Method("__saveIdeConfig__", "json"),
            new Method("__onFormDirtyChange__", "isDirty"),
            new Method("__ideSaveAttemptFinished__", "status"),
            new Method("__ideExecuteCommand__", "command", "argsJson", "callbackId"),
            new Method("OpenLink", "href"),
            new Method("OpenFileInEditor", "filePath", "startLine", "endLine", "startCharacter", "endCharacter"),
            new Method("EnableDelta", "isEnabled"),
            new Method("GenerateFixes", "value"),
            new Method("ApplyFixDiff", "fixId"),
            new Method("SubmitIgnoreRequest", "issueId", "ignoreType", "ignoreReason", "ignoreExpirationDate"),
            new Method("FocusToolWindow"),
        };

        public static IReadOnlyList<string> KnownMethods { get; } =
            Definitions.Select(m => m.Name).ToArray();

        public static string BuildScript()
        {
            var sb = new StringBuilder();
            sb.Append("(function () {");
            sb.Append(Environment.NewLine);
            sb.Append("  var post = function (method, args) { chrome.webview.postMessage({ method: method, args: args || [] }); };");
            sb.Append(Environment.NewLine);

            foreach (var m in Definitions)
            {
                var paramList = string.Join(", ", m.Parameters);
                var argList = m.Parameters.Count == 0
                    ? "[]"
                    : "[" + string.Join(", ", m.Parameters) + "]";
                sb.Append($"  window.{m.Name} = function ({paramList}) {{ post('{m.Name}', {argList}); }};");
                sb.Append(Environment.NewLine);
            }

            sb.Append("})();");
            return sb.ToString();
        }
    }
}
