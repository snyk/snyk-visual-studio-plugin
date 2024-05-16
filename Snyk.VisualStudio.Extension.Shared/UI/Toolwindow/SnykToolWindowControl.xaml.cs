namespace Snyk.VisualStudio.Extension.Shared.UI.Toolwindow
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Serilog;
    using Snyk.Analytics;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Common;
    using Snyk.Common.Service;
    using Snyk.Common.Settings;
    using Snyk.VisualStudio.Extension.Shared.CLI;
    using Snyk.VisualStudio.Extension.Shared.Commands;
    using Snyk.VisualStudio.Extension.Shared.Model;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Snyk.VisualStudio.Extension.Shared.Settings;
    using Snyk.VisualStudio.Extension.Shared.Theme;
    using Snyk.VisualStudio.Extension.Shared.UI.Notifications;
    using Snyk.VisualStudio.Extension.Shared.UI.Tree;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Interaction logic for SnykToolWindowControl.
    /// </summary>
    public partial class SnykToolWindowControl : UserControl
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

            this.messagePanel.Context = this.context;
        }

        /// <summary>
        /// Gets a value indicating whether VulnerabilitiesTree.
        /// </summary>
        public SnykFilterableTree VulnerabilitiesTree => this.resultsTree;

        /// <summary>
        /// Gets a value indicating whether tree content not empty.
        /// </summary>
        /// <returns>True if result tree not empty.</returns>
        public bool IsTreeContentNotEmpty() => this.resultsTree.CliRootNode.HasContent
            || this.resultsTree.CodeSecurityRootNode.HasContent
            || this.resultsTree.CodeQualityRootNode.HasContent;

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

            SnykTasksService tasksService = serviceProvider.TasksService;

            tasksService.OssScanError += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(() => this.OnCliDisplayErrorAsync(sender, args));
            tasksService.SnykCodeScanError += this.OnSnykCodeDisplayError;
            tasksService.SnykCodeDisabled += this.OnSnykCodeDisabledHandler;
            tasksService.ScanningCancelled += this.OnScanningCancelled;
            tasksService.CliScanningStarted += this.OnCliScanningStarted;
            tasksService.OssScanningDisabled += this.OnOssScanningDisabled;
            tasksService.SnykCodeScanningStarted += this.OnSnykCodeScanningStarted;
            tasksService.OssScanningUpdate += this.OnCliScanningUpdate;
            tasksService.SnykCodeScanningUpdate += this.OnSnykCodeScanningUpdate;
            tasksService.SnykCodeScanningFinished += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(this.OnSnykCodeScanningFinishedAsync);
            tasksService.OssScanningFinished += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(this.OnOssScanningFinishedAsync);
            tasksService.TaskFinished += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(this.OnTaskFinishedAsync);

            Logger.Information("Initialize Download Event Listeners");

            tasksService.DownloadStarted += this.OnDownloadStarted;
            tasksService.DownloadFinished += this.OnDownloadFinished;
            tasksService.DownloadUpdate += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(() => this.OnDownloadUpdateAsync(sender, args));
            tasksService.DownloadCancelled += this.OnDownloadCancelled;
            tasksService.DownloadFailed += this.OnDownloadFailed;

            this.Loaded += (sender, args) => tasksService.Download();

            serviceProvider.Options.SettingsChanged += this.OnSettingsChanged;

            SnykScanCommand.Instance.UpdateControlsStateCallback = (isEnabled) => ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.messagePanel.runScanButton.IsEnabled = isEnabled;

                // When the run button is disabled, we need to show the welcome screen that prompts the user to authenticate.
                if (!isEnabled)
                {
                    this.context.TransitionTo(OverviewState.Instance);
                }
            });

            Logger.Information("Leave InitializeEventListenersAsync() method.");
        }

        private void OnOssScanningDisabled(object sender, SnykCliScanEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();


                this.resultsTree.CliRootNode.State = RootTreeNodeState.Disabled;
                this.resultsTree.CliRootNode.Clean();
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
        /// Scanning update event handler. Append CLI results to tree.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnCliScanningUpdate(object sender, SnykCliScanEventArgs eventArgs) => this.AppendCliResultToTree(eventArgs.Result);

        /// <summary>
        /// Scanning update event handler. Append CLI results to tree.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnSnykCodeScanningUpdate(object sender, SnykCodeScanEventArgs eventArgs) => this.AppendSnykCodeResultToTree(eventArgs.Result);

        /// <summary>
        /// Cli ScanningStarted event handler..
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnCliScanningStarted(object sender, SnykCliScanEventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.messagePanel.ShowScanningMessage();
            this.Show();
            this.mainGrid.Visibility = Visibility.Visible;

            this.resultsTree.CliRootNode.State = RootTreeNodeState.Scanning;

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

            SetScanNodeState(resultsTree.CodeSecurityRootNode, eventArgs.CodeScanEnabled);
            SetScanNodeState(resultsTree.CodeQualityRootNode, eventArgs.QualityScanEnabled);

            await this.UpdateActionsStateAsync();
        });

        private void SetScanNodeState(RootTreeNode node, bool isEnabled)
        {
            if (node == null) 
                return;
            if (!isEnabled)
            {
                node.State = RootTreeNodeState.Disabled;
                node.Clean();
                return;
            }
            node.State = RootTreeNodeState.Scanning;
        }

        /// <summary>
        /// ScanningFinished event handler. Switch context to ScanResultsState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnScanningFinished(object sender, SnykCliScanEventArgs eventArgs) => this.context.TransitionTo(ScanResultsState.Instance);

        /// <summary>
        /// Handle Cli error.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnCliDisplayErrorAsync(object sender, SnykCliScanEventArgs eventArgs)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.serviceProvider.AnalyticsService.LogAnalysisReadyEvent(AnalysisType.SnykOpenSource, AnalyticsAnalysisResult.Error);

            this.resultsTree.CliRootNode.State = RootTreeNodeState.Error;

            NotificationService.Instance.ShowErrorInfoBar(eventArgs.Error.Message);

            if (eventArgs.FeaturesSettings != null && !eventArgs.FeaturesSettings.CodeSecurityEnabled && !eventArgs.FeaturesSettings.CodeQualityEnabled)
            {
                this.context.TransitionTo(RunScanState.Instance);
            }

            await this.UpdateActionsStateAsync();
        }

        /// <summary>
        /// Initialize tool window control.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider instance.</param>
        public void Initialize(ISnykServiceProvider serviceProvider) => this.UpdateTreeNodeItemsState();

        /// <summary>
        /// Handle SnykCode error.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnSnykCodeDisplayError(object sender, SnykCodeScanEventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.serviceProvider.AnalyticsService.LogAnalysisReadyEvent(AnalysisType.SnykCodeSecurity, AnalyticsAnalysisResult.Error);

            this.resultsTree.CodeQualityRootNode.State = RootTreeNodeState.Error;
            this.resultsTree.CodeSecurityRootNode.State = RootTreeNodeState.Error;

            NotificationService.Instance.ShowErrorInfoBar(eventArgs.Error);

            if (!this.serviceProvider.Options.OssEnabled)
            {
                this.context.TransitionTo(RunScanState.Instance);
            }

            await this.UpdateActionsStateAsync();
        });

        /// <summary>
        /// Handle SnykCode disabled.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnSnykCodeDisabledHandler(object sender, SnykCodeScanEventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var disabledNodeState = RootTreeNodeState.DisabledForOrganization;

            this.resultsTree.CodeQualityRootNode.State = disabledNodeState;
            this.resultsTree.CodeSecurityRootNode.State = disabledNodeState;
        });

        /// <summary>
        /// ScanningCancelled event handler. Switch context to ScanResultsState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnScanningCancelled(object sender, SnykCliScanEventArgs eventArgs)
        {
            this.context.TransitionTo(RunScanState.Instance);

            this.UpdateTreeNodeItemsState();
        }

        /// <summary>
        /// DownloadStarted event handler. Switch context to DownloadState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnDownloadStarted(object sender, SnykCliDownloadEventArgs eventArgs)
        {
            if (eventArgs.IsUpdateDownload)
            {
                this.context.TransitionTo(UpdateDownloadState.Instance);
            }
            else
            {
                this.context.TransitionTo(DownloadState.Instance);
            }
        }

        /// <summary>
        /// DownloadFinished event handler. Call SetInitialState() method.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnDownloadFinished(object sender, SnykCliDownloadEventArgs eventArgs) => this.ShowWelcomeOrRunScanScreen();

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
            var snykCli = this.serviceProvider.NewCli();
            if (snykCli.IsCliFileFound())
            {
                this.ShowWelcomeOrRunScanScreen();
            }
            else
            {
                this.messagePanel.Text = "Snyk CLI not found. You can specify a path to a Snyk CLI executable from the settings.";
            }
        }

        private void OnDownloadFailed(object sender, Exception e)
        {
            var snykCli = this.serviceProvider.NewCli();
            if (snykCli.IsCliFileFound())
            {
                this.ShowWelcomeOrRunScanScreen();
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
            this.descriptionPanel.Visibility = Visibility.Collapsed;
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
        /// Clean vulnerability tree and transition state to RunScanState.
        /// </summary>
        public void Clean() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.resultsTree.Clear();

            this.UpdateTreeNodeItemsState();

            this.context.TransitionTo(RunScanState.Instance);
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
            this.ShowWelcomeOrRunScanScreen();
        }

        private async Task OnOssScanningFinishedAsync() => await this.UpdateActionsStateAsync();

        private async Task OnSnykCodeScanningFinishedAsync() => await this.UpdateActionsStateAsync();

        private void OnSettingsChanged(object sender, SnykSettingsChangedEventArgs e) => this.UpdateTreeNodeItemsState();

        private async Task OnTaskFinishedAsync() => await this.UpdateActionsStateAsync();

        private void UpdateTreeNodeItemsState() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var options = this.serviceProvider.Options;

            this.resultsTree.CliRootNode.State = this.GetOssRootTreeNodeState(options);

            try
            {
                var sastSettings = await this.serviceProvider.ApiService.GetSastSettingsAsync();

                this.resultsTree.CodeQualityRootNode.State = this.GetSnykCodeRootNodeState(sastSettings, options.SnykCodeQualityEnabled);
                this.resultsTree.CodeSecurityRootNode.State = this.GetSnykCodeRootNodeState(sastSettings, options.SnykCodeSecurityEnabled);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error on get sast settings and display tree node state");

                this.resultsTree.CodeQualityRootNode.State = RootTreeNodeState.Error;
                this.resultsTree.CodeSecurityRootNode.State = RootTreeNodeState.Error;

                NotificationService.Instance.ShowErrorInfoBar(e.Message);
            }
        });

        private RootTreeNodeState GetOssRootTreeNodeState(ISnykOptions options) =>
            options.ApiToken.IsValid() && options.OssEnabled ? RootTreeNodeState.Enabled : RootTreeNodeState.Disabled;

        private RootTreeNodeState GetSnykCodeRootNodeState(SastSettings sastSettings, bool enabledInOptions)
        {
            if (sastSettings == null)
            {
                return RootTreeNodeState.Disabled;
            }

            if (!sastSettings.SastEnabled)
            {
                return RootTreeNodeState.DisabledForOrganization;
            }

            return enabledInOptions ? RootTreeNodeState.Enabled : RootTreeNodeState.Disabled;
        }

        /// <summary>
        /// On link click handler. It open provided link.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="e">Event args.</param>
        private void OnHyperlinkClick(object sender, RoutedEventArgs e)
        {
            var destination = ((Hyperlink)e.OriginalSource).NavigateUri;

            using (Process browser = new Process())
            {
                browser.StartInfo = new ProcessStartInfo
                {
                    FileName = destination.ToString(),
                    UseShellExecute = true,
                    ErrorDialog = true,
                };

                browser.Start();
            }
        }

        /// <summary>
        /// Append CLI results to tree.
        /// </summary>
        /// <param name="cliResult">CLI result.</param>
        private void AppendCliResultToTree(CliResult cliResult)
        {
            if (cliResult.CliVulnerabilitiesList == null)
            {
                return;
            }

            this.context.TransitionTo(ScanResultsState.Instance);

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.resultsTree.OssResult = cliResult;

                this.serviceProvider.AnalyticsService
                    .LogAnalysisReadyEvent(AnalysisType.SnykOpenSource, AnalyticsAnalysisResult.Success);
            });
        }

        private void AppendSnykCodeResultToTree(AnalysisResult analysisResult)
        {
            this.context.TransitionTo(ScanResultsState.Instance);

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (analysisResult != null)
                {
                    this.resultsTree.AnalysisResults = analysisResult;
                }
                else
                {
                    this.resultsTree.CodeQualityRootNode.State = RootTreeNodeState.NoFilesForSnykCodeScan;
                    this.resultsTree.CodeSecurityRootNode.State = RootTreeNodeState.NoFilesForSnykCodeScan;
                }
            });
        }

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
            this.descriptionPanel.Visibility = Visibility.Collapsed;
        }

        private void VulnerabilitiesTree_SelectetVulnerabilityChanged(object sender, RoutedEventArgs args)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.messagePanel.Visibility = Visibility.Collapsed;

                TreeNode treeNode = null;

                if (this.resultsTree.SelectedItem is OssVulnerabilityTreeNode)
                {
                    this.HandleOssTreeNodeSelected();

                    return;
                }

                if (this.resultsTree.SelectedItem is SnykCodeVulnerabilityTreeNode)
                {
                    await this.HandleSnykCodeTreeNodeSelectedAsync();

                    return;
                }

                this.HandleRootTreeNodeSelected();
            });
        }

        private void HandleRootTreeNodeSelected()
        {
            this.descriptionPanel.Visibility = Visibility.Collapsed;
            this.messagePanel.Visibility = Visibility.Visible;

            var selectedItem = this.resultsTree.SelectedItem;

            // Check if selected tree node is related to Snyk Code and if state is LocalCodeEngineIsEnabled.
            // In this case display additional informaiton in toolwindow panel.
            if (selectedItem is SnykCodeQualityRootTreeNode || selectedItem is SnykCodeSecurityRootTreeNode)
            {
                var rootTreeNode = selectedItem as RootTreeNode;
            }

            this.messagePanel.ShowSelectIssueMessage();
        }

        private void HandleOssTreeNodeSelected()
        {
            var ossTreeNode = this.resultsTree.SelectedItem as OssVulnerabilityTreeNode;

            var vulnerability = ossTreeNode.Vulnerability;

            if (vulnerability != null)
            {
                this.descriptionPanel.Visibility = Visibility.Visible;

                this.descriptionPanel.Vulnerability = vulnerability;

                var issueType = vulnerability.IsLicense() ? ScanResultIssueType.LicenceIssue : ScanResultIssueType.OpenSourceVulnerability;
                this.serviceProvider.AnalyticsService.LogIssueIsViewedEvent(
                    vulnerability.Id,
                    issueType,
                    vulnerability.Severity);
            }
            else
            {
                this.descriptionPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async Task HandleSnykCodeTreeNodeSelectedAsync()
        {
            this.descriptionPanel.Visibility = Visibility.Visible;

            var snykCodeTreeNode = this.resultsTree.SelectedItem as SnykCodeVulnerabilityTreeNode;

            await this.descriptionPanel.SetSuggestionAsync(snykCodeTreeNode.Suggestion);

            var suggestion = snykCodeTreeNode.Suggestion;

            VsCodeService.Instance.OpenAndNavigate(
                await this.serviceProvider.SolutionService.GetFileFullPathAsync(suggestion.FileName),
                suggestion.Rows.Item1 - 1,
                suggestion.Columns.Item1 - 1,
                suggestion.Rows.Item2 - 1,
                suggestion.Columns.Item2);

            var issueType = suggestion.Categories.Contains("Security") ? ScanResultIssueType.CodeSecurityVulnerability : ScanResultIssueType.CodeQualityIssue;
            this.serviceProvider.AnalyticsService.LogIssueIsViewedEvent(
                    suggestion.Id,
                    issueType,
                    Severity.FromInt(suggestion.Severity));
        }

        private void StopButton_Click(object sender, RoutedEventArgs e) => SnykTasksService.Instance.CancelTasks();

        private void CleanButton_Click(object sender, RoutedEventArgs e) => this.context.TransitionTo(RunScanState.Instance);

        private void SnykToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.context.IsEmptyState())
            {
                this.ShowWelcomeOrRunScanScreen();
            }

            this.UpdateTreeNodeItemsState();
        }

        /// <summary>
        /// If api token is valid it will show run scan screen. If api token is invalid it will show Welcome screen.
        /// </summary>
        private void ShowWelcomeOrRunScanScreen()
        {
            var options = this.serviceProvider.Options;
            this.serviceProvider.AnalyticsService.AnalyticsEnabledOption = 
                options.UsageAnalyticsEnabled && options.IsAnalyticsPermitted();

            if (options.ApiToken.IsValid())
            {
                this.context.TransitionTo(RunScanState.Instance);

                return;
            }

            this.context.TransitionTo(OverviewState.Instance);

            this.serviceProvider.AnalyticsService.LogWelcomeIsViewedEvent();
        }
    }
}