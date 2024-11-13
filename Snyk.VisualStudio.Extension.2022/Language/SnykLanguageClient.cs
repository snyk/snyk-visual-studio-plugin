using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Serilog;
using Snyk.Common;
using Snyk.Common.Authentication;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.Analytics;
using Snyk.VisualStudio.Extension.CLI;
using StreamJsonRpc;
using Task = System.Threading.Tasks.Task;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using Process = System.Diagnostics.Process;
// ReSharper disable UnusedMember.Local

namespace Snyk.VisualStudio.Extension.Language
{
    [Export(typeof(ILanguageClient))]
    [Export(typeof(ILanguageClientManager))]
    [RunOnContext(RunningContext.RunOnHost)]
    public partial class SnykLanguageClient : ILanguageClient, ILanguageClientCustomMessage2, ILanguageClientManager
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykLanguageClient>();
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1,1);
        private SnykLsInitializationOptions initializationOptions;

        [ImportingConstructor]
        public SnykLanguageClient()
        {
            middleware = new SnykLanguageClientMiddleware();
        }

        public string Name => "Snyk Language Server";

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
            if (SnykVSPackage.ServiceProvider == null)
            {
                return null;
            }

            var options = SnykVSPackage.ServiceProvider.Options;
            initializationOptions = new SnykLsInitializationOptions
            {
                ActivateSnykCode = options.SnykCodeSecurityEnabled.ToString(),
                ActivateSnykCodeSecurity = options.SnykCodeSecurityEnabled.ToString(),
                ActivateSnykCodeQuality = options.SnykCodeQualityEnabled.ToString(),
                ActivateSnykOpenSource = options.OssEnabled.ToString(),
                ActivateSnykIac = options.IacEnabled.ToString(),
                SendErrorReports = "true",
                ManageBinariesAutomatically = options.BinariesAutoUpdate.ToString(),
                EnableTrustedFoldersFeature = "false",
                TrustedFolders = options.TrustedFolders.ToList(),
                IntegrationName = options.IntegrationName,
                FilterSeverity = new FilterSeverityOptions
                {
                    Critical = false,
                    High = false,
                    Low = false,
                    Medium = false,
                },
                ScanningMode = options.AutoScan ? "auto" : "manual",
#pragma warning disable VSTHRD104
                AdditionalParams = ThreadHelper.JoinableTaskFactory.Run(() => options.GetAdditionalOptionsAsync()),
#pragma warning restore VSTHRD104
                AuthenticationMethod = options.AuthenticationMethod == AuthenticationType.OAuth ? "oauth" : "token",
                CliPath = SnykCli.GetCliFilePath(options.CliCustomPath),
                Organization = options.Organization,
                Token = options.ApiToken.ToString(),
                AutomaticAuthentication = "false",
                Endpoint = options.CustomEndpoint,
                Insecure = options.IgnoreUnknownCA.ToString(),
                IntegrationVersion = options.IntegrationVersion,
                RequiredProtocolVersion = LsConstants.ProtocolVersion,
                HoverVerbosity = 1,
                OutputFormat = "plain",
                DeviceId = GetDeviceId(options)
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
        public event AsyncEventHandler<SnykLanguageServerEventArgs> OnLanguageServerReadyAsync;
        public event AsyncEventHandler<SnykLanguageServerEventArgs> OnLanguageClientNotInitializedAsync;

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            await Task.Yield();
            if (SnykVSPackage.ServiceProvider?.Options == null)
            {
                Logger.Error("Could not activate Language Server because ServiceProvider is null. Is the extension initialized?");
                return null;
            }
            var options = SnykVSPackage.ServiceProvider.Options;
            // ReSharper disable once RedundantAssignment
            var lsDebugLevel = await GetLsDebugLevelAsync(options);
#if DEBUG
            lsDebugLevel = "trace";
#endif
            var info = new ProcessStartInfo
            {
                FileName = SnykCli.GetCliFilePath(options.CliCustomPath),
                Arguments = "language-server -l "+ lsDebugLevel,
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
            var isPackageInitialized = SnykVSPackage.Instance?.IsInitialized ?? false;
            var shouldStart =  isPackageInitialized && !SnykVSPackage.ServiceProvider.TasksService.ShouldDownloadCli();
            Logger.Debug("OnLoadedAsync Called and shouldStart is: {ShouldStart}", shouldStart);

            await StartServerAsync(shouldStart);
        }

        public async Task StartServerAsync(bool shouldStart = false)
        {
            await semaphore.WaitAsync();
            try
            {
                if (StartAsync == null && shouldStart)
                {
                    FireOnLanguageClientNotInitializedAsync();
                    return;
                }

                if (StartAsync != null && SnykVSPackage.Instance?.Options != null && shouldStart)
                {
                    if (CustomMessageTarget == null)
                    {
                        CustomMessageTarget = new SnykLanguageClientCustomTarget(SnykVSPackage.ServiceProvider);
                    }

                    Logger.Information("Starting Language Server");
                    await StartAsync.InvokeAsync(this, EventArgs.Empty);
                    IsReady = true;
                    FireOnLanguageServerReadyAsyncEvent();
                    SendPluginInstalledEvent();
                }
                else
                {
                    Logger.Debug("Couldn't Start Language Server");
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void SendPluginInstalledEvent()
        {
            var settings = SnykVSPackage.Instance?.Options;
            if (settings == null) return;
            if (settings.AnalyticsPluginInstalledSent) return;
            
            var deviceId = GetDeviceId(settings);
            
            var analyticsSender = AnalyticsSender.Instance(settings, LanguageClientHelper.LanguageClientManager());
            var categories = new List<string> { "install" };
            var pluginInstalledEvent = new AnalyticsEvent("plugin installed", categories, deviceId);

            analyticsSender.LogEvent(pluginInstalledEvent, Callback);
            return;

            void Callback(object _)
            {
                settings.AnalyticsPluginInstalledSent = true;
            }
        }

        private string GetDeviceId(ISnykOptions settings)
        {
            if (string.IsNullOrEmpty(settings.DeviceId))
            {
                settings.DeviceId = Guid.NewGuid().ToString();
            }

            return settings.DeviceId;
        }

        public async Task StopServerAsync()
        {
            if (StopAsync != null)
            {
                try
                {
                    await StopAsync.InvokeAsync(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Logger.Error("Could not stop Language Server. {Ex}", ex);
                }
                finally
                {
                    IsReady = false;
                }
            }
            else
            {
                Logger.Debug("Could not stop Language Server because StopAsync is null");
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

            Logger.Error("{Ex}",message);

            return Task.FromResult(failureContext);
        }

        public Task OnServerInitializedAsync()
        {
            Rpc.Disconnected += Rpc_Disconnected;
            return Task.CompletedTask;
        }

        private async Task<string> GetLsDebugLevelAsync(ISnykOptions options)
        {
            var logLevel = "info";
            var additionalCliParameters = await options.GetAdditionalOptionsAsync();
            if (!string.IsNullOrEmpty(additionalCliParameters) && (additionalCliParameters.Contains("-d") || additionalCliParameters.Contains("--debug")))
            {
                logLevel = "debug";
            }

            return logLevel;
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
        }

        protected void OnStopping() { }
        protected void OnStopped() { }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
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
                await StopServerAsync();
                OnStopped();
                await StartServerAsync(true);
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
            var res = await InvokeWithParametersAsync<object>(LsConstants.WorkspaceExecuteCommand, param, cancellationToken);
            return res;
        }

        public async Task<object> InvokeFolderScanAsync(string folderPath, CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams
            {
                Command = LsConstants.SnykWorkspaceFolderScan,
                Arguments = new object[]{folderPath}
            };
            var res = await InvokeWithParametersAsync<object>(LsConstants.WorkspaceExecuteCommand, param, cancellationToken);
            return res;
        }


        public async Task<SastSettings> InvokeGetSastEnabled(CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams
            {
                Command = LsConstants.SnykSastEnabled
            };
            var sastSettings = await InvokeWithParametersAsync<SastSettings>(LsConstants.WorkspaceExecuteCommand, param, cancellationToken);
            return sastSettings;
        }

        public async Task<string> InvokeLogin(CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams
            {
                Command = LsConstants.SnykLogin
            };
            var token = await InvokeWithParametersAsync<string>(LsConstants.WorkspaceExecuteCommand, param, cancellationToken);
            return token;
        }

        public async Task<object> InvokeLogout(CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams
            {
                Command = LsConstants.SnykLogout
            };
            var isEnabled = await InvokeWithParametersAsync<object>(LsConstants.WorkspaceExecuteCommand, param, cancellationToken);
            return isEnabled;
        }

        public async Task<string> InvokeCopyLinkAsync(CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams
            {
                Command = LsConstants.SnykCopyAuthLink,
            };
            var copyLink = await InvokeWithParametersAsync<string>(LsConstants.WorkspaceExecuteCommand, param, cancellationToken);
            return copyLink;
        }

        public async Task<string> InvokeGenerateIssueDescriptionAsync(string issueId, CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams
            {
                Command = LsConstants.SnykGenerateIssueDescription,
                Arguments = new object[] { issueId }
            };
            var result = await InvokeWithParametersAsync<string>(LsConstants.WorkspaceExecuteCommand, param, cancellationToken);
            return result;
        }

        public async Task<string> InvokeGetFeatureFlagStatusAsync(string featureFlag, CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams
            {
                Command = LsConstants.SnykGetFeatureFlagStatus,
                Arguments = new object[] { featureFlag }
            };
            return await InvokeWithParametersAsync<string>(LsConstants.WorkspaceExecuteCommand, param, cancellationToken);
        }
        
        public async Task InvokeReportAnalyticsAsync(IAbstractAnalyticsEvent analyticsEvent, CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams
            {
                Command = LsConstants.SnykReportAnalytics,
                Arguments = new object[] { Json.Serialize(analyticsEvent) }
            };
            await InvokeWithParametersAsync<object>(LsConstants.WorkspaceExecuteCommand, param, cancellationToken);
        }

        public async Task<object> DidChangeConfigurationAsync(CancellationToken cancellationToken)
        {
            if (!IsReady) return default;

            var param = new LSP.DidChangeConfigurationParams
            {
                Settings = GetInitializationOptions()
            };
            
            return await InvokeWithParametersAsync<object>(LsConstants.WorkspaceChangeConfiguration, param, cancellationToken).ConfigureAwait(false);
        }

        public async Task RestartServerAsync()
        {
            await RestartAsync(true);
        }

        public void FireOnLanguageServerReadyAsyncEvent()
        {
            this.OnLanguageServerReadyAsync?.InvokeAsync(this, new SnykLanguageServerEventArgs{IsReady = true}).FireAndForget();
        }
        public void FireOnLanguageClientNotInitializedAsync()
        {
            this.OnLanguageClientNotInitializedAsync?.InvokeAsync(this, new SnykLanguageServerEventArgs { IsReady = false }).FireAndForget();
        }

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