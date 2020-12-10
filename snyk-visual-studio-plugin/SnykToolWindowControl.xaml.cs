//------------------------------------------------------------------------------
// <copyright file="SnykToolWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Snyk.VisualStudio.Extension.UI
{
    using CLI;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;

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

        public void DisplayCliResult(CliResult cliResult)
        {
            this.Dispatcher.Invoke(() =>
            {
                resultsDataGrid.Visibility = Visibility.Visible;

                resultsDataGrid.Items.Clear();

                foreach (CliVulnerabilities cliVulnerabilities in cliResult.CLIVulnerabilities)
                {
                    foreach (Vulnerability vulnerability in cliVulnerabilities.vulnerabilities)
                    {
                        resultsDataGrid.Items.Add(vulnerability);
                    }
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

                this.resultsDataGrid.Visibility = Visibility.Hidden;
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

                this.resultsDataGrid.Visibility = Visibility.Hidden;
            });
        }

        public void Update(int value)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.progressBar.Value = value;
            });
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
                "SnykToolWindow");
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
    }
}