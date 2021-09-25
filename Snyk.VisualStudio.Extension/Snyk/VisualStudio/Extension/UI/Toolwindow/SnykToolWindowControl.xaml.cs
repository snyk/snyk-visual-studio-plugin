namespace Snyk.VisualStudio.Extension.UI.Toolwindow
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
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.CLI;
    using Snyk.VisualStudio.Extension.Service;
    using Snyk.VisualStudio.Extension.Settings;
    using Snyk.VisualStudio.Extension.Theme;
    using Snyk.VisualStudio.Extension.UI.Notifications;
    using Snyk.VisualStudio.Extension.UI.Tree;

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
        /// Initialize event listeners for UI.
        /// </summary>
        /// <param name="serviceProvider">Service provider implementation.</param>
        public void InitializeEventListeners(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.messagePanel.ServiceProvider = serviceProvider;

            Logger.Information("Enter InitializeEventListenersAsync() method.");

            Logger.Information("Initialize Solultion Event Listeners");

            SnykSolutionService solutionService = serviceProvider.SolutionService;

            solutionService.SolutionEvents.AfterCloseSolution += this.OnAfterCloseSolution;

            Logger.Information("Initialize CLI Event Listeners");

            SnykTasksService tasksService = serviceProvider.TasksService;

            tasksService.OssScanError += this.OnCliDisplayError;
            tasksService.SnykCodeScanError += this.OnSnykCodeDisplayError;
            tasksService.SnykCodeDisabled += this.OnSnykCodeDisabledHandler;
            tasksService.ScanningCancelled += this.OnScanningCancelled;
            tasksService.CliScanningStarted += this.OnCliScanningStarted;
            tasksService.SnykCodeScanningStarted += this.OnSnykCodeScanningStarted;
            tasksService.CliScanningUpdate += this.OnCliScanningUpdate;
            tasksService.SnykCodeScanningUpdate += this.OnSnykCodeScanningUpdate;
            //tasksService.ScanningFinished += this.OnScanningFinished;

            Logger.Information("Initialize Download Event Listeners");

            tasksService.DownloadStarted += this.OnDownloadStarted;
            tasksService.DownloadFinished += this.OnDownloadFinished;
            tasksService.DownloadUpdate += this.OnDownloadUpdate;
            tasksService.DownloadCancelled += this.OnDownloadCancelled;

            this.Loaded += tasksService.OnUiLoaded;

            serviceProvider.VsThemeService.ThemeChanged += this.OnVsThemeChanged;

            serviceProvider.Options.SettingsChanged += this.OnSettingsChanged;

            Logger.Information("Leave InitializeEventListenersAsync() method.");
        }

        /// <summary>
        /// AfterBackgroundSolutionLoadComplete event handler. Switch context to RunScanState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnAfterBackgroundSolutionLoadComplete(object sender, EventArgs eventArgs) => this.context.TransitionTo(RunScanState.Instance);

        /// <summary>
        /// AfterCloseSolution event handler. Switch context to RunScanState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnAfterCloseSolution(object sender, EventArgs eventArgs) => this.context.TransitionTo(RunScanState.Instance);

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

            this.messagePanel.ScanningMessage();

            this.mainGrid.Visibility = Visibility.Visible;

            this.resultsTree.CliRootNode.State = RootTreeNodeState.Scanning;
        });

        /// <summary>
        /// SnykCode ScanningStarted event handler. Switch context to ScanningState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnSnykCodeScanningStarted(object sender, SnykCodeScanEventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.messagePanel.ScanningMessage();

            this.resultsTree.CodeSequrityRootNode.State = RootTreeNodeState.Scanning;
            this.resultsTree.CodeQualityRootNode.State = RootTreeNodeState.Scanning;
        });

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
        public void OnCliDisplayError(object sender, SnykCliScanEventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.resultsTree.CliRootNode.State = RootTreeNodeState.Error;

            NotificationService.Instance.ShowWarningInfoBar(eventArgs.Error.Message);
        });

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

            this.resultsTree.CodeQualityRootNode.State = RootTreeNodeState.Error;
            this.resultsTree.CodeSequrityRootNode.State = RootTreeNodeState.Error;

            NotificationService.Instance.ShowWarningInfoBar(eventArgs.Error);
        });

        /// <summary>
        /// Handle SnykCode disabled.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnSnykCodeDisabledHandler(object sender, SnykCodeScanEventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.resultsTree.CodeQualityRootNode.State = RootTreeNodeState.DisabledForOrganization;
            this.resultsTree.CodeSequrityRootNode.State = RootTreeNodeState.DisabledForOrganization;
        });

        /// <summary>
        /// ScanningCancelled event handler. Switch context to ScanResultsState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnScanningCancelled(object sender, SnykCliScanEventArgs eventArgs) => this.context.TransitionTo(RunScanState.Instance);

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
        public void OnDownloadFinished(object sender, SnykCliDownloadEventArgs eventArgs) => this.SetInitialState();

        /// <summary>
        /// DownloadUpdate event handler. Call UpdateDonwloadProgress() method.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnDownloadUpdate(object sender, SnykCliDownloadEventArgs eventArgs) => this.UpdateDownloadProgress(eventArgs.Progress);

        /// <summary>
        /// DownloadCancelled event handler. Call SetInitialState() method.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnDownloadCancelled(object sender, SnykCliDownloadEventArgs eventArgs) => this.SetInitialState();

        /// <summary>
        /// VsThemeChanged event handler. Call Adapt methods for <see cref="HtmlRichTextBox"/> controls.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnVsThemeChanged(object sender, SnykVsThemeChangedEventArgs eventArgs)
        {
            //this.overview.AdaptForeground(); todo: fix theme cheged for description panel.
        }

        /// <summary>
        /// Show tool window.
        /// </summary>
        public void ShowToolWindow() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsWindowFrame windowFrame = (IVsWindowFrame)toolWindow.Frame;

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

        private void OnSettingsChanged(object sender, SnykSettingsChangedEventArgs e) => this.UpdateTreeNodeItemsState();

        private void UpdateTreeNodeItemsState() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var options = this.serviceProvider.Options;

            this.resultsTree.CliRootNode.State = options.OssEnabled? RootTreeNodeState.Enabled : RootTreeNodeState.Disabled;

            if (!await this.serviceProvider.ApiService.IsSnykCodeEnabledAsync())
            {
                this.resultsTree.CodeQualityRootNode.State = RootTreeNodeState.DisabledForOrganization;
                this.resultsTree.CodeSequrityRootNode.State = RootTreeNodeState.DisabledForOrganization;
            }
            else
            {
                this.resultsTree.CodeQualityRootNode.State = options.SnykCodeQualityEnabled ? RootTreeNodeState.Enabled : RootTreeNodeState.Disabled;
                this.resultsTree.CodeSequrityRootNode.State = options.SnykCodeSecurityEnabled ? RootTreeNodeState.Enabled : RootTreeNodeState.Disabled;
            }
        });

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

                this.serviceProvider.AnalyticsService.LogOpenSourceAnalysisReadyEvent(
                    cliResult.CriticalSeverityCount,
                    cliResult.HighSeverityCount,
                    cliResult.MediumSeverityCount,
                    cliResult.LowSeverityCount);
            });
        }

        private void AppendSnykCodeResultToTree(AnalysisResult analysisResult)
        {
            this.context.TransitionTo(ScanResultsState.Instance);

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.resultsTree.AnalysisResults = analysisResult;

                // TODO: Add SnykCode analytics event.
                // this.serviceProvider.AnalyticsService.LogOpenSourceAnalysisReadyEvent(
                //    cliResult.CriticalSeverityCount,
                //    cliResult.HighSeverityCount,
                //    cliResult.MediumSeverityCount,
                //    cliResult.LowSeverityCount);
            });
        }

        /// <summary>
        /// Update progress bar.
        /// </summary>
        /// <param name="value">Progress bar value.</param>
        private void UpdateDownloadProgress(int value) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.progressBar.Value = value;

            this.messagePanel.Text = $"Downloading latest Snyk CLI release {value}%...";

            this.messagePanel.Visibility = Visibility.Visible;
            this.descriptionPanel.Visibility = Visibility.Collapsed;
        });

        private void VulnerabilitiesTree_SelectetVulnerabilityChanged(object sender, RoutedEventArgs args)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.messagePanel.Visibility = Visibility.Collapsed;

                TreeNode treeNode = null;

                if (this.resultsTree.SelectedItem is OssVulnerabilityTreeNode)
                {
                    var ossTreeNode = this.resultsTree.SelectedItem as OssVulnerabilityTreeNode;

                    var vulnerability = ossTreeNode.Vulnerability;

                    if (vulnerability != null)
                    {
                        treeNode = ossTreeNode;

                        this.descriptionPanel.Visibility = Visibility.Visible;

                        this.descriptionPanel.Vulnerability = vulnerability;

                        this.serviceProvider.AnalyticsService.LogUserSeesAnIssueEvent(vulnerability.Id, vulnerability.Severity);
                    }
                    else
                    {
                        this.descriptionPanel.Visibility = Visibility.Collapsed;
                    }
                }

                if (this.resultsTree.SelectedItem is SnykCodeVulnerabilityTreeNode)
                {
                    this.descriptionPanel.Visibility = Visibility.Visible;

                    var snykCodeTreeNode = this.resultsTree.SelectedItem as SnykCodeVulnerabilityTreeNode;

                    this.descriptionPanel.Suggestion = snykCodeTreeNode.Suggestion;

                    treeNode = snykCodeTreeNode;

                    var suggestion = snykCodeTreeNode.Suggestion;

                    VsCodeService.Instance.OpenAndNavigate(
                        this.serviceProvider.SolutionService.GetFileFullPath(suggestion.FileName),
                        suggestion.Rows.Item1 - 1,
                        suggestion.Columns.Item1 - 1,
                        suggestion.Rows.Item2 - 1,
                        suggestion.Columns.Item2);
                }

                if (treeNode == null)
                {
                    this.descriptionPanel.Visibility = Visibility.Collapsed;

                    this.messagePanel.Visibility = Visibility.Visible;
                    this.messagePanel.ShowSelectIssueMessage();

                    return;
                }
            });
        }

        private void StopButton_Click(object sender, RoutedEventArgs e) => SnykTasksService.Instance.CancelCurrentTask();

        private void CleanButton_Click(object sender, RoutedEventArgs e) => this.context.TransitionTo(RunScanState.Instance);

        private void SnykToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.context.IsEmptyState())
            {
                this.SetInitialState();
            }

            this.UpdateTreeNodeItemsState();
        }

        private void SetInitialState()
        {
            this.serviceProvider.AnalyticsService.AnalyticsEnabled = this.serviceProvider.Options.UsageAnalyticsEnabled;

            if (string.IsNullOrEmpty(this.serviceProvider.GetApiToken()))
            {
                this.context.TransitionTo(OverviewState.Instance);

                this.serviceProvider.AnalyticsService.LogUserLandedOnTheWelcomePageEvent();
            }
            else
            {
                this.context.TransitionTo(RunScanState.Instance);
            }
        }
    }
}