using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Serilog;
using Snyk.Common;
using Snyk.Common.Authentication;
using Snyk.VisualStudio.Extension.CLI;
using StreamJsonRpc;
using Task = System.Threading.Tasks.Task;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using Process = System.Diagnostics.Process;

namespace Snyk.VisualStudio.Extension.Language
{
    [Export(typeof(ILanguageClient))]
    [Export(typeof(ILanguageClientManager))]
    [RunOnContext(RunningContext.RunOnHost)]
    public partial class SnykLanguageClient : ILanguageClient, ILanguageClientCustomMessage2, ILanguageClientManager
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykLanguageClient>();
        private object _lock = new object();

        private SnykLsInitializationOptions initializationOptions;

        [ImportingConstructor]
        public SnykLanguageClient()
        {
            middleware = new SnykLanguageClientMiddleware();
        }

        public string Name => "SnykLS";

        public IEnumerable<string> ConfigurationSections
        {
            get
            {
                yield return "snyk";
            }
        }
        public bool IsReady { get; set; }
        public object InitializationOptions => GetInitializationOptions();

        public object GetInitializationOptions()
        {
            if (SnykVSPackage.ServiceProvider == null || initializationOptions != null)
            {
                return initializationOptions;
            }

            var options = SnykVSPackage.ServiceProvider.Options;
            initializationOptions = new SnykLsInitializationOptions
            {
                ActivateSnykCode = options.SnykCodeSecurityEnabled.ToString(),
                ActivateSnykCodeQuality = options.SnykCodeQualityEnabled.ToString(),
                ActivateSnykCodeSecurity = options.SnykCodeQualityEnabled.ToString(),
                ActivateSnykOpenSource = options.OssEnabled.ToString(),
                SendErrorReports = options.UsageAnalyticsEnabled.ToString(),
                ManageBinariesAutomatically = options.BinariesAutoUpdate.ToString(),
                EnableTrustedFoldersFeature = "false",
                IntegrationName = options.IntegrationName,
                FilterSeverity = new FilterSeverityOptions
                {
                    Critical = false,
                    High = false,
                    Low = false,
                    Medium = false,
                },
                ScanningMode = "auto",
#pragma warning disable VSTHRD104
                AdditionalParams = ThreadHelper.JoinableTaskFactory.Run(() => options.GetAdditionalOptionsAsync()),
#pragma warning restore VSTHRD104
                AuthenticationMethod = options.AuthenticationMethod == AuthenticationType.OAuth ? "oauth" : "token",
                CliPath = options.CliCustomPath,
                Organization = options.Organization,
                Token = options.ApiToken.ToString(),
                AutomaticAuthentication = "false",
                Endpoint = options.CustomEndpoint,
                Insecure = options.IgnoreUnknownCA.ToString(),
                IntegrationVersion = options.IntegrationVersion,
                RequiredProtocolVersion = LsConstants.ProtocolVersion.ToString()
            };
            return initializationOptions;
        }

        public IEnumerable<string> FilesToWatch => null;

        public bool ShowNotificationOnInitializeFailed => true;

        public JsonRpc Rpc { get; set; }
        private readonly SnykLanguageClientMiddleware middleware;

        public object MiddleLayer => middleware;

        public object CustomMessageTarget { get; private set; }

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            await Task.Yield();
            if (SnykVSPackage.ServiceProvider?.Options == null) return null;
            var options = SnykVSPackage.ServiceProvider.Options;
            var info = new ProcessStartInfo
            {
                FileName = string.IsNullOrEmpty(options.CliCustomPath) ? SnykCli.GetSnykCliDefaultPath() : options.CliCustomPath,
                Arguments = "language-server -l trace",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
#if DEBUG
            info.CreateNoWindow = false;
#endif
            var process = new Process
            {
                StartInfo = info
            };
            var isStarted = process.Start();
            return isStarted ? new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream) : null;
        }

        public async Task OnLoadedAsync()
        {
            //await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            //var myPackage = Package.GetGlobalService(typeof(SnykVSPackage)) as SnykVSPackage;
            //if (myPackage == null)
            //{
            //    // Force package loading
            //    var shell = (IVsShell)GetServiceAsync(typeof(SVsShell));
            //    IVsPackage package;
            //    Guid packageGuid = typeof(SnykVSPackage).GUID;
            //    shell.LoadPackage(ref packageGuid, out package);
            //}

            await StartServerAsync();
        }

        public async Task StartServerAsync(bool manualTrigger = false)
        {
            if (!IsReady && StartAsync != null && SnykVSPackage.Instance?.Options != null)
            {
                if (CustomMessageTarget == null)
                {
                    CustomMessageTarget = new SnykLanguageClientCustomTarget(SnykVSPackage.ServiceProvider);
                }
                await StartAsync.InvokeAsync(this, EventArgs.Empty);
            }
        }

        public async Task StopServerAsync()
        {
            if (StopAsync != null)
            {
                await StopAsync.InvokeAsync(this, EventArgs.Empty);
                IsReady = false;
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
            if (StopAsync == null || StartAsync == null)
                return;
            try
            {
                if (isReload)
                {
                    IsReloading = true;
                }
                OnStopping();
                await StopAsync.InvokeAsync(this, EventArgs.Empty);
                OnStopped();
                await StartAsync.InvokeAsync(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in restarting Language client");
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
                Command = LsConstants.SnykWorkspaceScan
            };
            var res = await InvokeWithParametersAsync<object>("workspace/executeCommand", param, cancellationToken);
            return res;
        }

        public async Task<object> InvokeGetSastEnabled(CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams
            {
                Command = LsConstants.SnykSastEnabled
            };
            var isEnabled = await InvokeWithParametersAsync<object>("workspace/executeCommand", param, cancellationToken);
            return isEnabled;
        }

        public async Task RestartServerAsync()
        {
            await RestartAsync(true);
        }

        // TODO: Add Logging

        private async Task<T> InvokeAsync<T>(string request, CancellationToken t)
        {
            if (!IsReady) return default;
            return await Rpc.InvokeAsync<T>(request, t).ConfigureAwait(false);
        }

        private async Task<T> InvokeWithParametersAsync<T>(string request, object parameters, CancellationToken t)
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