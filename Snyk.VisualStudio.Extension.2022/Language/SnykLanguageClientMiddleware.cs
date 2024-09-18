using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using Serilog;
using Snyk.Common;

namespace Snyk.VisualStudio.Extension.Language
{
    public class SnykLanguageClientMiddleware : ILanguageClientMiddleLayer
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykLanguageClientMiddleware>();

        public SnykLanguageClientMiddleware() { }

        public bool CanHandle(string methodName)
        {
            return true;
        }

        public async Task HandleNotificationAsync(string methodName, JToken methodParam, Func<JToken, Task> sendNotification)
        {
            if (methodName == "window/logMessage")
                LogLspMessage(methodParam);

            await sendNotification(methodParam);
        }

        public async Task<JToken> HandleRequestAsync(string methodName, JToken methodParam, Func<JToken, Task<JToken>> sendRequest)
        {
            var result = await sendRequest(methodParam);
            return result;
        }

        private void LogLspMessage(JToken methodParams)
        {
            var logMsg = methodParams.TryParse<LSP.LogMessageParams>();
            if (logMsg == null)
                return;
            switch (logMsg.MessageType)
            {
                case LSP.MessageType.Error:
                    Logger.Error("LSP {MSG}", logMsg.Message);
                    break;
                case LSP.MessageType.Warning:
                    Logger.Warning("LSP {MSG}", logMsg.Message);
                    break;
                case LSP.MessageType.Info:
                    Logger.Information("LSP {MSG}", logMsg.Message);
                    break;
                case LSP.MessageType.Log:
                    Logger.Debug("LSP {MSG}", logMsg.Message);
                    break;
            }
        }
    }
}