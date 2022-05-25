namespace Snyk.VisualStudio.Extension.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.LanguageServer.Client;
    using Microsoft.VisualStudio.LanguageServer.Protocol;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;
    using Newtonsoft.Json.Linq;
    using Serilog;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using StreamJsonRpc;

    [ContentType("CSharp")]
    [Export(typeof(ILanguageClient))]
    public class SnykLanguageClient : ILanguageClient, ILanguageClientCustomMessage2
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykLanguageClient>();
        private JsonRpc jsonRpc;

        public event AsyncEventHandler<EventArgs> StartAsync;

        public event AsyncEventHandler<EventArgs> StopAsync;

        public async Task OnLoadedAsync()
        {
            Logger.Information("Plugin loaded - starting language server...");
            await this.StartAsync.InvokeAsync(this, EventArgs.Empty);
            Logger.Information("Language server startup completed");
        }

        public Task OnServerInitializedAsync()
        {
            Logger.Information("Language Server initialized");
            return Task.CompletedTask;
        }

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            Logger.Information("Activating LSP client...");
            await Task.Yield();
            token.ThrowIfCancellationRequested();

            var snykLsPath = Path.Combine(SnykExtension.GetExtensionDirectoryPath(), "snyk-ls.exe");
            if (!File.Exists(snykLsPath))
            {
                Logger.Error("LSP server executable could not be found in \"{FilePath}\"", snykLsPath);
                return null;
            }

            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = snykLsPath,
                    Arguments = "-l trace",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    UseShellExecute = false,
                    //CreateNoWindow = true,
                    CreateNoWindow = false,
                },
            };

            var cliPath = "C:\\Users\\Asaf Agami\\AppData\\Local\\Snyk\\snyk-win.exe";
            Logger.Debug("Awaiting package initialization completion");
            await SnykVSPackage.InitializationCompletedAwaiter;
            Logger.Debug("Package initialization completed, resuming LS startup");
            var apiToken = SnykVSPackage.ServiceProvider?.Options?.ApiToken;
            process.StartInfo.EnvironmentVariables["ACTIVATE_SNYK_OPEN_SOURCE"] = "false";
            process.StartInfo.EnvironmentVariables["ACTIVATE_SNYK_CODE"] = "true";
            process.StartInfo.EnvironmentVariables["ACTIVATE_SNYK_IAC"] = "false";
            process.StartInfo.EnvironmentVariables["SNYK_CLI_PATH"] = cliPath;
            if (apiToken != null)
            {
                process.StartInfo.EnvironmentVariables["SNYK_TOKEN"] = apiToken;
            }
            process.StartInfo.EnvironmentVariables["INSECURE_KEY"] = "false";
            Logger.Information("Starting LSP server...");
            Logger.Information("CLI Path: {Cli}", cliPath);
            Logger.Information("API Token: {Cli}", apiToken);

            if (process.Start())
            {
                this.StopAsync += OnStopAsync(process);
                Logger.Information("LSP server started");
                var connection = new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
                Logger.Information("LSP client activated");
                return connection;
            }

            Logger.Error("LSP server failed to start");
            return null;
        }

        private static AsyncEventHandler<EventArgs> OnStopAsync(Process process)
        {
            return (sender, args) =>
            {
                Logger.Information("Closing LS process");
                try
                {
                    process.Close();
                }
                catch (Exception exception)
                {
                    Logger.Error("Error when trying to close process: {Exception}", exception);
                }
                return Task.CompletedTask;
            };
        }

        public Task<InitializationFailureContext> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
        {
            var initializationStatus = initializationState.Status;
            var errorMessage = $"Language server failed to initialize during the {initializationStatus} Stage";
            Logger.Error("Language server failed to initialize during the {InitializationStage} Stage", initializationStatus);
            return Task.FromResult(new InitializationFailureContext()
            {
                FailureMessage = errorMessage,
            });
        }

        public string Name => "Snyk Language Extension";

        public IEnumerable<string> ConfigurationSections => null;

        public object InitializationOptions => null;

        public IEnumerable<string> FilesToWatch => null;

        public bool ShowNotificationOnInitializeFailed => true;

        public Task AttachForCustomMessageAsync(JsonRpc rpc)
        {
            this.jsonRpc = rpc;
            rpc.AllowModificationWhileListening = true;
            rpc.ActivityTracingStrategy = null;
            rpc.AllowModificationWhileListening = false;
            return Task.CompletedTask;
        }

        public object MiddleLayer { get; } = new SnykLsMiddleLayer();

        public object CustomMessageTarget { get; } = new CustomTarget();

        internal class CustomTarget
        {
            public void OnCustomNotification(JToken arg)
            {
                Logger.Information("OnCustomNotification: {Arg}", arg);
            }

            public string OnCustomRequest(string arg)
            {
                Logger.Information("OnCustomRequest: {Arg}", arg);
                return arg;
            }
        }

        internal class SnykLsMiddleLayer : ILanguageClientMiddleLayer
        {
            public bool CanHandle(string methodName)
            {
                var canHandle = methodName == Methods.InitializeName;
                Logger.Information("Middle layer can handle {MethodName}? : {CanHandle}", methodName, canHandle);
                return canHandle;
            }

            public async Task<JToken> HandleRequestAsync(string methodName, JToken methodParam, Func<JToken, Task<JToken>> sendRequest)
            {
                Logger.Information("Handle request - methodParam: {MethodParam}", methodParam);
                return await sendRequest(methodParam);
            }

            public Task HandleNotificationAsync(string methodName, JToken methodParam, Func<JToken, Task> sendNotification)
            {
                Logger.Information("Handle notification - methodParam: {MethodParam}", methodParam);
                return Task.CompletedTask;
            }
        }
    }
}