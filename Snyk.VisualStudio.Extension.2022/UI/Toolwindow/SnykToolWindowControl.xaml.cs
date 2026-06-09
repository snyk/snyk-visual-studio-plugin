using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Commands;
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
    public partial class SnykToolWindowControl : UserControl, IDisposable
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykToolWindowControl>();

        private SnykToolWindow toolWindow;

        private ISnykServiceProvider serviceProvider;

        private ToolWindowContext context;

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
            this.TreeHtmlPanel.Init();
            this.messagePanel.Context = this.context;
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

            var solutionService = serviceProvider.SolutionService as SnykSolutionService;

            solutionService.SolutionEvents.AfterCloseSolution += this.OnAfterCloseSolution;

            Logger.Information("Initialize CLI Event Listeners");

            var tasksService = serviceProvider.TasksService;

            // Scan results are rendered by the LS-driven HTML tree (the $/snyk.treeView
            // notification), so the per-product ScanningUpdate/ScanningDisabled handlers that
            // populated the old native tree are gone. We still react to Started/Error/Finished to
            // drive the right-pane message panel, toolbar command state, and error notifications.
            tasksService.SnykCodeScanningStarted += this.OnSnykCodeScanningStarted;
            tasksService.SnykCodeScanError += this.OnSnykCodeDisplayError;
            tasksService.SnykCodeScanningFinished += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(this.OnSnykCodeScanningFinishedAsync);

            tasksService.OssScanningStarted += this.OnOssScanningStarted;
            tasksService.OssScanError += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(() => this.OnOssDisplayErrorAsync(sender, args));
            tasksService.OssScanningFinished += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(this.OnOssScanningFinishedAsync);

            tasksService.IacScanningStarted += OnIacScanningStarted;
            tasksService.IacScanError += OnIacScanError;
            tasksService.IacScanningFinished += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(this.OnIacScanningFinishedAsync);


            tasksService.ScanningCancelled += this.OnScanningCancelled;
            tasksService.TaskFinished += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(this.OnTaskFinishedAsync);

            Logger.Information("Initialize Download Event Listeners");

            tasksService.DownloadStarted += (sender, args) =>
            {
                if (LanguageClientHelper.IsLanguageServerReady())
                    ThreadHelper.JoinableTaskFactory.RunAsync(serviceProvider.LanguageClientManager.StopServerAsync).FireAndForget();
                this.OnDownloadStarted(sender, args);
            };
            tasksService.DownloadFinished += (sender, args) =>
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async ()=> await serviceProvider.LanguageClientManager.StartServerAsync(true)).FireAndForget();
                this.OnDownloadFinished(sender, args);
            };
            tasksService.DownloadUpdate += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(() => this.OnDownloadUpdateAsync(sender, args));
            tasksService.DownloadCancelled += this.OnDownloadCancelled;
            tasksService.DownloadFailed += this.OnDownloadFailed;
            LanguageClientHelper.LanguageClientManager().OnLanguageServerReadyAsync += OnOnLanguageServerReadyAsync;
            this.Loaded += (sender, args) => tasksService.Download();

            SnykScanCommand.Instance.UpdateControlsStateCallback = (isEnabled) => ThreadHelper.JoinableTaskFactory.Run(async () =>
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
            ThreadHelper.JoinableTaskFactory.Run(async () =>
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
            });
        }

        /// <summary>
        /// DownloadFinished event handler. Call SetInitialState() method.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnDownloadFinished(object sender, SnykCliDownloadEventArgs eventArgs) => this.DetermineInitScreen();

        /// <summary>
        /// DownloadUpdate event handler. Call UpdateDonwloadProgress() method.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnDownloadUpdateAsync(object sender, SnykCliDownloadEventArgs eventArgs) => await this.UpdateDownloadProgressAsync(eventArgs.Progress);

        /// <summary>
        /// DownloadCancelled event handler. Call SetInitialState() method.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnDownloadCancelled(object sender, SnykCliDownloadEventArgs eventArgs)
        {
            if (SnykCli.IsCliFileFound(serviceProvider.Options.CliCustomPath))
            {
                if (LanguageClientHelper.LanguageClientManager() != null)
                    ThreadHelper.JoinableTaskFactory.RunAsync(async () => await LanguageClientHelper.LanguageClientManager().RestartServerAsync()).FireAndForget();
                
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    this.DetermineInitScreen();
                });
            }
            else
            {
                this.messagePanel.Text = "Snyk CLI not found. You can specify a path to a Snyk CLI executable from the settings.";
            }
        }

        private void OnDownloadFailed(object sender, Exception e)
        {
            if (SnykCli.IsCliFileFound(serviceProvider.Options.CliCustomPath))
            {
                if (LanguageClientHelper.LanguageClientManager() != null)
                    ThreadHelper.JoinableTaskFactory.RunAsync(async () => await LanguageClientHelper.LanguageClientManager().RestartServerAsync()).FireAndForget();
                this.DetermineInitScreen();
            }
            else
            {
                this.messagePanel.Text =
                "Failed to download Snyk CLI. You can specify a path to a Snyk CLI executable from the settings.";
            }
        }

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

            this.TreeHtmlPanel.TotalIssues = 0;
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

            var isFolderTrusted = ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                var solutionFolderPath = await this.serviceProvider.SolutionService.GetSolutionFolderAsync();
                var isFolderTrusted = this.serviceProvider.WorkspaceTrustService.IsFolderTrusted(solutionFolderPath);
                return isFolderTrusted;
            });

            if (options.ApiToken.IsValid() && isFolderTrusted)
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
            this.DescriptionPanel?.Dispose();
            this.SummaryPanel?.Dispose();
            this.TreeHtmlPanel?.Dispose();
        }
    }
}