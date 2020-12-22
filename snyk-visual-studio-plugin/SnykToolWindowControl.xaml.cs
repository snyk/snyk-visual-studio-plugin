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
    using System.Drawing;
    using System.Windows.Media;
    using System.Windows.Data;
    using System;
    using System.Globalization;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Interaction logic for SnykToolWindowControl.
    /// </summary>
    public partial class SnykToolWindowControl : UserControl, ISnykProgressBarManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykToolWindowControl"/> class.
        /// </summary>
        public SnykToolWindowControl()
        {
            this.InitializeComponent();
        }

        public SnykVSPackage Package { get; internal set; }

        public void ClearDataGrid()
        {
            this.Dispatcher.Invoke(() =>
            {
                vulnerabilitiesTree.Items.Clear();
            });            
        }

        public void DisplayDataGrid()
        {
            this.Dispatcher.Invoke(() =>
            {
                resultsGrid.Visibility = Visibility.Visible;
            });
        }

        public void AddCliResultToDataGrid(CliResult cliResult)
        {
            this.Dispatcher.Invoke(() =>
            {                                              
                foreach (CliVulnerabilities cliVulnerabilities in cliResult.CLIVulnerabilities)
                {
                    var rootNode = new VulnerabilityTreeNode
                    {
                        CliVulnerabilities = cliVulnerabilities
                    };

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

        public void Hide()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.progressBarPanel.Visibility = Visibility.Collapsed;

                this.progressBarTitle.Text = "";

                this.progressBar.IsIndeterminate = false;
                this.progressBarPercent.Visibility = Visibility.Visible;
            });
        }

        public void SetTitle(string title)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.progressBarTitle.Text = title;
            });
        }

        public void Show()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.progressBarPanel.Visibility = Visibility.Collapsed;
            });
        }

        public void Show(string title)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.progressBarPanel.Visibility = Visibility.Visible;

                this.progressBarTitle.Text = title;

                this.resultsGrid.Visibility = Visibility.Collapsed;
            });
        }

        public void ShowIndeterminate(string title)
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

        public void Update(int value)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.progressBar.Value = value;
            });
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

        public void HideError()
        {
            this.Dispatcher.Invoke(() =>
            {
                errorPanel.Visibility = Visibility.Collapsed;
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
                }
            ); 
        }

        private void SetupSeverity(Vulnerability vulnerability)
        {
            System.Windows.Media.Color severityColor;
            string severityText;

            switch (vulnerability.severity)
            {
                case "high":
                    severityColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#C75450");
                    severityText = "High severity";

                    break;
                case "medium":
                    severityColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EDA200");
                    severityText = "Medium severity";

                    break;
                case "low":
                    severityColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6E6E6E");
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
    }

    public class VulnerabilityTreeNode
    {
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

        public ImageSource Icon
        {
            get
            {
                if (Vulnerability != null)
                {
                    Bitmap severityBitmap;

                    switch (Vulnerability.severity)
                    {
                        case "high":
                            severityBitmap = SnykIcons.SeverityHigh;

                            break;
                        case "medium":
                            severityBitmap = SnykIcons.SeverityMedium;

                            break;
                        case "low":
                            severityBitmap = SnykIcons.SeverityLow;

                            break;
                        default:
                            severityBitmap = SnykIcons.Nuget;

                            break;
                    }

                    return GetImageSource(severityBitmap);
                }

                return GetImageSource(SnykIcons.Nuget);
            }
        }

        public ObservableCollection<VulnerabilityTreeNode> Items { get; set; }

        private ImageSource GetImageSource(Bitmap bitmap)
        {
            return System.Windows.Interop.Imaging
                .CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(16, 16));
        }
    }

    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "";
        }
    }    
}