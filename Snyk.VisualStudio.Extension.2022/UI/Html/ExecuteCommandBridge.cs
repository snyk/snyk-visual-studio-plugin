// ABOUTME: Shared bridge for the window.__ideExecuteCommand__ JS<->IDE contract.
// ABOUTME: Reusable by any WebView2-hosted LS HTML page (settings, tree view, etc.).

using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using Snyk.VisualStudio.Extension;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Shared bridge for the <c>window.__ideExecuteCommand__</c> JS↔IDE contract.
    /// Reusable by any WebView2-hosted LS HTML page (settings window, tree view, etc.).
    ///
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Provide the ES5-compatible JS that defines <c>window.__ideExecuteCommand__</c>.</item>
    ///   <item>Dispatch incoming commands to the Language Server.</item>
    ///   <item>Return results to the JS callback via <c>window.__ideCallbacks__</c>.</item>
    /// </list>
    /// </summary>
    public static class ExecuteCommandBridge
    {
        private static readonly ILogger Logger = LogManager.ForContext(typeof(ExecuteCommandBridge));

        // Allowlist regex for callbackIds produced by BuildClientScript: "" or "__cb_<digits>"
        private static readonly Regex CallbackIdPattern = new Regex(@"^(__cb_\d+)?$", RegexOptions.Compiled);

        /// <summary>
        /// Escapes a string for safe embedding inside a single-quoted JavaScript string literal.
        /// Handles backslashes first, then single quotes, to avoid double-escaping.
        /// </summary>
        public static string EscapeForJsString(string value) =>
            (value ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'");

        /// <summary>
        /// Returns true if <paramref name="callbackId"/> matches the expected format produced by
        /// <see cref="BuildClientScript"/>. Used as an XSS guard before injecting the id back into JS.
        /// </summary>
        public static bool IsValidCallbackId(string callbackId) =>
            CallbackIdPattern.IsMatch(callbackId ?? string.Empty);

        /// <summary>
        /// Returns true if <paramref name="command"/> is in the <c>snyk.*</c> namespace and may be
        /// dispatched from a webview.
        /// </summary>
        public static bool IsAllowedCommand(string command) =>
            !string.IsNullOrEmpty(command) && command.StartsWith("snyk.");

        /// <summary>
        /// Returns the ES5-compatible JavaScript that redefines <c>window.__ideExecuteCommand__</c>
        /// to add callback-id roundtrip support on top of the raw bridge binding established
        /// by <see cref="WebView2BridgeBindings"/>. The wrapper posts directly via
        /// <c>chrome.webview.postMessage</c>, bypassing the raw binding so the bound callback
        /// metadata travels with the call.
        /// </summary>
        public static string BuildClientScript()
        {
            return @"
                window.__ideCallbacks__ = {};
                var __cbCounter_ide = 0;
                window.__ideExecuteCommand__ = function(command, args, callback) {
                    var callbackId = '';
                    if (typeof callback === 'function') {
                        callbackId = '__cb_' + (++__cbCounter_ide);
                        window.__ideCallbacks__[callbackId] = callback;
                    }
                    var argsJson = JSON.stringify(args || []);
                    chrome.webview.postMessage({ method: '__ideExecuteCommand__', args: [command, argsJson, callbackId] });
                };
            ";
        }

        /// <summary>
        /// Builds the JS that invokes <c>window.setAuthToken(token, apiUrl)</c> on the page,
        /// guarded by a <c>typeof</c> check so a missing function logs rather than throws.
        /// Used by the settings panel when the Language Server pushes an updated auth token
        /// (e.g. after an OAuth round-trip).
        /// </summary>
        public static string BuildSetAuthTokenScript(string token, string apiUrl)
        {
            var escapedToken = EscapeForJsString(token);
            var escapedApiUrl = EscapeForJsString(apiUrl);
            return $@"
                (function() {{
                    if (typeof window.setAuthToken === 'function') {{
                        window.setAuthToken('{escapedToken}', '{escapedApiUrl}');
                    }} else {{
                        console.warn('window.setAuthToken is not available');
                    }}
                }})();
            ";
        }

        /// <summary>
        /// Builds the JS that pops a pending callback from <c>window.__ideCallbacks__</c> and
        /// invokes it with the LS command's result. The <paramref name="callbackId"/> must have
        /// already been validated with <see cref="IsValidCallbackId"/>; we still escape it as a
        /// belt-and-braces guard against accidental injection.
        /// </summary>
        public static string BuildCommandCallbackScript(string callbackId, string resultJson)
        {
            var escapedCallbackId = EscapeForJsString(callbackId);
            return $"if(window.__ideCallbacks__&&window.__ideCallbacks__['{escapedCallbackId}']){{" +
                   $"var cb=window.__ideCallbacks__['{escapedCallbackId}'];" +
                   $"delete window.__ideCallbacks__['{escapedCallbackId}'];" +
                   $"cb({resultJson});}}";
        }

        /// <summary>
        /// Dispatches a command to the Language Server and invokes <paramref name="onCommandResult"/>
        /// with the serialized result when a <paramref name="callbackId"/> is provided.
        /// </summary>
        public static async Task DispatchAsync(
            ILanguageClientManager languageClientManager,
            string command,
            string argsJson,
            string callbackId,
            Action<string, string> onCommandResult,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!IsAllowedCommand(command))
                {
                    Logger.Warning("Webview attempted to execute disallowed command: {Command}", command);
                    return;
                }

                var args = string.IsNullOrEmpty(argsJson)
                    ? Array.Empty<object>()
                    : JsonConvert.DeserializeObject<object[]>(argsJson);

                var result = await languageClientManager.InvokeExecuteCommandAsync(
                    command, args, cancellationToken);

                if (!string.IsNullOrEmpty(callbackId))
                {
                    if (!IsValidCallbackId(callbackId))
                    {
                        Logger.Warning("Rejected callbackId with unexpected format: {CallbackId}", callbackId);
                        return;
                    }

                    var resultJson = result != null ? JsonConvert.SerializeObject(result) : "null";
                    onCommandResult?.Invoke(callbackId, resultJson);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error executing command {Command} from HTML bridge", command);
            }
        }
    }
}
