using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Serilog;
using Snyk.VisualStudio.Extension.Analytics;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.Utils;
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
        private LsSettingsV25 settingsV25;
        // Holds the delegate subscribed to SolutionEvents.AfterBackgroundSolutionLoadComplete so we
        // can unsubscribe before re-subscribing on server restarts (idempotent wiring).
        private EventHandler<EventArgs> solutionOpenedMigrationHandler;

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
            settingsV25 ??= new LsSettingsV25(SnykVSPackage.ServiceProvider);
            return settingsV25.GetInitializationOptions();
        }

        public IEnumerable<string> FilesToWatch => null;

        public bool ShowNotificationOnInitializeFailed => true;

        public IJsonRpc Rpc { get; set; }
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
            var serviceProvider = SnykVSPackage.ServiceProvider;
            if (serviceProvider?.Options == null)
            {
                Logger.Error("Could not activate Language Server because ServiceProvider is null. Is the extension initialized?");
                return null;
            }
            var options = serviceProvider.Options;
            // ReSharper disable once RedundantAssignment
            var lsDebugLevel = await GetLsDebugLevelAsync();
            var info = new ProcessStartInfo
            {
                FileName = SnykCli.GetCliFilePath(options.CliCustomPath),
                Arguments = "language-server -l "+ lsDebugLevel,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
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
            Logger.Information("OnLoadedAsync Called and shouldStart is: {ShouldStart}", shouldStart);

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

                    await MigrateLegacySolutionSettingsAsync();

                    Logger.Information("Starting Language Server");
                    await StartAsync.InvokeAsync(this, EventArgs.Empty);
                }
                else
                {
                    Logger.Information("Couldn't Start Language Server");
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

            var deviceId = settings.DeviceId;
            
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

        public async Task StopServerAsync()
        {
            // Detach the solution-opened migration handler as part of stop teardown so a permanent
            // stop (extension disable / VS shutdown) doesn't leave it running against a dead LS.
            UnsubscribeFromSolutionOpenedForMigration();

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
                Logger.Information("Could not stop Language Server because StopAsync is null");
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
            IsReady = true;
            FireOnLanguageServerReadyAsyncEvent();
            SendPluginInstalledEvent();
            Rpc.Disconnected += Rpc_Disconnected;
            SubscribeToSolutionOpenedForMigration();
            return Task.CompletedTask;
        }

        // Subscribes MigrateLegacySolutionSettingsAsync to the solution-opened event so that
        // per-solution legacy settings are migrated whenever the user opens a different solution
        // while the LS stays alive (multi-solution VS session). Unsubscribes first so repeated
        // server restarts don't accumulate duplicate handlers.
        private void SubscribeToSolutionOpenedForMigration()
        {
            try
            {
                // Remove any previous subscription to stay idempotent across server restarts.
                UnsubscribeFromSolutionOpenedForMigration();

                var solutionEvents = SnykVSPackage.ServiceProvider?.SolutionService?.SolutionEvents;
                if (solutionEvents == null)
                    return;

                solutionOpenedMigrationHandler = (_, __) =>
                    ThreadHelper.JoinableTaskFactory.RunAsync(MigrateLegacySolutionSettingsAsync).FireAndForget();

                solutionEvents.AfterBackgroundSolutionLoadComplete += solutionOpenedMigrationHandler;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Could not subscribe to solution-opened event for legacy settings migration.");
            }
        }

        // Detaches the solution-opened migration handler if subscribed. Idempotent and best-effort,
        // so it is safe to call from both the explicit stop teardown (StopServerAsync) and the RPC
        // disconnect path: when the LS stops for good (extension disable / VS shutdown) this stops a
        // later solution-open from running MigrateLegacySolutionSettingsAsync against a dead LS and
        // leaking the closure. On a transient disconnect the handler is re-attached by the next
        // OnServerInitializedAsync.
        private void UnsubscribeFromSolutionOpenedForMigration()
        {
            try
            {
                if (solutionOpenedMigrationHandler == null)
                    return;

                var solutionEvents = SnykVSPackage.ServiceProvider?.SolutionService?.SolutionEvents;
                if (solutionEvents != null)
                    solutionEvents.AfterBackgroundSolutionLoadComplete -= solutionOpenedMigrationHandler;

                solutionOpenedMigrationHandler = null;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Could not unsubscribe from solution-opened event for legacy settings migration.");
            }
        }

        private Task<string> GetLsDebugLevelAsync()
        {
            var serviceProvider = SnykVSPackage.ServiceProvider;
            var options = serviceProvider?.Options;
            if (options == null)
                return Task.FromResult("info");

            // Enable debug logging for the whole LS process if -d/--debug is set in global
            // additional parameters OR in ANY folder's additional parameters.
            var globalParams = options.AdditionalParameters ?? Enumerable.Empty<string>();
            var folderParams = (options.FolderConfigs ?? Enumerable.Empty<FolderConfig>())
                .Where(fc => fc?.AdditionalParameters != null)
                .SelectMany(fc => fc.AdditionalParameters);

            var anyDebug = globalParams.Concat(folderParams).Any(p => p == "-d" || p == "--debug");

            return Task.FromResult(anyDebug ? "debug" : "info");
        }

        // One-time, best-effort migration of legacy per-solution settings (IDE-1651) into the folder
        // config, run just before the LS starts so the migrated values reach it via the initialization
        // options. Idempotent — once an entry is migrated it is removed, so later starts are no-ops.
        private static async Task MigrateLegacySolutionSettingsAsync()
        {
            try
            {
                var serviceProvider = SnykVSPackage.ServiceProvider;
                var optionsManager = serviceProvider?.SnykOptionsManager;
                var solutionService = serviceProvider?.SolutionService;
                if (optionsManager == null || solutionService == null)
                    return;

                var solutionFolder = await solutionService.GetSolutionFolderAsync();
                optionsManager.MigrateLegacySolutionSettings(solutionFolder);
            }
            catch (Exception ex)
            {
                // Error (not Warning) so a genuine migration failure is visible in diagnostics — the
                // no-op path returns without throwing, so reaching here means real legacy settings
                // (which can include auth tokens / custom filters) failed to migrate. Still best-effort
                // and non-fatal: the migration is idempotent and retries on the next solution-open or
                // LS restart, so we continue startup without a disruptive user-facing prompt.
                Logger.Error(ex, "Legacy per-solution settings migration failed; continuing LS startup.");
            }
        }

        private void Rpc_Disconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            IsReady = false;
            UnsubscribeFromSolutionOpenedForMigration();
        }

        public async Task AttachForCustomMessageAsync(JsonRpc rpc)
        {
            await Task.Yield();
            Rpc = new JsonRpcWrapper(rpc);
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
            var isFolderTrusted = await SnykVSPackage.ServiceProvider.TasksService.IsFolderTrustedAsync();
            if (!isFolderTrusted)
            {
                return null;
            }

            var param = new LSP.ExecuteCommandParams {
                Command = LsConstants.SnykWorkspaceScan
            };
            var res = await InvokeWithParametersAsync<object>(LsConstants.WorkspaceExecuteCommand, param, cancellationToken);
            return res;
        }
        public async Task<object> SendCodeFixDiffsAsync(string issueID, CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams {
                Command = LsConstants.SnykCodeFixDiffs,
                Arguments = new object[] { issueID }
            };
            var res = await InvokeWithParametersAsync<object>(LsConstants.WorkspaceExecuteCommand,param, cancellationToken);
            return res;
        }
        public async Task<object> SendApplyFixDiffsAsync(string fixID, CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams
            {
                Command = LsConstants.SnykCodeFixApplyEdit,
                Arguments = new object[] { fixID }
            };
            var res = await InvokeWithParametersAsync<object>(LsConstants.WorkspaceExecuteCommand, param, cancellationToken);
            return res;
        }
        public async Task<object> SubmitIgnoreRequestAsync(string workflow, string issueId, string ignoreType, string ignoreReason, string ignoreExpirationDate, CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams
            {
                Command = LsConstants.SnykSubmitIgnoreRequest,
                Arguments = new object[] { workflow, issueId, ignoreType, ignoreReason, ignoreExpirationDate }
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

        public async Task<object> InvokeExecuteCommandAsync(string command, object[] args, CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams
            {
                Command = command,
                Arguments = args
            };
            return await InvokeWithParametersAsync<object>(LsConstants.WorkspaceExecuteCommand, param, cancellationToken);
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

        public async Task<FeatureFlagResponse> InvokeGetFeatureFlagStatusAsync(string featureFlag, CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams
            {
                Command = LsConstants.SnykGetFeatureFlagStatus,
                Arguments = new object[] { featureFlag }
            };
            return await InvokeWithParametersAsync<FeatureFlagResponse>(LsConstants.WorkspaceExecuteCommand, param, cancellationToken);
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

        /// <summary>
        /// Retrieves HTML configuration UI from the Language Server.
        /// Returns null if LS is not available or command fails.
        /// </summary>
        public async Task<string> GetConfigHtmlAsync(CancellationToken cancellationToken)
        {
            var param = new LSP.ExecuteCommandParams
            {
                Command = LsConstants.SnykWorkspaceConfiguration
            };
            return await InvokeWithParametersAsync<string>(LsConstants.WorkspaceExecuteCommand, param, cancellationToken);
        }

        public async Task<object> DidChangeConfigurationAsync(CancellationToken cancellationToken)
        {
            if (!IsReady) return default;

            settingsV25 ??= new LsSettingsV25(SnykVSPackage.ServiceProvider);
            var config = settingsV25.GetLspConfigurationParam();
            if (config == null)
            {
                Logger.Warning("DidChangeConfigurationAsync: GetLspConfigurationParam returned null; skipping workspace/didChangeConfiguration notification.");
                return default;
            }
            var param = new LSP.DidChangeConfigurationParams { Settings = config };
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
            try
            {
                return await Rpc.InvokeWithParameterObjectAsync<T>(request, parameters, t).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error("{Ex}" ,ex);
                return default;
            }
        }

        private async Task NotifyWithParametersAsync(string request, object parameters)
        {
            if (!IsReady) return;
            await Rpc.NotifyWithParameterObjectAsync(request, parameters).ConfigureAwait(false);
        }
    }
}