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
                    var rootNode = new TreeNode
                    {
                        Title = cliVulnerabilities.projectName + "\\" + cliVulnerabilities.displayTargetFile
                    };

                    foreach (Vulnerability vulnerability in cliVulnerabilities.vulnerabilities)
                    {
                        var node = new TreeNode
                        {
                            Title = vulnerability.GetPackageNameTitle(),
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
                this.progressBarPanel.Visibility = Visibility.Hidden;

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
                this.progressBarPanel.Visibility = Visibility.Visible;
            });
        }

        public void Show(string title)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.progressBarPanel.Visibility = Visibility.Visible;

                this.progressBarTitle.Text = title;

                this.resultsGrid.Visibility = Visibility.Hidden;
            });
        }

        public void ShowIndeterminate(string title)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.progressBarPanel.Visibility = Visibility.Visible;

                this.progressBarTitle.Text = title;

                this.progressBar.IsIndeterminate = true;

                this.progressBarPercent.Visibility = Visibility.Hidden;

                this.resultsGrid.Visibility = Visibility.Hidden;
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
                progressBarPanel.Visibility = Visibility.Hidden;
                resultsGrid.Visibility = Visibility.Hidden;

                errorPanel.Visibility = Visibility.Visible;

                errorMessage.Text = cliError.Message;
                errorPath.Text = cliError.Path;
            });
        }

        public void HideError()
        {
            this.Dispatcher.Invoke(() =>
            {
                errorPanel.Visibility = Visibility.Hidden;
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
                    var treeNode = vulnerabilitiesTree.SelectedItem as TreeNode;

                    if (treeNode == null)
                    {
                        return;
                    }

                    if (treeNode.Vulnerability != null)
                    {
                        var vulnerability = treeNode.Vulnerability;

                        vulnerableModule.Text = vulnerability.name;

                        string introducedThroughText = vulnerability.from != null && vulnerability.from.Length != 0 
                                    ? string.Join(", ", vulnerability.from) : "";

                        introducedThrough.Text = introducedThroughText;
                        exploitMaturity.Text = vulnerability.exploit;
                        fixedIn.Text = vulnerability.Remediation;

                        string detaiedIntroducedThroughText = vulnerability.from != null && vulnerability.from.Length != 0
                                    ? string.Join(" > ", vulnerability.from) : "";

                        detaiedIntroducedThrough.Text = detaiedIntroducedThroughText;

                        string remediationText = vulnerability.fixedIn != null && vulnerability.fixedIn.Length != 0
                                                 ? string.Join(" > ", vulnerability.fixedIn) : "";

                        remediation.Text = "Upgrade to " + remediationText;

                        overview.Text = vulnerability.Overview;

                        moreAboutThisIssue.NavigateUri = new System.Uri(vulnerability.url);
                    }
                    else
                    {
                        // Group name
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

        private void moreAboutThisIssue_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs args)
        {
            Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri));

            args.Handled = true;
        }
    }

    public class TreeNode
    {
        public TreeNode()
        {
            this.Items = new ObservableCollection<TreeNode>();
        }

        public Vulnerability Vulnerability { get; set; }

        public string Title { get; set; }

        public ObservableCollection<TreeNode> Items { get; set; }
    }
}