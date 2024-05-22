using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.ComponentModel.Composition;
using StreamJsonRpc;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
using Snyk.Common;
using Serilog;
using System.Security.Policy;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using System.Windows.Documents;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.Shared.CLI;

namespace Snyk.VisualStudio.Extension.Shared.Language
{
    [Export(typeof(ILanguageClient))]
    [Export(typeof(ILanguageClientManager))]
    [RunOnContext(RunningContext.RunOnHost)]
    public partial class SnykLanguageClient : ILanguageClient, ILanguageClientCustomMessage2, ILanguageClientManager
    {
        private static readonly ILogger _logger = LogManager.ForContext<SnykLanguageClient>();
        private object _lock = new object();
        private bool _isReady = false;

        [ImportingConstructor]
        public SnykLanguageClient([Import(typeof(SVsServiceProvider))]
        IServiceProvider serviceProvider
            )
        {
            _serviceProvider = serviceProvider;
        }

        public string Name => "Snyk LSP";

        public IEnumerable<string> ConfigurationSections
        {
            get
            {
                yield return "snyk";
            }
        }
        public bool IsReady
        {
            get { return _isReady; }
            set
            {
                _isReady = value;
            }
        }

        private SnykLSInitializationOptions _initializationOptions;
        private ISnykOptions _options;

        private readonly IServiceProvider _serviceProvider;

        public object InitializationOptions
        {
            get
            {
                return GetInitializationOptions();
            }
        }

        public object GetInitializationOptions()
        {
            if (_initializationOptions != null)
            {
                return _initializationOptions;
            }

            _initializationOptions = new SnykLSInitializationOptions
            {
                ActivateSnykCode = "true",
                ActivateSnykCodeQuality = "true",
                ActivateSnykCodeSecurity = "true",
                SendErrorReports = "true",
                ManageBinariesAutomatically = "true",
                EnableTrustedFoldersFeature = "false",
                ActivateSnykOpenSource = "true",
                IntegrationName = "Visual Studio",
                FilterSeverity = new FilterSeverityOptions
                {
                    Critical = false,
                    High = false,
                    Low = false,
                    Medium = false,
                },
                ScanningMode = "auto",
                AuthenticationMethod = "oauth",
                CliPath = _options.CliCustomPath,
                Token = _options.ApiToken.ToString(),
            };
            return _initializationOptions;
        }

        public IEnumerable<string> FilesToWatch => null;

        public bool ShowNotificationOnInitializeFailed => true;

        public JsonRpc Rpc { get; set; }
        public object MiddleLayer => SnykLanguageClientMiddleware.Instance;

        public object CustomMessageTarget { get; private set; }

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            await Task.Yield();
            if (_options == null) return null;
            var info = new ProcessStartInfo
            {
                FileName = string.IsNullOrEmpty(_options.CliCustomPath) ? SnykCli.GetSnykCliDefaultPath() : _options.CliCustomPath,
                Arguments = "language-server -l trace",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            //info.CreateNoWindow = true;
            var process = new Process
            {
                StartInfo = info
            };
            var isStarted = process.Start();
            return isStarted ? new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream) : null;
        }

        public void SetSnykOptions(ISnykOptions snykOptions)
        {
           _options = snykOptions;
        }

        public async Task OnLoadedAsync()
        {
            var customTarget = new SnykLanguageClientCustomTarget();
            CustomMessageTarget = customTarget;
            await StartServerAsync();
        }

        public async Task StartServerAsync()
        {
            if (StartAsync != null)
            {
                await StartAsync.InvokeAsync(this, EventArgs.Empty);
            }
        }

        public async Task StopServerAsync()
        {
            if (StopAsync != null)
            {
                await StopAsync.InvokeAsync(this, EventArgs.Empty);
                IsReady = IsReloading = false;
            }
        }

        public Task<InitializationFailureContext> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
        {
            var message = "Language Client failed to activate";
            var exception = initializationState.InitializationException?.ToString() ?? string.Empty;
            message = $"{message}\n {exception}";

            var failureContext = new InitializationFailureContext()
            {
                FailureMessage = message,
            };

            return Task.FromResult(failureContext);
        }

        public Task OnServerInitializedAsync()
        {
            Rpc.Disconnected += Rpc_Disconnected;
            return Task.CompletedTask;
        }

        private void Rpc_Disconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {

        }

        public async Task AttachForCustomMessageAsync(JsonRpc rpc)
        {
            await Task.Yield();
            Rpc = rpc;
            Rpc.AllowModificationWhileListening = true;
            Rpc.ActivityTracingStrategy = null;
            Rpc.AllowModificationWhileListening = false;
            IsReady = true;
        }

        protected void OnStopping() { }
        protected void OnStopped() { }
        public bool IsReloading { get; set; }

        private async Task RestartAsync(bool isReload)
        {
            try
            {
                if (isReload)
                {
                    IsReloading = true;
                }
                OnStopping();
                await StopAsync?.InvokeAsync(this, EventArgs.Empty);
                OnStopped();
                await StartAsync?.InvokeAsync(this, EventArgs.Empty);

            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }
            finally
            {
                if (isReload)
                {
                    IsReloading = false;
                }
            }
        }

        public async Task<object> InvokeWorkspaceScanAsync(CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams {
                Command = "snyk.workspace.scan"
            };
            var res = await InvokeWithParametersAsync<object>("workspace/executeCommand", param, cancellationToken);
            return res;
        }

        public async Task RestartServerAsync()
        {
            await RestartAsync(true);
        }

        // TODO: Add Logging

        private async Task<T> InvokeAsync<T>(string request, CancellationToken t) where T : class
        {
            if (!IsReady) return default;
            return await Rpc.InvokeAsync<T>(request, t).ConfigureAwait(false);
        }

        private async Task<T> InvokeWithParametersAsync<T>(string request, object parameters, CancellationToken t) where T : class
        {
            if (!IsReady) return default;
            return await Rpc.InvokeWithParameterObjectAsync<T>(request, parameters, t).ConfigureAwait(false);
        }

        private async Task NotifyWithParametersAsync(string request, object parameters)
        {
            if (!IsReady) return;
            await Rpc.NotifyWithParameterObjectAsync(request, parameters).ConfigureAwait(false);
        }
    }
}