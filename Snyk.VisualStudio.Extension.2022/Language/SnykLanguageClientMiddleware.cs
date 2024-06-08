using Microsoft.VisualStudio.LanguageServer.Client;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json.Linq;

namespace Snyk.VisualStudio.Extension.Shared.Language
{
    public class SnykLanguageClientMiddleware : ILanguageClientMiddleLayer
    {
        internal readonly static SnykLanguageClientMiddleware Instance =
            new SnykLanguageClientMiddleware();
        private SnykLanguageClientMiddleware() { }

        public bool CanHandle(string methodName)
        {
            return true;
        }

        public async Task HandleNotificationAsync(string methodName, JToken methodParam, Func<JToken, Task> sendNotification)
        {
            // Testing Custom handlers for well-known already registered methods
            // By default window/logMessage will be shown in Output Window
            //if (methodName == "window/logMessage")
            //    await _logger.WriteLine(methodParam);

            await sendNotification(methodParam);
        }

        public async Task<JToken> HandleRequestAsync(string methodName, JToken methodParam, Func<JToken, Task<JToken>> sendRequest)
        {
            var result = await sendRequest(methodParam);
            return result;
        }
    }
}