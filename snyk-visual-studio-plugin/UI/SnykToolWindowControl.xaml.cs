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
        private SnykToolWindow toolWindow;

        private ISnykServiceProvider serviceProvider;

        private ToolWindowContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykToolWindowControl"/> class.
        /// </summary>
        public SnykToolWindowControl(SnykToolWindow toolWindow)
        {
            this.toolWindow = toolWindow;

            this.InitializeComponent();

            this.context = new ToolWindowContext(this, RunScanState.Instance);
        }        

        public void InitializeEventListeners(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            SnykActivityLogger logger = serviceProvider.ActivityLogger;

            logger.LogInformation("Enter InitializeEventListenersAsync() method.");

            logger.LogInformation("Initialize Solultion Event Listeners");            

            SnykSolutionService solutionService = serviceProvider.SolutionService;

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
            context.TransitionTo(RunScanState.Instance);
        }
         
        public void OnAfterCloseSolution(object sender, EventArgs eventArgs)
        {
            context.TransitionTo(RunScanState.Instance);
        }

        public void OnScanningUpdate(object sender, SnykCliScanEventArgs eventArgs) => AppendCliResultToTree(eventArgs.Result);

        public void OnScanningStarted(object sender, SnykCliScanEventArgs eventArgs) => context.TransitionTo(ScanningState.Instance);

        public void OnScanningFinished(object sender, SnykCliScanEventArgs eventArgs) => context.TransitionTo(ScanResultsState.Instance);            
        
        public void OnDisplayError(object sender, SnykCliScanEventArgs eventArgs) => context.TransitionTo(ErrorState.Instance(eventArgs.Error));

        public void OnScanningCancelled(object sender, SnykCliScanEventArgs eventArgs) => context.TransitionTo(RunScanState.Instance);

        public void OnDownloadStarted(object sender, SnykCliDownloadEventArgs eventArgs) => context.TransitionTo(DownloadState.Instance);

        public void OnDownloadFinished(object sender, SnykCliDownloadEventArgs eventArgs) => SetInitialState();

        public void OnDownloadUpdate(object sender, SnykCliDownloadEventArgs eventArgs) => UpdateDownloadProgress(eventArgs.Progress);

        public void OnDownloadCancelled(object sender, SnykCliDownloadEventArgs eventArgs) => SetInitialState();

        public void OnVsThemeChanged(object sender, SnykVsThemeChangedEventArgs eventArgs)
        {
            errorMessage.AdaptForeground();
            errorPath.AdaptForeground();
            overview.AdaptForeground();
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

        public void HideRunScanMessage() => this.Dispatcher.Invoke(() => noVulnerabilitiesAddedMessageGrid.Visibility = Visibility.Collapsed);

        public void DisplayRunScanMessage() => this.Dispatcher.Invoke(() => noVulnerabilitiesAddedMessageGrid.Visibility = Visibility.Visible);

        public void DisplayMainMessage(string text)
        {
            this.Dispatcher.Invoke(() =>
            {
                message.Text = text;

                messageGrid.Visibility = Visibility.Visible;
            });
        }

        public void HideMainMessage()
        {
            this.Dispatcher.Invoke(() =>
            {
                message.Text = "";

                messageGrid.Visibility = Visibility.Collapsed;
            });
        }

        public void DisplayError(CliError cliError)
        {
            this.Dispatcher.Invoke(() =>
            {
                errorPanel.Visibility = Visibility.Visible;

                errorMessage.Html = cliError.Message;
                errorPath.Html = cliError.Path;
            });
        }

        public void EnableExecuteActions()
        {
            this.Dispatcher.Invoke(() =>
            {
                runButton.IsEnabled = true;
                cleanButton.IsEnabled = true;
                stopButton.IsEnabled = false;

                runScanLink.IsEnabled = true;
            });
        }

        public void EnableStopActions()
        {
            this.Dispatcher.Invoke(() =>
            {
                runButton.IsEnabled = false;
                cleanButton.IsEnabled = false;
                stopButton.IsEnabled = true;

                runScanLink.IsEnabled = false;
            });
        }

        public void DisableAllActions()
        {
            this.Dispatcher.Invoke(() =>
            {
                runButton.IsEnabled = false;
                cleanButton.IsEnabled = false;
                stopButton.IsEnabled = false;

                runScanLink.IsEnabled = false;
            });
        }

        private void AppendCliResultToTree(CliResult cliResult)
        {
            this.Dispatcher.Invoke(() =>
            {
                vulnerabilitiesTree.AppendVulnerabilities(cliResult);

                serviceProvider.AnalyticsService.LogOpenSourceAnalysisReadyEvent(
                    cliResult.HighSeverityCount, 
                    cliResult.MediumSeverityCount, 
                    cliResult.LowSeverityCount);
            });
        }

        private void UpdateDownloadProgress(int value)
        {
            this.Dispatcher.BeginInvoke((Action) (() =>
            {
                progressBar.Value = value;

                message.Text = $"Downloading latest Snyk CLI release {value}%...";
            }));
        }        

        public void CleanVulnerabilitiesTree() => this.Dispatcher.Invoke(() => vulnerabilitiesTree.Clear());

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

                        overview.Html = Markdig.Markdown.ToHtml(vulnerability.description);

                        moreAboutThisIssue.NavigateUri = new Uri(vulnerability.url);


                        serviceProvider.AnalyticsService.LogUserSeesAnIssueEvent(vulnerability.id, vulnerability.severity);
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

        public void CleanAndHideVulnerabilityDetailsPanel()
        {
            vulnerabilityDetailsPanel.Visibility = Visibility.Hidden;

            vulnerableModule.Text = "";
            introducedThrough.Text = "";
            exploitMaturity.Text = "";
            fixedIn.Text = "";
            detaiedIntroducedThrough.Text = "";
            remediation.Text = "";
            overview.Html = "";
            moreAboutThisIssue.NavigateUri = null;            
        }

        public void HideIssueMessages()
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
            context.TransitionTo(RunScanState.Instance);
        }

        private void SnykToolWindow_Loaded(object sender, RoutedEventArgs e)
        {            
            serviceProvider.TasksService.Download();
            
            SetInitialState();                        
        }

        private void SetInitialState()
        {
            serviceProvider.AnalyticsService.AnalyticsEnabled = serviceProvider.Options.UsageAnalyticsEnabled;

            if (string.IsNullOrEmpty(serviceProvider.GetApiToken()))
            {
                context.TransitionTo(OverviewState.Instance);

                serviceProvider.AnalyticsService.LogUserLandedOnTheWelcomePageEvent();
            }
            else
            {
                context.TransitionTo(RunScanState.Instance);
            }
        }

        private void connectToSnykLink_Click(object sender, RoutedEventArgs e)
        {
            Action<string> successCallbackAction = (apiToken) =>
            {
                Dispatcher.Invoke(() =>
                {
                    connectVSToSnykLink.IsEnabled = true;

                    connectVSToSnykProgressBar.Visibility = Visibility.Collapsed;
                });

                serviceProvider.Options.ApiToken = apiToken;

                context.TransitionTo(RunScanState.Instance);
            };

            Action<string> errorCallbackAction = (error) =>
            {
                Dispatcher.Invoke(() =>
                {
                    connectVSToSnykLink.IsEnabled = true;

                    connectVSToSnykProgressBar.Visibility = Visibility.Collapsed;
                });

                var cliError = new CliError()
                {
                    Message = error
                };

                context.TransitionTo(ErrorState.Instance(cliError));
            };

            Dispatcher.Invoke(() =>
            {
                connectVSToSnykLink.IsEnabled = false;

                connectVSToSnykProgressBar.Visibility = Visibility.Visible;
            });

            serviceProvider.Options.Authenticate(successCallbackAction, errorCallbackAction);
        }
    }

    class ToolWindowContext
    {
        private SnykToolWindowControl toolWindowControl;

        private ToolWindowState state = EmptyState.Instance;

        public ToolWindowContext(SnykToolWindowControl control, ToolWindowState state)
        {
            this.toolWindowControl = control;

            this.TransitionTo(state);
        }

        public SnykToolWindowControl ToolWindowControl
        {
            get
            {
                return toolWindowControl;
            }
        }

        public void TransitionTo(ToolWindowState state)
        {
            this.state.HideComponents();

            this.state = state;

            this.state.Context = this;

            this.state.DisplayComponents();
        }

        public void RequestUpdateUI()
        {
            this.state.DisplayComponents();
        }
    }

    abstract class ToolWindowState
    {
        public ToolWindowContext Context { get; set; }

        public SnykToolWindowControl ToolWindowControl
        {
            get
            {
                return Context.ToolWindowControl;
            }
        }

        public abstract void HideComponents();

        public abstract void DisplayComponents();
    }

    class RunScanState : ToolWindowState
    {
        public static RunScanState Instance
        {
            get
            {
                return new RunScanState();
            }
        }

        public override void HideComponents()
        {
            ToolWindowControl.HideRunScanMessage();

            ToolWindowControl.EnableStopActions();
        }

        public override void DisplayComponents()
        {
            ToolWindowControl.DisplayRunScanMessage();            

            ToolWindowControl.EnableExecuteActions();
        }
    }

    class ErrorState : ToolWindowState
    {
        private CliError cliError;
        public static ErrorState Instance(CliError cliError) => new ErrorState(cliError);

        public ErrorState(CliError cliError)
        {
            this.cliError = cliError;
        }

        public override void HideComponents()
        {
            ToolWindowControl.Dispatcher.Invoke(() => ToolWindowControl.errorPanel.Visibility = Visibility.Collapsed);
        }

        public override void DisplayComponents()
        {
            ToolWindowControl.EnableExecuteActions();

            ToolWindowControl.DisplayError(cliError);
        }
    }

    class DownloadState : ToolWindowState
    {
        public static DownloadState Instance
        {
            get
            {
                return new DownloadState();
            }
        }

        public override void HideComponents()
        {
            ToolWindowControl.HideMainMessage();

            ToolWindowControl.Dispatcher.Invoke(() =>
            {
                ToolWindowControl.progressBar.Value = 0;

                ToolWindowControl.progressBarPanel.Visibility = Visibility.Collapsed;                
            });
        }

        public override void DisplayComponents()
        {
            ToolWindowControl.Dispatcher.Invoke(() =>
            {
                ToolWindowControl.progressBar.Value = 0;

                ToolWindowControl.progressBarPanel.Visibility = Visibility.Visible;                
            });

            ToolWindowControl.DisplayMainMessage("Downloading latest Snyk CLI release 0%...");
        }
    }

    class ScanningState : ToolWindowState
    {
        public static ScanningState Instance
        {
            get
            {
                return new ScanningState();
            }
        }

        public override void HideComponents()
        {
            ToolWindowControl.HideMainMessage();

            ToolWindowControl.Dispatcher.Invoke(() =>
            {               
                ToolWindowControl.progressBar.Value = 0;
                ToolWindowControl.progressBar.IsIndeterminate = false;

                ToolWindowControl.progressBarPanel.Visibility = Visibility.Collapsed;

                ToolWindowControl.stopButton.IsEnabled = false;
            });
        }

        public override void DisplayComponents()
        {
            ToolWindowControl.DisplayMainMessage("Scanning project for vulnerabilities...");

            ToolWindowControl.Dispatcher.Invoke(() =>
            {               
                ToolWindowControl.progressBar.Value = 0;
                ToolWindowControl.progressBar.IsIndeterminate = true;

                ToolWindowControl.progressBarPanel.Visibility = Visibility.Visible;

                ToolWindowControl.stopButton.IsEnabled = true;
            });
        }
    }

    class ScanResultsState : ToolWindowState
    {
        public static ScanResultsState Instance
        {
            get
            {
                return new ScanResultsState();
            }
        }

        public override void DisplayComponents()
        {            
            ToolWindowControl.Dispatcher.Invoke(() =>
            {
                ToolWindowControl.EnableExecuteActions();

                ToolWindowControl.selectIssueMessageGrid.Visibility = Visibility.Visible;

                ToolWindowControl.resultsGrid.Visibility = Visibility.Visible;
            });            
        }

        public override void HideComponents()
        {
            ToolWindowControl.CleanVulnerabilitiesTree();

            ToolWindowControl.Dispatcher.Invoke(() =>
            {
                ToolWindowControl.resultsGrid.Visibility = Visibility.Collapsed;

                ToolWindowControl.HideIssueMessages();

                ToolWindowControl.CleanAndHideVulnerabilityDetailsPanel();                
            });
        }
    }

    class OverviewState : ToolWindowState
    {
        public static OverviewState Instance
        {
            get
            {
                return new OverviewState();
            }
        }

        public override void DisplayComponents()
        {
            ToolWindowControl.Dispatcher.Invoke(() =>
            {
                ToolWindowControl.overviewGrid.Visibility = Visibility.Visible;
            });

            ToolWindowControl.DisableAllActions();
        }

        public override void HideComponents()
        {
            ToolWindowControl.Dispatcher.Invoke(() =>
            {
                ToolWindowControl.overviewGrid.Visibility = Visibility.Collapsed;
            });
        }
    }
    class EmptyState : ToolWindowState
    {
        public static EmptyState Instance
        {
            get
            {
                return new EmptyState();
            }
        }

        public override void DisplayComponents()
        {            
        }

        public override void HideComponents()
        {
        }
    }
}