using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Serilog;
using Snyk.Common;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Interaction logic for OssDescriptionControl.xaml.
    /// </summary>
    public partial class OssDescriptionControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.ForContext<OssDescriptionControl>();
        /// <summary>
        /// Initializes a new instance of the <see cref="OssDescriptionControl"/> class.
        /// </summary>
        public OssDescriptionControl() => this.InitializeComponent();

        /// <summary>
        /// Sets <see cref="OssIssue"/> information and update corresponding UI elements.
        /// </summary>
        public Issue OssIssue
        {
            set
            {
                var vulnerability = value;

                this.vulnerabilityDescriptionGrid.Visibility = Visibility.Visible;
                if (vulnerability.AdditionalData == null) return;

                this.vulnerableModule.Text = vulnerability.AdditionalData.Name;

                string introducedThroughText = vulnerability.AdditionalData.From != null && vulnerability.AdditionalData.From.Count != 0
                            ? string.Join(", ", vulnerability.AdditionalData.From) : string.Empty;

                this.introducedThrough.Text = introducedThroughText;
                this.exploitMaturity.Text = vulnerability.AdditionalData.Exploit;
                this.fixedIn.Text = string.IsNullOrWhiteSpace(vulnerability.FixedInDisplayText)
                    ? $"There is no fixed version for {vulnerability.AdditionalData.Name}" : vulnerability.FixedInDisplayText;

                string detaiedIntroducedThroughText = vulnerability.AdditionalData.From != null && vulnerability.AdditionalData.From.Count != 0
                            ? string.Join(" > ", vulnerability.AdditionalData.From) : string.Empty;

                this.detaiedIntroducedThrough.Text = detaiedIntroducedThroughText;

                this.fix.Text = vulnerability.AdditionalData.FixedIn != null && vulnerability.AdditionalData.FixedIn.Count != 0
                                         ? "Upgrade to " + string.Join(" > ", vulnerability.AdditionalData.FixedIn) : string.Empty;

                this.Overview.Markdown = vulnerability.AdditionalData.Description;
                
                this.moreAboutThisIssue.NavigateUri = new Uri(vulnerability.GetVulnerabilityUrl());
            }
        }

        private void MoreAboutThisIssue_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs args)
        {
            Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri));

            args.Handled = true;
        }
    }
}
