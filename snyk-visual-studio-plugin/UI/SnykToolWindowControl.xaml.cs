//------------------------------------------------------------------------------
// <copyright file="SnykToolWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Snyk.VisualStudio.Extension.UI
{
    using CLI;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Threading;
    using System.Threading;
    using System.Windows.Media;
    using System;
    using Theme;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio;
    using Task = System.Threading.Tasks.Task;
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Interaction logic for SnykToolWindowControl.
    /// </summary>
    public partial class SnykToolWindowControl : UserControl
    {
        private bool isSolutionLoaded = true;

        private SnykToolWindow toolWindow;

        private ISnykServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykToolWindowControl"/> class.
        /// </summary>
        public SnykToolWindowControl(SnykToolWindow toolWindow)
        {
            this.toolWindow = toolWindow;

            this.InitializeComponent();

            //DisableAllActions();
        }

        public async Task InitializeEventListenersAsync(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            SnykActivityLogger logger = serviceProvider.ActivityLogger;

            logger.LogInformation("Enter InitializeEventListenersAsync() method.");

            logger.LogInformation("Initialize Solultion Event Listeners");

            await serviceProvider.Package.JoinableTaskFactory.SwitchToMainThreadAsync();

            SnykSolutionService solutionService = serviceProvider.SolutionService;

            //solutionService.SolutionEvents.AfterBackgroundSolutionLoadComplete += OnAfterBackgroundSolutionLoadComplete;
            solutionService.SolutionEvents.AfterCloseSolution += OnAfterCloseSolution;

            logger.LogInformation("Initialize CLI Event Listeners");

            SnykTasksService tasksService = serviceProvider.TasksService;

            tasksService.ScanError += OnDisplayError;
            tasksService.ScanningCancelled += OnScanningCancelled;
            tasksService.ScanningStarted += OnScanningStarted;
            tasksService.ScanningUpdate += OnScanningUpdate;
            tasksService.ScanningFinished += OnScanningFinished;

            logger.LogInformation("Initialize Download Event Listeners");

            tasksService.DownloadStarted += OnDownloadStarted;
            tasksService.DownloadFinished += OnDownloadFinished;
            tasksService.DownloadUpdate += OnDownloadUpdate;
            tasksService.DownloadCancelled += OnDownloadCancelled;

            serviceProvider.VsThemeService.ThemeChanged += OnVsThemeChanged;

            logger.LogInformation("Leave InitializeEventListenersAsync() method.");
        }

        public void OnAfterBackgroundSolutionLoadComplete(object sender, EventArgs eventArgs)
        {
            HideAllControls();

            EnableExecuteActions();

            isSolutionLoaded = true;
        }

        public void OnAfterCloseSolution(object sender, EventArgs eventArgs)
        {
            HideAllControls();

            //DisableAllActions();

            isSolutionLoaded = false;
        }

        public void OnScanningUpdate(object sender, SnykCliScanEventArgs eventArgs) => AppendCliResultToTree(eventArgs.Result);

        public void OnScanningStarted(object sender, SnykCliScanEventArgs eventArgs)
        {
            DisplayMainPanelMessage("Scanning project for vulnerabilities...");

            EnableStopActions();

            HideError();

            ShowIndeterminateProgressBar();

            CleanVulnerabilitiesTree();

            HideRunScanMessage();
        }

        public void OnScanningFinished(object sender, SnykCliScanEventArgs eventArgs)
        {
            EnableExecuteActions();

            HideProgressBar();

            HideMainPanelMessage();
        }

        public void OnDisplayError(object sender, SnykCliScanEventArgs eventArgs)
        {
            EnableExecuteActions();

            DisplayError(eventArgs.Error);

            HideMainPanelMessage();
        }

        public void OnScanningCancelled(object sender, SnykCliScanEventArgs eventArgs)
        {
            EnableExecuteActions();

            HideAllControls();

            HideMainPanelMessage();

            DisplayRunScanMessage();
        }

        public void OnDownloadStarted(object sender, SnykCliDownloadEventArgs eventArgs)
        {
            HideRunScanMessage();

            EnableStopActions();

            ShowProgressBar();
        }

        public void OnDownloadFinished(object sender, SnykCliDownloadEventArgs eventArgs)
        {
            SetupAvailableActions();
                        
            HideAllControls();

            HideMainPanelMessage();

            DisplayRunScanMessage();
        }

        public void OnDownloadUpdate(object sender, SnykCliDownloadEventArgs eventArgs) => UpdateProgressBar(eventArgs.Progress);

        public void OnDownloadCancelled(object sender, SnykCliDownloadEventArgs eventArgs)
        {
            //DisableAllActions();

            HideAllControls();

            HideMainPanelMessage();
        }

        public void OnVsThemeChanged(object sender, SnykVsThemeChangedEventArgs eventArgs)
        {
            errorMessage.SetupForeground();
            errorPath.SetupForeground();
            overview.SetupForeground();
        }

        public void ShowToolWindow()
        {
            this.Dispatcher.Invoke(() =>
            {
                IVsWindowFrame windowFrame = (IVsWindowFrame)toolWindow.Frame;

                ErrorHandler.ThrowOnFailure(windowFrame.Show());
            });            
        }

        public SnykFilterableTree VulnerabilitiesTree
        {
            get
            {
                return vulnerabilitiesTree;
            }
        }

        public void CancelIfCancellationRequested(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }
        }

        private void HideRunScanMessage() => this.Dispatcher.Invoke(() => noVulnerabilitiesAddedMessageGrid.Visibility = Visibility.Collapsed);

        private void DisplayRunScanMessage() => this.Dispatcher.Invoke(() => noVulnerabilitiesAddedMessageGrid.Visibility = Visibility.Visible);

        private void DisplayMainPanelMessage(string text)
        {
            this.Dispatcher.Invoke(() =>
            {
                message.Text = text;

                messageGrid.Visibility = Visibility.Visible;
            });
        }

        private void HideMainPanelMessage()
        {
            this.Dispatcher.Invoke(() =>
            {
                message.Text = "";

                messageGrid.Visibility = Visibility.Visible;
            });
        }

        public void DisplayError(CliError cliError)
        {
            this.Dispatcher.Invoke(() =>
            {
                progressBarPanel.Visibility = Visibility.Collapsed;
                resultsGrid.Visibility = Visibility.Collapsed;

                errorPanel.Visibility = Visibility.Visible;

                errorMessage.RichText = cliError.Message;
                errorPath.RichText = cliError.Path;
            });
        }

        private void SetupAvailableActions()
        {
            if (isSolutionLoaded)
            {
                EnableExecuteActions();
            }
            else
            {
                //DisableAllActions();
            }
        }

        private void EnableExecuteActions()
        {
            this.Dispatcher.Invoke(() =>
            {
                runButton.IsEnabled = true;
                cleanButton.IsEnabled = true;
                stopButton.IsEnabled = false;

                runScanLink.IsEnabled = true;
            });
        }

        private void DisableAllActions()
        {
            this.Dispatcher.Invoke(() =>
            {
                runButton.IsEnabled = false;
                cleanButton.IsEnabled = false;
                stopButton.IsEnabled = false;

                runScanLink.IsEnabled = false;
            });
        }

        private void EnableStopActions()
        {
            this.Dispatcher.Invoke(() =>
            {
                runButton.IsEnabled = false;
                cleanButton.IsEnabled = false;
                stopButton.IsEnabled = true;

                runScanLink.IsEnabled = false;
            });
        }

        private void AppendCliResultToTree(CliResult cliResult)
        {
            this.Dispatcher.Invoke(() =>
            {
                DisplayResultsGrid();

                vulnerabilitiesTree.AppendVulnerabilities(cliResult.CLIVulnerabilities);
            });
        }

        private void HideProgressBar()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.progressBarPanel.Visibility = Visibility.Collapsed;

                this.progressBar.IsIndeterminate = false;
            });
        }

        private void ShowProgressBar()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.progressBarPanel.Visibility = Visibility.Visible;

                this.resultsGrid.Visibility = Visibility.Collapsed;
            });
        }        

        private void ShowIndeterminateProgressBar()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.progressBarPanel.Visibility = Visibility.Visible;
                                
                this.progressBar.IsIndeterminate = true;

                this.resultsGrid.Visibility = Visibility.Collapsed;
            });
        }

        private void UpdateProgressBar(int value)
        {
            this.Dispatcher.BeginInvoke((Action) (() =>
            {
                progressBar.Value = value;

                DisplayMainPanelMessage($"Downloading latest Snyk CLI release {value}%...");
            }));
        }

        private void HideAllControls()
        {
            this.Dispatcher.Invoke(() =>
            {
                progressBarPanel.Visibility = Visibility.Collapsed;
                resultsGrid.Visibility = Visibility.Collapsed;

                errorPanel.Visibility = Visibility.Collapsed;

                HideError();
                CleanAndHideVulnerabilityDetailsPanel();
            });
        }

        private void DisplayResultsGrid() => resultsGrid.Visibility = Visibility.Visible;

        private void HideError() => this.Dispatcher.Invoke(() => errorPanel.Visibility = Visibility.Collapsed);

        private void CleanVulnerabilitiesTree() => this.Dispatcher.Invoke(() => vulnerabilitiesTree.Clear());

        private void OnHyperlinkClick(object sender, RoutedEventArgs e)
        {
            var destination = ((Hyperlink) e.OriginalSource).NavigateUri;

            using (Process browser = new Process())
            {
                browser.StartInfo = new ProcessStartInfo
                {
                    FileName = destination.ToString(),
                    UseShellExecute = true,
                    ErrorDialog = true
                };

                browser.Start();
            }
        }

        private void vulnerabilitiesTree_SelectetVulnerabilityChanged(object sender, RoutedEventArgs args)
        {            
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
                {
                    HideIssueMessages();

                    var treeNode = vulnerabilitiesTree.SelectedItem as VulnerabilityTreeNode;

                    if (treeNode == null)
                    {
                        return;
                    }

                    if (treeNode.Vulnerability != null)
                    {                        
                        vulnerabilityDetailsPanel.Visibility = Visibility.Visible;

                        var vulnerability = treeNode.Vulnerability;

                        SetupSeverity(vulnerability);

                        vulnerableModule.Text = vulnerability.name;

                        string introducedThroughText = vulnerability.from != null && vulnerability.from.Length != 0 
                                    ? string.Join(", ", vulnerability.from) : "";

                        introducedThrough.Text = introducedThroughText;
                        exploitMaturity.Text = vulnerability.exploit;
                        fixedIn.Text = String.IsNullOrWhiteSpace(vulnerability.Remediation) 
                            ? $"There is no fixed version for {vulnerability.name}" : vulnerability.Remediation;

                        string detaiedIntroducedThroughText = vulnerability.from != null && vulnerability.from.Length != 0
                                    ? string.Join(" > ", vulnerability.from) : "";

                        detaiedIntroducedThrough.Text = detaiedIntroducedThroughText;

                        remediation.Text = vulnerability.fixedIn != null && vulnerability.fixedIn.Length != 0
                                                 ? "Upgrade to " + string.Join(" > ", vulnerability.fixedIn) : "";
                      
                        overview.RichText = vulnerability.Overview;

                        moreAboutThisIssue.NavigateUri = new System.Uri(vulnerability.url);
                    }
                    else
                    {
                        CleanAndHideVulnerabilityDetailsPanel();                        

                        if (treeNode.Items.Count > 0)
                        {
                            selectIssueMessageGrid.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            noIssuesMessageGrid.Visibility = Visibility.Visible;
                        }                                      
                    }                    
                }
            ); 
        }

        private void CleanAndHideVulnerabilityDetailsPanel()
        {
            vulnerabilityDetailsPanel.Visibility = Visibility.Hidden;

            vulnerableModule.Text = "";
            introducedThrough.Text = "";
            exploitMaturity.Text = "";
            fixedIn.Text = "";
            detaiedIntroducedThrough.Text = "";
            remediation.Text = "";
            overview.RichText = "";
            moreAboutThisIssue.NavigateUri = null;            
        }

        private void HideIssueMessages()
        {
            selectIssueMessageGrid.Visibility = Visibility.Collapsed;
            noIssuesMessageGrid.Visibility = Visibility.Collapsed;
        }

        private void SetupSeverity(Vulnerability vulnerability)
        {
            Color severityColor;
            string severityText;

            switch (vulnerability.severity)
            {
                case Severity.High:
                    severityColor = (Color)ColorConverter.ConvertFromString("#C75450");
                    severityText = "High severity";

                    break;
                case Severity.Medium:
                    severityColor = (Color)ColorConverter.ConvertFromString("#EDA200");
                    severityText = "Medium severity";

                    break;
                case Severity.Low:
                    severityColor = (Color)ColorConverter.ConvertFromString("#6E6E6E");
                    severityText = "Low severity";

                    break;
                default:
                    severityColor = Colors.Transparent;
                    severityText = "";

                    break;
            }

            severityBorder.Background = new SolidColorBrush(severityColor);
            severityBorder.BorderBrush = new SolidColorBrush(severityColor);

            severity.Background = new SolidColorBrush(severityColor);
            severity.Text = severityText;
        }

        private void moreAboutThisIssue_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs args)
        {
            Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri));

            args.Handled = true;
        }        

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            SnykTasksService.Instance.Scan();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            SnykTasksService.Instance.CancelCurrentTask();
        }

        private void cleanButton_Click(object sender, RoutedEventArgs e)
        {
            CleanVulnerabilitiesTree();

            HideAllControls();

            DisplayRunScanMessage();
        }

        private void SnykToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            serviceProvider.TasksService.Download();
        }
    }
}