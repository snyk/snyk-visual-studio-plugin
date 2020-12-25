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
    using System.Collections.ObjectModel;
    using System.Windows.Threading;
    using System.Threading;
    using System.Windows.Media;
    using System.Windows.Data;
    using System;
    using System.Globalization;
    using System.Windows.Media.Imaging;
    
    /// <summary>
    /// Interaction logic for SnykToolWindowControl.
    /// </summary>
    public partial class SnykToolWindowControl : UserControl
    {
        private static SnykToolWindowControl instance;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykToolWindowControl"/> class.
        /// </summary>
        public SnykToolWindowControl()
        {
            this.InitializeComponent();

            instance = this;
        }

        public void OnScanningUpdate(object sender, SnykCliScanEventArgs eventArgs) => AppendCliResultToTree(eventArgs.Result);

        public void OnScanningStarted(object sender, SnykCliScanEventArgs eventArgs)
        {
            HideError();
            ShowIndeterminateProgressBar("Scanning...");

            CleanVulnerabilitiesTree();
        }        

        public void OnOnScanningFinished(object sender, SnykCliScanEventArgs eventArgs) => HideProgressBar();

        public void OnDisplayError(object sender, SnykCliScanEventArgs eventArgs) => DisplayError(eventArgs.Error);

        public void OnScanningCancelled(object sender, SnykCliScanEventArgs eventArgs) => HideAllControls();

        public void OnDownloadStarted(object sender, SnykCliDownloadEventArgs eventArgs) => ShowProgressBar("Downloading latest Snyk CLI release...");

        public void OnDownloadFinished(object sender, SnykCliDownloadEventArgs eventArgs) => HideAllControls();

        public void OnDownloadUpdate(object sender, SnykCliDownloadEventArgs eventArgs) => UpdateProgressBar(eventArgs.Progress);

        public void OnDownloadCancelled(object sender, SnykCliDownloadEventArgs eventArgs) => HideAllControls();
        
        public SnykVSPackage Package { get; internal set; }        

        public void CancelIfCancellationRequested(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }
        }

        public void DisplayError(CliError cliError)
        {
            this.Dispatcher.Invoke(() =>
            {
                progressBarPanel.Visibility = Visibility.Collapsed;
                resultsGrid.Visibility = Visibility.Collapsed;

                errorPanel.Visibility = Visibility.Visible;

                errorMessage.Text = cliError.Message;
                errorPath.Text = cliError.Path;
            });
        }

        private void AppendCliResultToTree(CliResult cliResult)
        {
            this.Dispatcher.Invoke(() =>
            {
                DisplayResultsGrid();

                foreach (CliVulnerabilities cliVulnerabilities in cliResult.CLIVulnerabilities)
                {
                    var rootNode = new VulnerabilityTreeNode
                    {
                        CliVulnerabilities = cliVulnerabilities
                    };

                    Array.Sort(cliVulnerabilities.vulnerabilities);

                    foreach (Vulnerability vulnerability in cliVulnerabilities.vulnerabilities)
                    {
                        var node = new VulnerabilityTreeNode
                        {
                            Vulnerability = vulnerability
                        };

                        rootNode.Items.Add(node);
                    }

                    vulnerabilitiesTree.Items.Add(rootNode);
                }
            });
        }

        private void HideProgressBar()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.progressBarPanel.Visibility = Visibility.Collapsed;

                this.progressBarTitle.Text = "";

                this.progressBar.IsIndeterminate = false;
                this.progressBarPercent.Visibility = Visibility.Visible;
            });
        }

        private void ShowProgressBar(string title)
        {
            this.Dispatcher.Invoke(() =>
            {
                Package.ShowToolWindow();

                this.progressBarPanel.Visibility = Visibility.Visible;

                this.progressBarTitle.Text = title;

                this.resultsGrid.Visibility = Visibility.Collapsed;
            });
        }

        private void ShowIndeterminateProgressBar(string title)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.progressBarPanel.Visibility = Visibility.Visible;

                this.progressBarTitle.Text = title;

                this.progressBar.IsIndeterminate = true;

                this.progressBarPercent.Visibility = Visibility.Collapsed;

                this.resultsGrid.Visibility = Visibility.Collapsed;
            });
        }

        private void UpdateProgressBar(int value)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.progressBar.Value = value;
            });
        }                

        private void HideAllControls()
        {
            this.Dispatcher.Invoke(() =>
            {
                progressBarPanel.Visibility = Visibility.Collapsed;
                resultsGrid.Visibility = Visibility.Collapsed;

                errorPanel.Visibility = Visibility.Visible;

                HideError();
                CleanAndHideVulnerabilityDetailsPanel();
            });
        }

        private void DisplayResultsGrid() => resultsGrid.Visibility = Visibility.Visible;

        private void HideError()
        {
            this.Dispatcher.Invoke(() =>
            {
                errorPanel.Visibility = Visibility.Collapsed;
            });        
        }

        private void CleanVulnerabilitiesTree()
        {
            this.Dispatcher.Invoke(() =>
            {
                vulnerabilitiesTree.Items.Clear();
            });
        }

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

        private void vulnerabilitiesTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> args)
        {            
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
                {
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
                        fixedIn.Text = vulnerability.Remediation;

                        string detaiedIntroducedThroughText = vulnerability.from != null && vulnerability.from.Length != 0
                                    ? string.Join(" > ", vulnerability.from) : "";

                        detaiedIntroducedThrough.Text = detaiedIntroducedThroughText;

                        remediation.Text = vulnerability.fixedIn != null && vulnerability.fixedIn.Length != 0
                                                 ? "Upgrade to " + string.Join(" > ", vulnerability.fixedIn) : "";
                      
                        overview.Text = vulnerability.Overview;

                        moreAboutThisIssue.NavigateUri = new System.Uri(vulnerability.url);
                    }
                    else
                    {                        
                        CleanAndHideVulnerabilityDetailsPanel();
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
            overview.Text = "";
            moreAboutThisIssue.NavigateUri = null;
        }

        private void SetupSeverity(Vulnerability vulnerability)
        {
            Color severityColor;
            string severityText;

            switch (vulnerability.severity)
            {
                case "high":
                    severityColor = (Color)ColorConverter.ConvertFromString("#C75450");
                    severityText = "High severity";

                    break;
                case "medium":
                    severityColor = (Color)ColorConverter.ConvertFromString("#EDA200");
                    severityText = "Medium severity";

                    break;
                case "low":
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

        public static object GetToolWindowResource(object resourceKey) => instance.FindResource(resourceKey);

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            SnykTasksService.Instance().Scan();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            SnykTasksService.Instance().CancelCurrentTask();
        }

        private void cleanButton_Click(object sender, RoutedEventArgs e)
        {
            CleanVulnerabilitiesTree();

            HideAllControls();
        }
    }

    public class VulnerabilityTreeNode
    {
        private const string SeverityHighIcon = "SeverityHighIcon";
        private const string SeverityMediumIcon = "SeverityMediumIcon";
        private const string SeverityLowIcon = "SeverityLowIcon";
        private const string NugetIcon = "NugetIcon";

        public VulnerabilityTreeNode()
        {
            this.Items = new ObservableCollection<VulnerabilityTreeNode>();
        }

        public Vulnerability Vulnerability { get; set; }

        public CliVulnerabilities CliVulnerabilities { get; set; }

        public string Title
        {
            get
            {
                if (CliVulnerabilities != null)
                {
                    return CliVulnerabilities.projectName + "\\" + CliVulnerabilities.displayTargetFile;
                }

                if (Vulnerability != null)
                {
                    return Vulnerability.GetPackageNameTitle();
                }

                return "";
            }
        }

        public string Icon
        {
            get
            {
                if (Vulnerability != null)
                {
                    string severityBitmap;

                    switch (Vulnerability.severity)
                    {
                        case "high":
                            severityBitmap = SeverityHighIcon;

                            break;
                        case "medium":
                            severityBitmap = SeverityMediumIcon;

                            break;
                        case "low":
                            severityBitmap = SeverityLowIcon;

                            break;
                        default:
                            severityBitmap = NugetIcon;

                            break;
                    }

                    return severityBitmap;
                }

                return NugetIcon;
            }
        }

        public ObservableCollection<VulnerabilityTreeNode> Items { get; set; }
    }

    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => SnykToolWindowControl.GetToolWindowResource(value) as BitmapImage;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => "";
    }    
}