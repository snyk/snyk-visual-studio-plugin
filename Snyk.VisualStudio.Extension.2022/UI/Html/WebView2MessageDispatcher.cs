using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Parses JSON payloads from WebView2's <c>WebMessageReceived</c> event in the shape
    /// <c>{ "method": "...", "args": [...] }</c> and routes them to a registered handler.
    /// Errors (malformed payload, unknown method, handler exception) are logged and swallowed
    /// so that a bad JS message cannot crash the host process.
    /// </summary>
    public class WebView2MessageDispatcher
    {
        private static readonly ILogger Logger = LogManager.ForContext<WebView2MessageDispatcher>();

        private readonly Dictionary<string, Action<JArray>> _handlers =
            new Dictionary<string, Action<JArray>>(StringComparer.Ordinal);

        public WebView2MessageDispatcher Register(string method, Action<JArray> handler)
        {
            if (string.IsNullOrEmpty(method)) throw new ArgumentException("Method name is required", nameof(method));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handlers[method] = handler;
            return this;
        }

        public void Dispatch(string messageJson)
        {
            if (string.IsNullOrEmpty(messageJson))
            {
                Logger.Warning("Bridge dispatcher received empty payload");
                return;
            }

            JObject parsed;
            try
            {
                parsed = JObject.Parse(messageJson);
            }
            catch (JsonReaderException ex)
            {
                Logger.Warning(ex, "Bridge dispatcher could not parse payload");
                return;
            }

            var method = parsed["method"]?.Value<string>();
            if (string.IsNullOrEmpty(method))
            {
                Logger.Warning("Bridge payload had no method name");
                return;
            }

            if (!_handlers.TryGetValue(method, out var handler))
            {
                Logger.Warning("No bridge handler registered for method '{Method}'", method);
                return;
            }

            var args = parsed["args"] as JArray ?? new JArray();
            try
            {
                handler(args);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Bridge handler for '{Method}' threw", method);
            }
        }
    }
}
