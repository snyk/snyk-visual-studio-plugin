// ABOUTME: Shared bridge for the window.__ideExecuteCommand__ JS<->IDE contract.
// ABOUTME: Reusable by any WebBrowser panel (settings, tree view, etc.).

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
    /// Reusable by any WebBrowser panel (settings window, tree view, etc.).
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
        /// Returns true if <paramref name="callbackId"/> matches the expected format produced by
        /// <see cref="BuildClientScript"/>. Used as an XSS guard before injecting the id back into JS.
        /// </summary>
        public static bool IsValidCallbackId(string callbackId) =>
            CallbackIdPattern.IsMatch(callbackId ?? string.Empty);

        /// <summary>
        /// Returns the ES5-compatible JavaScript that defines <c>window.__ideExecuteCommand__</c>.
        /// Assumes <c>window.external.__ideExecuteCommand__(command, argsJson, callbackId)</c>
        /// is provided by the COM bridge (<see cref="HtmlSettingsScriptingBridge"/>).
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
                    window.external.__ideExecuteCommand__(command, argsJson, callbackId);
                };
            ";
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
