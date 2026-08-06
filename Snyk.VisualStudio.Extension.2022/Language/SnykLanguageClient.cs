using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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

        // Set when VS calls OnLoadedAsync. Raising StartAsync before that point is out of contract:
        // VS has already subscribed (so StartAsync is non-null and the guard below passes) but it is
        // not yet willing to activate, so the invoke returns immediately, ActivateAsync is never
        // called, no snyk-win process appears — and nothing retries, because OnLoadedAsync is the
        // only other thing that starts the server. Verified from a failing startup: "raising
        // StartAsync to VS" returned in 0ms with no "ActivateAsync: launching", against 2.7s and a
        // successful launch on a start raised after OnLoadedAsync.
        private volatile bool vsHasLoadedClient;

        // internal setter for testability: in production only OnLoadedAsync sets this, and only VS
        // calls OnLoadedAsync — so a test exercising StartServerAsync or RestartServerAsync directly
        // has no other way to establish the precondition those paths now require.
        internal bool VsHasLoadedClient
        {
            get => this.vsHasLoadedClient;
            set => this.vsHasLoadedClient = value;
        }
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

        // Serializes the peek→send→commit sequence of DidChangeConfigurationAsync (IDE-2152 fix #3).
        // The config push is fire-and-forget from several call sites; without this, two overlapping
        // sends would each peek the same queue and double-deliver, and their commits could race the
        // tracker/persistence. Distinct from `semaphore` (which guards start/stop).
        private readonly SemaphoreSlim configSendGate = new SemaphoreSlim(1, 1);

        public object GetInitializationOptions()
        {
            settingsV25 ??= new LsSettingsV25(SnykVSPackage.ServiceProvider);

            // Init folds in the current pending resets (non-destructively) but does NOT commit them
            // here (IDE-2152 fix #3): committing at init ran on a separate code path that could not be
            // serialized with the DidChangeConfiguration commits. Instead the first successful
            // DidChangeConfiguration commits, and persistence (fix #2) re-delivers across a restart if
            // the handshake never confirms. Re-sending the same reset is idempotent for the LS.
            //
            // Deferred-commit invariant (IDE-2152 fix #3, made explicit): after a SUCCESSFUL init that
            // carried the resets to the LS, those resets REMAIN queued in the tracker AND persisted in
            // settings.json (PendingResetConfigKeys) — init never drains them. They are drained by the
            // NEXT successful DidChangeConfigurationAsync, which peeks the same queue, sends, and only
            // then commits (peek→send→commit, through the options manager so persistence stays in
            // sync). This is safe precisely because a reset is idempotent for the LS: init delivering a
            // reset and the first config-update re-delivering the same reset produce the same LS state,
            // so double-delivery across the init→first-update boundary cannot corrupt anything. It also
            // means a crash between a successful init and the first config-update loses nothing — the
            // persisted queue re-delivers on the next session.
            var initializationOptions = settingsV25.GetInitializationOptions();

            // The folder set is fixed here for the life of the server — there is no
            // didChangeWorkspaceFolders in this client — so this line is the record of what the server
            // was given. A blank folder here is the signature of a start that beat the solution load,
            // which previously looked like a healthy startup in every log we had.
            Logger.Debug(
                "InitializationOptions requested; solution folder is '{Folder}'",
                SnykVSPackage.ServiceProvider?.SolutionService?.SolutionFolderCache ?? string.Empty);

            return initializationOptions;
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

            Logger.Information(
                "ActivateAsync: launching {FileName} {Arguments}",
                info.FileName,
                info.Arguments);

            try
            {
                var isStarted = process.Start();

                Logger.Information(
                    "ActivateAsync: process.Start returned {IsStarted}, pid {Pid}",
                    isStarted,
                    isStarted ? process.Id.ToString() : "(none)");

                return isStarted ? new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream) : null;
            }
            catch (Exception e)
            {
                // Rethrown: VS owns the failure path. Logged because it was previously invisible.
                Logger.Error(e, "ActivateAsync: process.Start threw for {FileName}", info.FileName);

                throw;
            }
        }

        // Generous: only ever used when package initialization has faulted. Observed init takes
        // roughly four seconds.
        //
        // internal and settable for testability: the completion source is static and is never completed
        // under test, so a test that deliberately leaves the package uninitialised would time out.
        internal static TimeSpan PackageInitializationTimeout = TimeSpan.FromSeconds(30);

        // Bounded so a solution that never finishes loading costs a server with no folder rather than no
        // server at all. Generous because overshooting delays nothing in the common cases: a solution
        // that is loading resolves in well under a second, and no-solution returns without waiting.
        // internal and settable for the same reason as the timeout above — tests must not sit here.
        internal static TimeSpan SolutionLoadTimeout = TimeSpan.FromSeconds(30);

        internal static TimeSpan SolutionLoadPollInterval = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Blocks this load callback until the package has finished initializing.
        ///
        /// Bounded, because <c>initializationTaskCompletionSource</c> is only completed on the success
        /// path — a faulted initialization must not hang a VS load callback forever.
        /// </summary>
        private static async Task<bool> WaitForPackageInitializationAsync()
        {
            if (SnykVSPackage.Instance?.IsInitialized ?? false)
            {
                return true;
            }

            // Paired with the completion log below.
            Logger.Information("Waiting for package initialisation before deciding whether to start the language server");

            var initialized = SnykVSPackage.PackageInitializedAwaiter;
            var finished = await Task.WhenAny(initialized, Task.Delay(PackageInitializationTimeout));

            if (finished != initialized)
            {
                Logger.Warning(
                    "Package initialisation did not complete within {Seconds}s, so this language client load will not start the server",
                    PackageInitializationTimeout.TotalSeconds);

                return false;
            }

            Logger.Information("Package initialisation complete; this language client load can start the language server");

            return SnykVSPackage.Instance?.IsInitialized ?? false;
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
            // Set before the download check below, which can take seconds: a start request arriving in
            // that window is now allowed to proceed rather than being silently discarded by VS.
            this.vsHasLoadedClient = true;

            // Wait for package initialisation rather than sampling it — see
            // WaitForPackageInitializationAsync for why this must not be a bool read. This is the only
            // in-contract chance to start the server: a raise of StartAsync outside VS's own load flow
            // is discarded.
            var isPackageInitialized = await WaitForPackageInitializationAsync();

            // Off the UI thread before asking. ShouldDownloadCli issues a synchronous release lookup and
            // hashes the ~175MB binary while holding SnykCliDownloader.memoLock; VS does not document
            // which thread it calls OnLoadedAsync on, and SnykVSPackage.InitializeLanguageClient already
            // hops for the same reason at the sibling call site.
            await TaskScheduler.Default;

            var shouldStart = isPackageInitialized && !SnykVSPackage.ServiceProvider.TasksService.ShouldDownloadCli();
            Logger.Information("OnLoadedAsync Called and shouldStart is: {ShouldStart}", shouldStart);

            // Wait for the solution before starting, because the folder set is fixed at initialize time.
            // InitializationOptions is the only channel that carries workspace folders to the server —
            // there is no didChangeWorkspaceFolders in this client — so a server started before the
            // solution has loaded runs against no folder for the rest of its life, and the only remedy
            // is a full restart. Previously the wait happened by accident: a CLI download took long
            // enough for the solution to load, so the failure only showed on the no-download path.
            if (shouldStart)
            {
                await WaitForSolutionLoadAsync();
            }

            // StartServerAsync marshals to the UI thread itself; no switch needed here.
            await StartServerAsync(shouldStart);
        }

        /// <summary>
        /// Waits until a solution has finished loading, or until it is clear that none is coming.
        ///
        /// Not a bare wait on the solution-loaded event: Visual Studio opens perfectly well with no
        /// solution at all, and the settings page needs the server up in that state — waiting
        /// unconditionally would leave it on its fallback HTML forever. So this returns as soon as
        /// either a solution is available or VS reports that nothing is being opened, and it is bounded
        /// regardless so a wait that never resolves costs a degraded start rather than no start.
        /// </summary>
        private static async Task WaitForSolutionLoadAsync()
        {
            // Instance first: the static ServiceProvider property dereferences it without a guard.
            if (SnykVSPackage.Instance == null || SnykVSPackage.ServiceProvider == null)
            {
                Logger.Debug("Service provider unavailable; not waiting for a solution");

                return;
            }

            var deadline = DateTime.UtcNow + SolutionLoadTimeout;
            var attempt = 0;

            while (true)
            {
                attempt++;

                var state = await GetSolutionStateAsync();

                // Open and resolvable: the start path's own GetSolutionFolderAsync will find it. That
                // call is deliberately NOT made here — it logs at Information on every invocation, so
                // polling it would fill the log with one line per check.
                if (state.IsOpen)
                {
                    Logger.Debug("Solution reported open after {Attempts} check(s)", attempt);

                    return;
                }

                if (!state.IsOpening)
                {
                    Logger.Debug(
                        "No solution open and none opening after {Attempts} check(s); starting without a folder",
                        attempt);

                    return;
                }

                if (DateTime.UtcNow >= deadline)
                {
                    Logger.Warning(
                        "Solution was still opening after {Seconds}s; starting the language server without waiting further",
                        SolutionLoadTimeout.TotalSeconds);

                    return;
                }

                Logger.Debug("Solution still opening after {Attempts} check(s); waiting", attempt);

                await Task.Delay(SolutionLoadPollInterval);
            }
        }

        /// <summary>
        /// Cheap, non-logging snapshot of whether a solution is open or on its way. Both come from
        /// IVsSolution rather than DTE so neither call writes to the log.
        /// </summary>
        private static async Task<(bool IsOpen, bool IsOpening)> GetSolutionStateAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                // Via ISnykServiceProvider, not the package: AsyncPackage.GetServiceAsync is protected.
                // Same route SnykSolutionService uses to obtain this service.
                if (!(await SnykVSPackage.ServiceProvider.GetServiceAsync(typeof(SVsSolution)) is IVsSolution solution))
                {
                    return (false, false);
                }

                var isOpen = ErrorHandler.Succeeded(
                                 solution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var openValue))
                             && openValue is bool opened && opened;

                // Covers the background-load window, which VSPROPID_IsSolutionOpen reports false for.
                var isOpening = ErrorHandler.Succeeded(
                                    solution.GetProperty((int)__VSPROPID4.VSPROPID_IsSolutionOpening, out var openingValue))
                                && openingValue is bool opening && opening;

                return (isOpen, isOpening);
            }
            catch (Exception e)
            {
                // Reported as "nothing open, nothing coming" so a probe failure starts the server rather
                // than holding it back for the whole timeout.
                Logger.Debug(e, "Could not read solution state; proceeding without waiting");

                return (false, false);
            }
        }

        public async Task StartServerAsync(bool shouldStart = false)
        {
            // Marshalled here, BEFORE the semaphore, rather than left to whichever thread the caller
            // happened to be on — there are three call sites and they arrive on different threads.
            // A raise from a pool thread was observed to produce no ActivateAsync at all, while a
            // UI-thread raise four seconds earlier in the same session was honoured, so the raise is
            // pinned to the thread VS drives its own load flow from.
            //
            // Before the gate and not inside it: switching while holding the semaphore is a deadlock
            // shape. JoinableTaskFactory can inline work to avoid deadlocking on itself, but it cannot
            // see through a SemaphoreSlim, and the UI thread does block in JoinableTaskFactory.Run
            // elsewhere in this extension. Awaiting the semaphore from the main thread yields it, so
            // nothing is blocked while we wait.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Logger.Debug(
                "StartServerAsync entered: shouldStart={ShouldStart}, vsHasLoadedClient={Loaded}, vsSubscribed={Subscribed}, isReady={IsReady}, onUiThread={OnUiThread}",
                shouldStart,
                this.vsHasLoadedClient,
                StartAsync != null,
                IsReady,
                ThreadHelper.CheckAccess());

            await semaphore.WaitAsync();
            try
            {
                // Handle start request that arrive before VS has loaded the client. These ask for
                // activation instead.
                if (shouldStart && (StartAsync == null || !this.vsHasLoadedClient))
                {
                    Logger.Information(
                        "Language server start requested before VS loaded the client; requesting activation instead (VS subscribed: {Subscribed})",
                        StartAsync != null);

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

                    // Raising StartAsync asks VS to call ActivateAsync; it is not a guarantee. The
                    // guard above is what keeps this in contract — see its comment. VS also ignores the
                    // request outright when it considers the server already started, so the pair of
                    // lines below plus ActivateAsync's own logging are what distinguish "VS declined"
                    // from "the launch failed".
                    Logger.Information("Starting Language Server");
                    Logger.Debug(
                        "Raising StartAsync on the {ThreadKind} thread; expect an ActivateAsync line next if VS accepts it",
                        ThreadHelper.CheckAccess() ? "UI" : "background");

                    await StartAsync.InvokeAsync(this, EventArgs.Empty);

                    Logger.Debug("StartAsync raise returned");
                }
                else
                {
                    Logger.Information("Couldn't Start Language Server");
                    Logger.Debug(
                        "Not started because: vsSubscribed={Subscribed}, optionsPresent={Options}, shouldStart={ShouldStart}",
                        StartAsync != null,
                        SnykVSPackage.Instance?.Options != null,
                        shouldStart);
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
            // The caller matters more than the fact here. A language-server shutdown was observed with
            // no explanation in this codebase — every StopServerAsync path was ruled out by elimination,
            // which is slow and inconclusive. If a shutdown appears in the server's log with no line
            // from here, it was Visual Studio's decision, not ours.
            //
            // Guarded, unlike the other Debug calls: Serilog evaluates arguments eagerly, so an
            // unguarded StackTrace would be captured on every stop even with Debug suppressed.
            if (LogManager.IsDebugEnabled)
            {
                Logger.Debug(
                    "StopServerAsync called: vsSubscribed={Subscribed}, isReady={IsReady}, isReloading={IsReloading}, calledFrom={Caller}",
                    StopAsync != null,
                    IsReady,
                    IsReloading,
                    new StackTrace(1, false).ToString());
            }

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
            // The only positive confirmation that a start actually produced a usable server. "Starting
            // Language Server" followed by ActivateAsync only proves a process was launched.
            Logger.Debug("OnServerInitializedAsync: server is ready");

            IsReady = true;

            // Note: pending resets are NOT committed here (IDE-2152 fix #3). The init options already
            // carried them to the LS, but committing is deferred to the first successful
            // DidChangeConfiguration (peek→send→commit), which is serialized and commits through the
            // options manager so persistence stays in sync. If the handshake never confirms, the
            // persisted pending-reset set (fix #2) re-delivers them after a restart.
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

            // Same signal as the extension's own log level — see LanguageClientHelper.IsDebugModeRequested.
            return Task.FromResult(LanguageClientHelper.IsDebugModeRequested(options) ? "debug" : "info");
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
            // The reason and description say whether the server exited, was killed, or the pipe broke —
            // which is otherwise indistinguishable from "VS never activated us" in a log.
            Logger.Debug(
                "Rpc disconnected: reason={Reason}, description={Description}",
                e?.Reason,
                e?.Description);

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
            // A restart is a stop and a start, and the start half is the fragile one: VS ignores a start
            // request for a server it still considers running, so the two halves are logged separately
            // to tell "the stop did not finish first" from "the start was declined".
            Logger.Debug("RestartAsync entered: isReload={IsReload}, isReady={IsReady}", isReload, IsReady);

            try
            {
                if (isReload)
                {
                    IsReloading = true;
                }
                OnStopping();
                await StopServerAsync();
                OnStopped();

                Logger.Debug("RestartAsync: stop half complete, starting");

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
            // Early-return BEFORE taking the gate: after a FAILED init, IsReady stays false so this
            // returns without sending. Recovery from a failed init is NOT via a config update — it is
            // the next successful (re-)initialization handshake, which flips IsReady back to true
            // (IDE-2152 fix #6: correct the previous stale comment that claimed a config-update
            // recovered a failed init).
            if (!IsReady) return default;

            settingsV25 ??= new LsSettingsV25(SnykVSPackage.ServiceProvider);
            var optionsManager = SnykVSPackage.ServiceProvider?.SnykOptionsManager;

            // Serialize the whole peek→send→commit sequence (IDE-2152 fix #3): the config push is
            // fire-and-forget from several call sites, so overlapping calls would otherwise each peek
            // the same pending-reset queue and double-deliver, and their commits would race the tracker
            // and its persistence. The gate makes exactly one push + its commit run at a time.
            await configSendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Re-check readiness after acquiring the gate: a Rpc disconnect may have flipped IsReady
                // to false while we were queued behind another send.
                if (!IsReady) return default;

                // Peek the pending resets ONCE, then thread this EXACT snapshot through the settings-map
                // build AND the commit, so committed == sent by construction. Building the map with a
                // second independent peek (as before) let a reset enqueued between the two reads be
                // sent-but-not-committed (re-sent) or committed-but-not-sent (dropped).
                var pendingResets = optionsManager?.OverrideTracker?.PeekPendingResets();

                var config = settingsV25.GetLspConfigurationParam(pendingResets);
                if (config == null)
                {
                    Logger.Warning("DidChangeConfigurationAsync: GetLspConfigurationParam returned null; skipping workspace/didChangeConfiguration notification.");
                    return default;
                }
                var param = new LSP.DidChangeConfigurationParams { Settings = config };

                // Do NOT route through InvokeWithParametersAsync here: that helper swallows every
                // exception and returns default, which would hide a failed send and let the peeked
                // resets be committed anyway. The config-update path must OBSERVE success/failure so it
                // can decide whether to commit the resets. On failure we log and leave the queue intact.
                try
                {
                    var result = await Rpc.InvokeWithParameterObjectAsync<object>(
                        LsConstants.WorkspaceChangeConfiguration, param, cancellationToken).ConfigureAwait(false);

                    // Confirmed successful send: commit EXACTLY the snapshot we sent, through the options
                    // manager so the pending-reset persistence is updated too (IDE-2152 fix #2). Never a
                    // blanket clear — a newer reset for a different key enqueued after this peek is not
                    // in `pendingResets`, so it survives for the next update.
                    //
                    // Marshal to the UI thread BEFORE committing (IDE-2152 fix #7). CommitPendingResets
                    // persists settings.json, and the whole settings-persistence subsystem was written
                    // assuming UI-thread-only saves. Committing here (the RPC continuation runs on a
                    // thread-pool thread after `ConfigureAwait(false)` above) would make this the first
                    // background-thread settings writer, racing the pre-existing unlocked UI-thread
                    // mutation sites (MigrateLegacySolutionSettings, LoadSettingsFromFile) and throwing
                    // "collection was modified" mid-serialize. Switching to the main thread makes this
                    // commit persist exactly like every other settings save — no background writer, so
                    // the entire race class dissolves. Same pattern as HtmlSettingsControl /
                    // AuthenticationFlowService. The switch happens only AFTER a confirmed send, still
                    // inside the configSendGate region; holding that SemaphoreSlim (async, not a Monitor)
                    // across the await is safe. On failure (catch below) we never switch and never commit.
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    optionsManager?.CommitPendingResets(pendingResets);
                    return result;
                }
                catch (OperationCanceledException)
                {
                    // Benign shutdown: the cancellationToken fired (IDE/LS shutting down) either DURING
                    // the RPC send above — which also takes this token, so the LS may NOT have received
                    // the notification — or AFTER a confirmed send, during the post-send main-thread
                    // marshal. In BOTH sub-cases skipping the commit is safe: the reset stays pending and
                    // re-delivers idempotently on the next configuration update (re-sending an already-
                    // delivered reset is a harmless no-op). Do NOT log at Error — this is expected, not a
                    // fault.
                    //
                    // Precondition: this benign treatment assumes callers pass a shutdown/DisposalToken
                    // (all current callers do). A future per-operation or timeout token that could cancel
                    // AFTER a confirmed send would cause the reset to re-deliver once more on the next
                    // update; if such a token is introduced, revisit whether it belongs in this benign path.
                    return default;
                }
                catch (Exception ex)
                {
                    // Transient failure: leave pendingResets in the queue (and in persistence) so the
                    // next configuration update re-delivers them. Match the (silent-to-caller) contract
                    // of the previous send helper.
                    Logger.Error("{Ex}", ex);
                    return default;
                }
            }
            finally
            {
                configSendGate.Release();
            }
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