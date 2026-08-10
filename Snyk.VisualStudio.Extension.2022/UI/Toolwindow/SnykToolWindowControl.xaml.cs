using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Serilog;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Commands;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.Theme;
using Snyk.VisualStudio.Extension.UI.Notifications;
using Task = System.Threading.Tasks.Task;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Interaction logic for SnykToolWindowControl. Implements <see cref="IDisposable"/> so
    /// <see cref="SnykToolWindow"/>'s base <c>ToolWindowPane.Dispose</c> chains cleanup into
    /// the WebView2-hosting child panels when the tool window is destroyed.
    /// </summary>
    public partial class SnykToolWindowControl : UserControl, ISnykToolWindow, IDisposable
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykToolWindowControl>();

        private SnykToolWindow toolWindow;

        private ISnykServiceProvider serviceProvider;

        private ToolWindowContext context;

        private bool disposed;

        // 1 when this control stopped the language server so a download could overwrite its binary.
        // An int rather than a bool so it can be read-and-cleared in one atomic step: it is written on
        // whichever thread raised DownloadStarted and consumed on whichever thread raised the outcome.
        private int stoppedServerForDownload;

        // Subscription owners + the lambda handlers, retained so Dispose can detach symmetrically.
        // tasksService is a long-lived singleton, so a leaked subscription would keep firing
        // handlers against this control after the tool window is torn down.
        private ISnykTasksService tasksService;
        private SnykSolutionService solutionService;
        private ILanguageClientManager languageClientManager;
        private EventHandler<SnykCodeScanEventArgs> codeScanningFinishedHandler;
        private EventHandler<SnykOssScanEventArgs> ossScanErrorHandler;
        private EventHandler<SnykOssScanEventArgs> ossScanningFinishedHandler;
        private EventHandler<SnykCodeScanEventArgs> iacScanningFinishedHandler;
        private EventHandler<EventArgs> taskFinishedHandler;
        private EventHandler<SnykCliDownloadEventArgs> downloadStartedHandler;
        private EventHandler<SnykCliDownloadEventArgs> downloadFinishedHandler;
        private EventHandler<SnykCliDownloadEventArgs> downloadUpdateHandler;
        private RoutedEventHandler loadedHandler;
        private SnykScanCommand.UpdateControlsState updateControlsStateCallback;

        // ISnykToolWindow seam. The x:Name fields below are the concrete WPF panels; these
        // explicit members expose them as the testable interfaces used by ISnykServiceProvider
        // collaborators (the LS message handlers, the auth flow).
        ITreeHtmlPanel ISnykToolWindow.TreeHtmlPanel => this.TreeHtmlPanel;

        IHtmlPanel ISnykToolWindow.SummaryPanel => this.SummaryPanel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykToolWindowControl"/> class.
        /// </summary>
        /// /// <param name="toolWindow">Tool window instance..</param>
        public SnykToolWindowControl(SnykToolWindow toolWindow)
        {
            this.toolWindow = toolWindow;

            this.InitializeComponent();

            this.context = new ToolWindowContext(this);

            this.DescriptionPanel.Init();

            this.SummaryPanel.Init();
            this.messagePanel.Context = this.context;

            // Paint the WPF chrome (the area behind the message/overview panels and the splitter
            // gaps) with the same themed tool-window background the WebView2 HTML panels use, so
            // everything matches. Set in code via VSColorTheme because the legacy VsBrush.* string
            // resources for these keys don't reliably resolve. Re-apply on theme switch.
            this.ApplyThemedColors();
            VSColorTheme.ThemeChanged += this.OnVsThemeChanged;
        }

        private void OnVsThemeChanged(ThemeChangedEventArgs e) => this.ApplyThemedColors();

        private void ApplyThemedColors()
        {
            var bg = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            var border = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBorderColorKey);

            this.mainGrid.Background = new SolidColorBrush(
                System.Windows.Media.Color.FromArgb(bg.A, bg.R, bg.G, bg.B));

            var borderBrush = new SolidColorBrush(
                System.Windows.Media.Color.FromArgb(border.A, border.R, border.G, border.B));
            this.horizontalDivider.Background = borderBrush;
            this.verticalDivider.Background = borderBrush;
        }

        /// <summary>
        /// Gets a value indicating whether the issue tree currently has results.
        /// Backed by the total-issue count from the last <c>$/snyk.treeView</c> notification.
        /// </summary>
        /// <returns>True if the result tree has at least one issue.</returns>
        public bool IsTreeContentNotEmpty() => this.TreeHtmlPanel.TotalIssues > 0;

        /// <summary>
        /// Initialize event listeners for UI.
        /// </summary>
        /// <param name="serviceProvider">Service provider implementation.</param>
        public void InitializeEventListeners(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.messagePanel.ServiceProvider = serviceProvider;

            Logger.Information("Enter InitializeEventListenersAsync() method.");

            Logger.Information("Initialize Solultion Event Listeners");

            this.solutionService = serviceProvider.SolutionService as SnykSolutionService;

            this.solutionService.SolutionEvents.AfterCloseSolution += this.OnAfterCloseSolution;

            Logger.Information("Initialize CLI Event Listeners");

            this.tasksService = serviceProvider.TasksService;

            // Scan results are rendered by the LS-driven HTML tree (the $/snyk.treeView
            // notification), so the per-product ScanningUpdate/ScanningDisabled handlers that
            // populated the old native tree are gone. We still react to Started/Error/Finished to
            // drive the right-pane message panel, toolbar command state, and error notifications.
            // Lambda handlers are stored in fields so Dispose can detach them symmetrically.
            this.codeScanningFinishedHandler = (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(this.OnSnykCodeScanningFinishedAsync);
            this.ossScanErrorHandler = (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(() => this.OnOssDisplayErrorAsync(sender, args));
            this.ossScanningFinishedHandler = (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(this.OnOssScanningFinishedAsync);
            this.iacScanningFinishedHandler = (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(this.OnIacScanningFinishedAsync);
            this.taskFinishedHandler = (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(this.OnTaskFinishedAsync);
            this.downloadStartedHandler = (sender, args) =>
            {
                // The binary is about to be overwritten, so a running server has to let go of it. Recorded
                // rather than re-derived later: every handler below needs to know whether the server was
                // taken down for this, and by the time they run the readiness flag no longer says.
                var stopping = LanguageClientHelper.IsLanguageServerReady();
                Interlocked.Exchange(ref this.stoppedServerForDownload, stopping ? 1 : 0);

                Logger.Debug("DownloadStarted: languageServerReady={Ready}; the stop is issued only when ready", stopping);

                if (stopping)
                    ThreadHelper.JoinableTaskFactory.RunAsync(serviceProvider.LanguageClientManager.StopServerAsync).FireAndForget();
                this.OnDownloadStarted(sender, args);
            };
            this.downloadFinishedHandler = (sender, args) =>
            {
                // cliUsable without probing: this outcome means the checksum matched, either because the
                // download just verified it or because the binary on disk was already current. The
                // protocol probe the failure paths run costs a CLI launch and would prove nothing here.
                this.EnsureServerOnConfiguredCli(
                    cliUsable: true,
                    stopIssued: this.ConsumeStoppedServerForDownload(),
                    cliSettingsChanged: args?.CliSettingsChanged == true,
                    source: "DownloadFinished");

                this.OnDownloadFinished(sender, args);
            };
            this.downloadUpdateHandler = (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(() => this.OnDownloadUpdateAsync(sender, args));

            this.tasksService.SnykCodeScanningStarted += this.OnSnykCodeScanningStarted;
            this.tasksService.SnykCodeScanError += this.OnSnykCodeDisplayError;
            this.tasksService.SnykCodeScanningFinished += this.codeScanningFinishedHandler;

            this.tasksService.OssScanningStarted += this.OnOssScanningStarted;
            this.tasksService.OssScanError += this.ossScanErrorHandler;
            this.tasksService.OssScanningFinished += this.ossScanningFinishedHandler;

            this.tasksService.IacScanningStarted += this.OnIacScanningStarted;
            this.tasksService.IacScanError += this.OnIacScanError;
            this.tasksService.IacScanningFinished += this.iacScanningFinishedHandler;


            this.tasksService.ScanningCancelled += this.OnScanningCancelled;
            this.tasksService.TaskFinished += this.taskFinishedHandler;

            Logger.Information("Initialize Download Event Listeners");

            this.tasksService.DownloadStarted += this.downloadStartedHandler;
            this.tasksService.DownloadFinished += this.downloadFinishedHandler;
            this.tasksService.DownloadUpdate += this.downloadUpdateHandler;
            // Both outcomes, one handler: what this control has to do depends on whether it stopped the
            // server for a download, which it recorded itself — not on which of the two ended the check.
            this.tasksService.CliDownloadDeclined += this.OnCliDownloadDidNotComplete;
            this.tasksService.CliDownloadAborted += this.OnCliDownloadDidNotComplete;
            this.tasksService.DownloadFailed += this.OnDownloadFailed;

            // The LanguageClientManager is created during package init (SetLanguageClientManagerAsync)
            // and can still be null here when VS restores the docked Snyk window early on startup, so
            // InitializeEventListeners runs before the manager is assigned — dereferencing it caused a
            // NullReferenceException that aborted the whole tool window. Guard it like MessagePanel and
            // RequestInitialTree already do. If the manager isn't ready yet the LS isn't ready either,
            // so there's no initial tree to pull; the LS pushes $/snyk.treeView once it comes up, which
            // renders the tree regardless of this (proactive-only) subscription.
            this.languageClientManager = LanguageClientHelper.LanguageClientManager();
            if (this.languageClientManager != null)
            {
                this.languageClientManager.OnLanguageServerReadyAsync += OnOnLanguageServerReadyAsync;
            }
            else
            {
                Logger.Warning("LanguageClientManager not available during InitializeEventListeners; skipping LS-ready subscription (tree will populate from $/snyk.treeView pushes).");
            }

            this.loadedHandler = (sender, args) => this.tasksService.EnsureCliReady();
            this.Loaded += this.loadedHandler;

            // Stored in a field so Dispose can detach it: SnykScanCommand.Instance is a static
            // singleton that outlives the tool window, and this lambda captures `this`. Without the
            // teardown detach, a later UpdateState() call would touch the disposed control.
            this.updateControlsStateCallback = (isEnabled) => ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.messagePanel.runScanButton.IsEnabled = isEnabled;

                if (!LanguageClientHelper.IsLanguageServerReady())
                {
                    this.context.TransitionTo(InitializingState.Instance);
                    return;
                }
                if (!isEnabled)
                {
                    this.DetermineInitScreen();
                }
            });
            SnykScanCommand.Instance.UpdateControlsStateCallback = this.updateControlsStateCallback;

            Logger.Information("Leave InitializeEventListenersAsync() method.");
        }

        private async Task OnOnLanguageServerReadyAsync(object sender, SnykLanguageServerEventArgs args)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            this.DetermineInitScreen();
            this.TreeHtmlPanel.RequestInitialTree();
        }

        private void OnIacScanError(object sender, SnykCodeScanEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (e.PresentableError?.ShowNotification ?? true)
                {
                    NotificationService.Instance.ShowErrorInfoBar(e.PresentableError?.ErrorMessage);
                }

                if (!this.serviceProvider.Options.OssEnabled)
                {
                    this.context.TransitionTo(RunScanState.Instance);
                }

                await this.UpdateActionsStateAsync();
            });
        }

        private void OnIacScanningStarted(object sender, SnykCodeScanEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.messagePanel.ShowScanningMessage();
                this.mainGrid.Visibility = Visibility.Visible;

                await this.UpdateActionsStateAsync();
            });
        }

        /// <summary>
        /// Shows the issue detail panel for the given issue. Driven by the LS
        /// <c>window/showDocument</c> "showInDetailPanel" callback when a node is clicked in the
        /// HTML tree. The description HTML is fetched lazily by <see cref="FillHtmlPanel"/>.
        /// </summary>
        public void SelectedItemInTree(string issueId, string product)
        {
            if (string.IsNullOrEmpty(issueId)) return;
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.messagePanel.Visibility = Visibility.Collapsed;
                this.DescriptionPanel.Visibility = Visibility.Visible;
                FillHtmlPanel(issueId, product, null);
            });
        }


        /// <summary>
        /// AfterBackgroundSolutionLoadComplete event handler. Switch context to RunScanState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnAfterBackgroundSolutionLoadComplete(object sender, EventArgs eventArgs)
        {
        }

        /// <summary>
        /// AfterCloseSolution event handler. Switch context to RunScanState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnAfterCloseSolution(object sender, EventArgs eventArgs) => this.Clean();

        /// <summary>
        /// Cli ScanningStarted event handler..
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnOssScanningStarted(object sender, SnykOssScanEventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.messagePanel.ShowScanningMessage();
            this.mainGrid.Visibility = Visibility.Visible;

            await this.UpdateActionsStateAsync();
        });

        /// <summary>
        /// SnykCode ScanningStarted event handler. Switch context to ScanningState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnSnykCodeScanningStarted(object sender, SnykCodeScanEventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.messagePanel.ShowScanningMessage();

            await this.UpdateActionsStateAsync();
        });

        /// <summary>
        /// ScanningFinished event handler. Switch context to ScanResultsState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnScanningFinished(object sender, SnykOssScanEventArgs eventArgs) => this.context.TransitionTo(ScanResultsState.Instance);

        /// <summary>
        /// Handle Cli error.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnOssDisplayErrorAsync(object sender, SnykOssScanEventArgs eventArgs)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (eventArgs.PresentableError?.ShowNotification ?? true)
            {
                NotificationService.Instance.ShowErrorInfoBar(eventArgs.PresentableError?.ErrorMessage);
            }

            if (eventArgs.FeaturesSettings != null && !eventArgs.FeaturesSettings.CodeSecurityEnabled)
            {
                this.context.TransitionTo(RunScanState.Instance);
            }

            await this.UpdateActionsStateAsync();
        }

        /// <summary>
        /// Initialize tool window control.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider instance.</param>
        public void Initialize(ISnykServiceProvider serviceProvider)
        {
        }

        /// <summary>
        /// Handle SnykCode error.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnSnykCodeDisplayError(object sender, SnykCodeScanEventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (eventArgs.PresentableError?.ShowNotification ?? true)
            {
                NotificationService.Instance.ShowErrorInfoBar(eventArgs.PresentableError?.ErrorMessage);
            }

            if (!this.serviceProvider.Options.OssEnabled)
            {
                this.context.TransitionTo(RunScanState.Instance);
            }

            await this.UpdateActionsStateAsync();
        });

        // OnSnykCodeDisabledHandler removed - SAST checks are no longer performed

        /// <summary>
        /// ScanningCancelled event handler. Switch context to ScanResultsState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnScanningCancelled(object sender, SnykOssScanEventArgs eventArgs)
        {
            this.context.TransitionTo(RunScanState.Instance);
        }

        /// <summary>
        /// DownloadStarted event handler. Switch context to DownloadState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnDownloadStarted(object sender, SnykCliDownloadEventArgs eventArgs)
        {
            // RunAsync, not Run: these four download handlers are raised from the thread pool, and a
            // blocking Run parks that pool thread until the UI thread is free. During startup the UI
            // thread is still loading the tool window, so that wait is measurable and can close a cycle
            // with anything the UI thread is itself waiting on. Nothing here needs to
            // complete before the handler returns: the binary is already downloaded, checksum-verified
            // and installed before the event is raised, and the language server start is separately
            // fire-and-forget.
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (eventArgs.IsUpdateDownload)
                {
                    this.context.TransitionTo(UpdateDownloadState.Instance);
                }
                else
                {
                    this.context.TransitionTo(DownloadState.Instance);
                }

                this.Show();
            }).FireAndForget();
        }

        /// <summary>
        /// DownloadFinished event handler. Call SetInitialState() method.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnDownloadFinished(object sender, SnykCliDownloadEventArgs eventArgs)
        {
            // The download runs on the thread pool, so this is raised from there; DetermineInitScreen
            // writes WPF state.
            //
            // RunAsync, not Run — see OnDownloadStarted. Blocking here parked a pool thread on the UI
            // thread during startup, which is a hazard even when it eventually completes.
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.DetermineInitScreen();
            }).FireAndForget();
        }

        /// <summary>
        /// DownloadUpdate event handler. Call UpdateDonwloadProgress() method.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnDownloadUpdateAsync(object sender, SnykCliDownloadEventArgs eventArgs) => await this.UpdateDownloadProgressAsync(eventArgs.Progress);

        /// <summary>
        /// Handles both ways a CLI check can end without a usable download: declined because automatic
        /// management is off, and aborted after one had started.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnCliDownloadDidNotComplete(object sender, SnykCliDownloadEventArgs eventArgs)
        {
            // Consumed here, synchronously, rather than inside the lambda below: the probe in there can in
            // principle throw, and a flag left set would tell the next check the server was already down.
            var stopIssued = this.ConsumeStoppedServerForDownload();

            // RunAsync, not Run — see OnDownloadStarted. Both branches write WPF state.
            //
            // TaskScheduler.Default before the probe, because this handler is NOT always raised from
            // the thread pool: SnykTasksService.EnsureCliReady fires CliDownloadDeclined before its own
            // hop when BinariesAutoUpdate is off, and EnsureCliReady is wired to this control's Loaded
            // event, so RunAsync starts out on the UI thread. IsExistingCliUsableForFallback launches
            // the CLI and waits up to ProtocolProbeTimeoutMs for it, which froze VS for the whole
            // timeout for anyone with auto-update disabled and a CLI already on disk. File.Exists
            // against an unreachable UNC share is the same hazard for tens of seconds.
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await TaskScheduler.Default;

                var fallbackUsable = this.IsExistingCliUsableForFallback();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.EnsureServerOnConfiguredCli(
                    fallbackUsable,
                    stopIssued,
                    cliSettingsChanged: eventArgs?.CliSettingsChanged == true,
                    source: "CliDownloadDidNotComplete");

                if (fallbackUsable)
                {
                    this.DetermineInitScreen();
                }
                else
                {
                    this.messagePanel.Text = "Snyk CLI not found, or the one installed is not usable by this version of the extension. You can specify a path to a Snyk CLI executable from the settings.";
                }
            }).FireAndForget();
        }

        private void OnDownloadFailed(object sender, Exception e)
        {
            // Consumed synchronously, as in OnCliDownloadDidNotComplete.
            var stopIssued = this.ConsumeStoppedServerForDownload();

            // The hop and the probe are placed as in OnCliDownloadDidNotComplete. DownloadFailed is only ever
            // raised after the tasks service's own hop, so the UI thread cannot be the caller here today;
            // the hop is kept so the two handlers cannot drift, and so the probe stays off whichever
            // thread does raise it.
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await TaskScheduler.Default;

                var fallbackUsable = this.IsExistingCliUsableForFallback();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // A failure can only follow an attempted download, so DownloadStarted has already recorded
                // whether it stopped the server — no reasoning here about whether that stop has landed.
                this.EnsureServerOnConfiguredCli(fallbackUsable, stopIssued, cliSettingsChanged: false, source: "DownloadFailed");

                if (fallbackUsable)
                {
                    this.DetermineInitScreen();
                }
                else
                {
                    this.messagePanel.Text =
                    "Failed to download the Snyk CLI, and the CLI already installed is either missing or not usable by this version of the extension. You can specify a path to a Snyk CLI executable from the settings.";
                }
            }).FireAndForget();
        }

        /// <summary>What a download outcome has to do to leave the language server on the configured CLI.</summary>
        internal enum ServerAction
        {
            /// <summary>Already where it should be, or there is nothing runnable to put it on.</summary>
            None,

            /// <summary>Not running, so raising a start is enough.</summary>
            Start,

            /// <summary>Running the previous executable, so it has to be stopped first.</summary>
            Restart,
        }

        /// <summary>
        /// Decides what a finished, cancelled or failed CLI check has to do about the language server.
        /// <list type="bullet">
        /// <item><paramref name="stopIssued"/> — this control took the server down so the download could
        /// overwrite the binary. That stop is fire-and-forget, so it may not have landed: if the server
        /// still reads as running, restart rather than start, because Visual Studio discards a start for a
        /// server it considers started and the pending stop would then take it down for good.</item>
        /// <item><paramref name="cliSettingsChanged"/> — the user repointed the CLI, so a server still
        /// serving is on the executable it was launched with and cannot switch without a restart.</item>
        /// <item>Neither — this is startup. A server that is already serving is left alone; stopping it
        /// would discard a working session for no gain.</item>
        /// </list>
        /// <paramref name="cliUsable"/> gates everything: never launch a binary not shown to run.
        /// </summary>
        internal static ServerAction DecideServerAction(
            bool cliUsable, bool stopIssued, bool serverRunning, bool cliSettingsChanged)
        {
            if (!cliUsable)
            {
                return ServerAction.None;
            }

            if (stopIssued)
            {
                return serverRunning ? ServerAction.Restart : ServerAction.Start;
            }

            if (!serverRunning)
            {
                return ServerAction.Start;
            }

            return cliSettingsChanged ? ServerAction.Restart : ServerAction.None;
        }

        // Read-and-clear in one step. The stop belongs to the episode that just ended, so leaving it set
        // would tell the next check the server was already down.
        private bool ConsumeStoppedServerForDownload() =>
            Interlocked.Exchange(ref this.stoppedServerForDownload, 0) == 1;

        // Shared by all three download outcomes so they cannot drift.
        private void EnsureServerOnConfiguredCli(bool cliUsable, bool stopIssued, bool cliSettingsChanged, string source)
        {
            var serverRunning = LanguageClientHelper.IsLanguageServerReady();
            var action = DecideServerAction(cliUsable, stopIssued, serverRunning, cliSettingsChanged);

            Logger.Debug(
                "{Source}: cliUsable={CliUsable}, stopIssued={StopIssued}, serverRunning={Running}, cliSettingsChanged={SettingsChanged}; action={Action}",
                source,
                cliUsable,
                stopIssued,
                serverRunning,
                cliSettingsChanged,
                action);

            if (action == ServerAction.None)
            {
                return;
            }

            var languageClient = LanguageClientHelper.LanguageClientManager();
            if (languageClient == null)
            {
                Logger.Debug("{Source}: no language client manager yet; nothing to act on", source);
                return;
            }

            ThreadHelper.JoinableTaskFactory.RunAsync(async () => await (action == ServerAction.Restart
                ? languageClient.RestartServerAsync()
                : languageClient.StartServerAsync(true))).FireAndForget();
        }

        /// <summary>
        /// Whether the download that just failed or was cancelled left a CLI behind that can actually
        /// run. Presence alone is not enough: restarting the language server against a truncated or
        /// protocol-incompatible binary leaves the tool window on its loading state with nothing to
        /// act on.
        /// The shared tasks service lets the following restart reuse this check's verdict.
        /// </summary>
        private bool IsExistingCliUsableForFallback() =>
            tasksService.IsExistingCliUsable(SnykCli.GetCliFilePath(serviceProvider.Options.CliCustomPath));

        /// <summary>
        /// Show tool window.
        /// </summary>
        public void Show() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsWindowFrame windowFrame = (IVsWindowFrame)this.toolWindow.Frame;

            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        });

        /// <summary>
        /// Cancel current task by user request.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        public void CancelIfCancellationRequested(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }
        }

        /// <summary>
        /// Display main message.
        /// </summary>
        /// <param name="text">Main message text.</param>
        public void DisplayMainMessage(string text) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.messagePanel.Text = text;

            this.messagePanel.Visibility = Visibility.Visible;
            this.DescriptionPanel.Visibility = Visibility.Collapsed;
        });

        /// <summary>
        /// Hide main message.
        /// </summary>
        public void HideMainMessage() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.messagePanel.Text = string.Empty;

            this.messagePanel.Visibility = Visibility.Collapsed;
        });

        /// <summary>
        /// Reset the results view and transition state to RunScanState. The LS re-renders the
        /// HTML tree on the next scan; we clear the local issue count so the "Clean" command
        /// disables until then.
        /// </summary>
        public void Clean() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.TreeHtmlPanel.Reset();
            this.DetermineInitScreen();
        });

        /// <summary>
        /// Switch to main thread and update state of toolbar (commands).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task UpdateActionsStateAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await Task.WhenAll(
                SnykScanCommand.Instance.UpdateStateAsync(),
                SnykStopCurrentTaskCommand.Instance.UpdateStateAsync(),
                SnykCleanPanelCommand.Instance.UpdateStateAsync(),
                SnykOpenSettingsCommand.Instance.UpdateStateAsync());
        }

        public async Task UpdateScreenStateAsync()
        {
            await Task.Delay(200);
            this.DetermineInitScreen();
        }

        // On scan completion, surface the "select an issue" prompt in the right pane; the issue
        // tree itself is rendered by the LS via the $/snyk.treeView notification.
        private async Task OnOssScanningFinishedAsync()
        {
            this.context.TransitionTo(ScanResultsState.Instance);
            await this.UpdateActionsStateAsync();
        }

        private async Task OnSnykCodeScanningFinishedAsync()
        {
            this.context.TransitionTo(ScanResultsState.Instance);
            await this.UpdateActionsStateAsync();
        }

        private async Task OnIacScanningFinishedAsync()
        {
            this.context.TransitionTo(ScanResultsState.Instance);
            await this.UpdateActionsStateAsync();
        }

        private async Task OnTaskFinishedAsync() => await this.UpdateActionsStateAsync();


        /// <summary>
        /// Update progress bar.
        /// </summary>
        /// <param name="value">Progress bar value.</param>
        private async Task UpdateDownloadProgressAsync(int value)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.progressBar.Value = value;

            this.messagePanel.Text = $"Downloading latest Snyk CLI release {value}%...";

            this.messagePanel.Visibility = Visibility.Visible;
            this.DescriptionPanel.Visibility = Visibility.Collapsed;
        }

        private void FillHtmlPanel(string issueId, string product, string html)
        {
            var languageClientManager = LanguageClientHelper.LanguageClientManager();
            if (languageClientManager == null) return;
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                if (string.IsNullOrEmpty(html))
                {
                    try
                    {
                        html = await languageClientManager.InvokeGenerateIssueDescriptionAsync(issueId,
                            SnykVSPackage.Instance.DisposalToken);
                    }
                    catch
                    {
                        Logger.Error("couldn't load html for issue {0}", issueId);
                        html = string.Empty;
                    }
                }

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.DescriptionPanel.SetContent(html, product);
            });
        }

        private void StopButton_Click(object sender, RoutedEventArgs e) => SnykTasksService.Instance.CancelTasks();

        private void CleanButton_Click(object sender, RoutedEventArgs e) => this.context.TransitionTo(RunScanState.Instance);

        private void SnykToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.context.IsEmptyState())
            {
                this.DetermineInitScreen();
            }
        }

        /// <summary>
        /// If api token is valid it will show run scan screen. If api token is invalid it will show Welcome screen.
        /// </summary>
        private void DetermineInitScreen()
        {
            var options = this.serviceProvider.Options;

            if (!LanguageClientHelper.IsLanguageServerReady())
            {
                this.context.TransitionTo(InitializingState.Instance);
                return;
            }

            if (SnykTasksService.Instance.IsTaskRunning())
            {
                this.messagePanel.ShowScanningMessage();
                return;
            }

            // Gate the screen on authentication only. The LS enforces trust (trust_enabled=true) and
            // renders the untrusted-folder prompt in the HTML tree view, so an authenticated user with
            // an untrusted folder open lands on the tree view where that prompt is shown.
            if (options.ApiToken.IsValid())
            {
                this.context.TransitionTo(RunScanState.Instance);
                return;
            }

            this.context.TransitionTo(OverviewState.Instance);
        }

        // Disposes the child WebView2-hosting panels. ToolWindowPane (the base of
        // SnykToolWindow) automatically calls Dispose on its Content property when the
        // tool window is destroyed, so this runs at real teardown — not on transient
        // WPF Unloaded events that can fire during docking or theme changes.
        public void Dispose()
        {
            if (this.disposed) return;
            this.disposed = true;

            VSColorTheme.ThemeChanged -= this.OnVsThemeChanged;

            if (this.solutionService?.SolutionEvents != null)
            {
                this.solutionService.SolutionEvents.AfterCloseSolution -= this.OnAfterCloseSolution;
            }

            if (this.tasksService != null)
            {
                this.tasksService.SnykCodeScanningStarted -= this.OnSnykCodeScanningStarted;
                this.tasksService.SnykCodeScanError -= this.OnSnykCodeDisplayError;
                this.tasksService.OssScanningStarted -= this.OnOssScanningStarted;
                this.tasksService.IacScanningStarted -= this.OnIacScanningStarted;
                this.tasksService.IacScanError -= this.OnIacScanError;
                this.tasksService.ScanningCancelled -= this.OnScanningCancelled;
                this.tasksService.CliDownloadDeclined -= this.OnCliDownloadDidNotComplete;
                this.tasksService.CliDownloadAborted -= this.OnCliDownloadDidNotComplete;
                this.tasksService.DownloadFailed -= this.OnDownloadFailed;

                if (this.codeScanningFinishedHandler != null) this.tasksService.SnykCodeScanningFinished -= this.codeScanningFinishedHandler;
                if (this.ossScanErrorHandler != null) this.tasksService.OssScanError -= this.ossScanErrorHandler;
                if (this.ossScanningFinishedHandler != null) this.tasksService.OssScanningFinished -= this.ossScanningFinishedHandler;
                if (this.iacScanningFinishedHandler != null) this.tasksService.IacScanningFinished -= this.iacScanningFinishedHandler;
                if (this.taskFinishedHandler != null) this.tasksService.TaskFinished -= this.taskFinishedHandler;
                if (this.downloadStartedHandler != null) this.tasksService.DownloadStarted -= this.downloadStartedHandler;
                if (this.downloadFinishedHandler != null) this.tasksService.DownloadFinished -= this.downloadFinishedHandler;
                if (this.downloadUpdateHandler != null) this.tasksService.DownloadUpdate -= this.downloadUpdateHandler;
            }

            if (this.loadedHandler != null) this.Loaded -= this.loadedHandler;

            // Detach the scan-command callback from the static singleton, but only if it is still
            // ours — a newer tool window may have replaced it, and we must not clobber that.
            if (this.updateControlsStateCallback != null
                && SnykScanCommand.Instance != null
                && ReferenceEquals(SnykScanCommand.Instance.UpdateControlsStateCallback, this.updateControlsStateCallback))
            {
                SnykScanCommand.Instance.UpdateControlsStateCallback = null;
            }

            // OnLanguageServerReadyAsync fires RequestInitialTree on the tree panel; without this
            // detach a post-teardown LS restart would touch the disposed panel. Detach only the
            // instance we actually subscribed (captured in this.languageClientManager). Re-resolving
            // via LanguageClientHelper could return a different instance after an LS restart and
            // detach a handler we never attached, leaving ours live on the original instance. If the
            // field was null at subscribe time we never subscribed, so this guard is a correct no-op.
            if (this.languageClientManager != null)
                this.languageClientManager.OnLanguageServerReadyAsync -= OnOnLanguageServerReadyAsync;

            this.DescriptionPanel?.Dispose();
            this.SummaryPanel?.Dispose();
            this.TreeHtmlPanel?.Dispose();
        }
    }
}