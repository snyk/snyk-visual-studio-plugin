namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;
    using System.Windows.Threading;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;
    using Theme;
    using CLI;
    using Service;
    using UI.Tree;
    using Task = System.Threading.Tasks.Task;
    using Snyk.Code.Library.Domain.Analysis;

    /// <summary>
    /// Interaction logic for SnykToolWindowControl.
    /// </summary>
    public partial class SnykToolWindowControl : UserControl
    {
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
        }

        /// <summary>
        /// Gets a value indicating whether VulnerabilitiesTree.
        /// </summary>
        public SnykFilterableTree VulnerabilitiesTree => this.vulnerabilitiesTree;

        /// <summary>
        /// Initialize event listeners for UI.
        /// </summary>
        /// <param name="serviceProvider">Service provider implementation.</param>
        public void InitializeEventListeners(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            SnykActivityLogger logger = serviceProvider.ActivityLogger;

            logger.LogInformation("Enter InitializeEventListenersAsync() method.");

            logger.LogInformation("Initialize Solultion Event Listeners");

            SnykSolutionService solutionService = serviceProvider.SolutionService;

            solutionService.SolutionEvents.AfterCloseSolution += this.OnAfterCloseSolution;

            logger.LogInformation("Initialize CLI Event Listeners");

            SnykTasksService tasksService = serviceProvider.TasksService;

            tasksService.ScanError += this.OnDisplayError;
            tasksService.ScanningCancelled += this.OnScanningCancelled;
            tasksService.ScanningStarted += this.OnScanningStarted;
            tasksService.CliScanningUpdate += this.OnScanningUpdate;
            tasksService.SnykCodeScanningUpdate += this.OnSnykCodeScanningUpdate;
            tasksService.ScanningFinished += this.OnScanningFinished;

            logger.LogInformation("Initialize Download Event Listeners");

            tasksService.DownloadStarted += this.OnDownloadStarted;
            tasksService.DownloadFinished += this.OnDownloadFinished;
            tasksService.DownloadUpdate += this.OnDownloadUpdate;
            tasksService.DownloadCancelled += this.OnDownloadCancelled;

            this.Loaded += tasksService.OnUiLoaded;

            serviceProvider.VsThemeService.ThemeChanged += this.OnVsThemeChanged;

            logger.LogInformation("Leave InitializeEventListenersAsync() method.");
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
        public void OnScanningUpdate(object sender, SnykCliScanEventArgs eventArgs) => this.AppendCliResultToTree(eventArgs.Result);

        /// <summary>
        /// Scanning update event handler. Append CLI results to tree.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnSnykCodeScanningUpdate(object sender, SnykCodeScanEventArgs eventArgs) => this.AppendSnykCodeResultToTree(eventArgs.Result);

        /// <summary>
        /// ScanningStarted event handler. Switch context to ScanningState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnScanningStarted(object sender, SnykCliScanEventArgs eventArgs) => this.context.TransitionTo(ScanningState.Instance);

        /// <summary>
        /// ScanningFinished event handler. Switch context to ScanResultsState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnScanningFinished(object sender, SnykCliScanEventArgs eventArgs) => this.context.TransitionTo(ScanResultsState.Instance);

        /// <summary>
        /// DisplayError event handler. Switch context to ErrorState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnDisplayError(object sender, SnykCliScanEventArgs eventArgs) => this.context.TransitionTo(ErrorState.Instance(eventArgs.Error));

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
            this.errorMessage.AdaptForeground();
            this.errorPath.AdaptForeground();
            this.overview.AdaptForeground();
        }

        /// <summary>
        /// Show tool window.
        /// </summary>
        public void ShowToolWindow()
        {
            this.Dispatcher.Invoke(() =>
            {
                IVsWindowFrame windowFrame = (IVsWindowFrame)toolWindow.Frame;

                ErrorHandler.ThrowOnFailure(windowFrame.Show());
            });
        }

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
        /// Hide run scan message.
        /// </summary>
        public void HideRunScanMessage() => this.Dispatcher.Invoke(() => this.noVulnerabilitiesAddedMessageGrid.Visibility = Visibility.Collapsed);

        /// <summary>
        /// Display run scan message.
        /// </summary>
        public void DisplayRunScanMessage() => this.Dispatcher.Invoke(() => this.noVulnerabilitiesAddedMessageGrid.Visibility = Visibility.Visible);

        /// <summary>
        /// Display main message.
        /// </summary>
        /// <param name="text">Main message text.</param>
        public void DisplayMainMessage(string text)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.message.Text = text;

                this.messageGrid.Visibility = Visibility.Visible;
            });
        }

        /// <summary>
        /// Hide main message.
        /// </summary>
        public void HideMainMessage()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.message.Text = string.Empty;

                this.messageGrid.Visibility = Visibility.Collapsed;
            });
        }

        /// <summary>
        /// Display error message.
        /// </summary>
        /// <param name="cliError"><see cref="CliError"/> object.</param>
        public void DisplayError(CliError cliError)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.errorPanel.Visibility = Visibility.Visible;

                this.errorMessage.Html = cliError.Message;
                this.errorPath.Html = cliError.Path;
            });
        }

        /// <summary>
        /// Enable execute actions.
        /// </summary>
        public void EnableExecuteActions()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.runScanLink.IsEnabled = true;
            });
        }

        /// <summary>
        /// Enable stop action buttons.
        /// </summary>
        public void EnableStopActions() => this.Dispatcher.Invoke(() => this.runScanLink.IsEnabled = false);

        /// <summary>
        /// Disable all action buttons.
        /// </summary>
        public void DisableAllActions() => this.Dispatcher.Invoke(() => this.runScanLink.IsEnabled = false);

        /// <summary>
        /// Clear vulnerability tree elements.
        /// </summary>
        public void CleanVulnerabilitiesTree() => this.Dispatcher.Invoke(() => this.vulnerabilitiesTree.Clear());

        /// <summary>
        /// Clean and hide vulnerability details panel.
        /// </summary>
        public void CleanAndHideVulnerabilityDetailsPanel()
        {
            this.vulnerabilityDetailsPanel.Visibility = Visibility.Hidden;

            this.vulnerableModule.Text = string.Empty;
            this.introducedThrough.Text = string.Empty;
            this.exploitMaturity.Text = string.Empty;
            this.fixedIn.Text = string.Empty;
            this.detaiedIntroducedThrough.Text = string.Empty;
            this.remediation.Text = string.Empty;
            this.overview.Html = string.Empty;
            this.moreAboutThisIssue.NavigateUri = null;
        }

        /// <summary>
        /// Clean vulnerability tree and transition state to RunScanState.
        /// </summary>
        public void Clean()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.vulnerabilitiesTree.Clear();

                this.context.TransitionTo(RunScanState.Instance);
            });
        }

        /// <summary>
        /// Hide issues messages.
        /// </summary>
        public void HideIssueMessages()
        {
            this.selectIssueMessageGrid.Visibility = Visibility.Collapsed;
            this.noIssuesMessageGrid.Visibility = Visibility.Collapsed;
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

            this.Dispatcher.Invoke(() =>
            {
                this.vulnerabilitiesTree.AppendVulnerabilities(cliResult);

                this.serviceProvider.AnalyticsService.LogOpenSourceAnalysisReadyEvent(
                    cliResult.CriticalSeverityCount,
                    cliResult.HighSeverityCount,
                    cliResult.MediumSeverityCount,
                    cliResult.LowSeverityCount);
            });
        }

        private void AppendSnykCodeResultToTree(AnalysisResult analysisResult)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.vulnerabilitiesTree.AppendVulnerabilities(analysisResult);

                //this.serviceProvider.AnalyticsService.LogOpenSourceAnalysisReadyEvent(
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
        private void UpdateDownloadProgress(int value)
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                this.progressBar.Value = value;

                this.message.Text = $"Downloading latest Snyk CLI release {value}%...";
            }));
        }

        private void VulnerabilitiesTree_SelectetVulnerabilityChanged(object sender, RoutedEventArgs args)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                this.HideIssueMessages();

                var treeNode = this.vulnerabilitiesTree.SelectedItem as VulnerabilityTreeNode;

                if (treeNode == null)
                {
                    return;
                }

                if (treeNode.Vulnerability != null)
                {
                    this.vulnerabilityDetailsPanel.Visibility = Visibility.Visible;

                    var vulnerability = treeNode.Vulnerability;

                    this.SetupSeverity(vulnerability);

                    this.vulnerableModule.Text = vulnerability.Name;

                    string introducedThroughText = vulnerability.From != null && vulnerability.From.Length != 0
                                ? string.Join(", ", vulnerability.From) : "";

                    this.introducedThrough.Text = introducedThroughText;
                    this.exploitMaturity.Text = vulnerability.Exploit;
                    this.fixedIn.Text = String.IsNullOrWhiteSpace(vulnerability.Remediation)
                        ? $"There is no fixed version for {vulnerability.Name}" : vulnerability.Remediation;

                    string detaiedIntroducedThroughText = vulnerability.From != null && vulnerability.From.Length != 0
                                ? string.Join(" > ", vulnerability.From) : "";

                    this.detaiedIntroducedThrough.Text = detaiedIntroducedThroughText;

                    this.remediation.Text = vulnerability.FixedIn != null && vulnerability.FixedIn.Length != 0
                                             ? "Upgrade to " + string.Join(" > ", vulnerability.FixedIn) : "";

                    this.overview.Html = Markdig.Markdown.ToHtml(vulnerability.Description);

                    this.moreAboutThisIssue.NavigateUri = new Uri(vulnerability.Url);

                    this.serviceProvider.AnalyticsService.LogUserSeesAnIssueEvent(vulnerability.Id, vulnerability.Severity);
                }
                else
                {
                    this.CleanAndHideVulnerabilityDetailsPanel();

                    if (treeNode.Items.Count > 0)
                    {
                        this.selectIssueMessageGrid.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        this.noIssuesMessageGrid.Visibility = Visibility.Visible;
                    }
                }
            });
        }

        private void SetupSeverity(Vulnerability vulnerability)
        {
            var severityFilter = SeverityFilter.ByName(vulnerability.Severity);

            string severityText = severityFilter.Title;
            Color severityColor = severityFilter.Color;

            this.severityBorder.Background = new SolidColorBrush(severityColor);
            this.severityBorder.BorderBrush = new SolidColorBrush(severityColor);

            this.severity.Background = new SolidColorBrush(severityColor);
            this.severity.Text = severityText;
        }

        private void MoreAboutThisIssue_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs args)
        {
            Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri));

            args.Handled = true;
        }

        private void RunButton_Click(object sender, RoutedEventArgs e) => SnykTasksService.Instance.Scan();

        private void StopButton_Click(object sender, RoutedEventArgs e) => SnykTasksService.Instance.CancelCurrentTask();

        private void CleanButton_Click(object sender, RoutedEventArgs e) => this.context.TransitionTo(RunScanState.Instance);

        private void SnykToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.context.IsEmptyState())
            {
                this.SetInitialState();
            }
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

        private void ConnectToSnykLink_Click(object sender, RoutedEventArgs e)
        {
            Action<string> successCallbackAction = (apiToken) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.connectVSToSnykLink.IsEnabled = true;

                    this.connectVSToSnykProgressBar.Visibility = Visibility.Collapsed;
                });

                this.serviceProvider.Options.ApiToken = apiToken;

                this.context.TransitionTo(RunScanState.Instance);
            };

            Action<string> errorCallbackAction = (error) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.connectVSToSnykLink.IsEnabled = true;

                    this.connectVSToSnykProgressBar.Visibility = Visibility.Collapsed;
                });

                this.context.TransitionTo(OverviewState.Instance);
            };

            this.Dispatcher.Invoke(() =>
            {
                this.connectVSToSnykLink.IsEnabled = false;

                this.connectVSToSnykProgressBar.Visibility = Visibility.Visible;
            });

            this.serviceProvider.Options.Authenticate(successCallbackAction, errorCallbackAction);
        }
    }
}