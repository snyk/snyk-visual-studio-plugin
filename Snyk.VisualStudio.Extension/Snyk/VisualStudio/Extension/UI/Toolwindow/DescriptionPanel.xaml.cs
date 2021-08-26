namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.VisualStudio.Extension.CLI;

    /// <summary>
    /// Interaction logic for DescriptionPanel.xaml.
    /// </summary>
    public partial class DescriptionPanel : UserControl
    {
        public DescriptionPanel()
        {
            this.InitializeComponent();
        }

        public Vulnerability Vulnerability
        {
            set
            {
                this.HideSuggestionDetailsPanel();

                var vulnerability = value;

                this.descriptionHeaderPanel.Vulnerability = vulnerability;

                this.vulnerabilityDescriptionGrid.Visibility = Visibility.Visible;

                this.vulnerableModule.Text = vulnerability.Name;

                string introducedThroughText = vulnerability.From != null && vulnerability.From.Length != 0
                            ? string.Join(", ", vulnerability.From) : string.Empty;

                this.introducedThrough.Text = introducedThroughText;
                this.exploitMaturity.Text = vulnerability.Exploit;
                this.fixedIn.Text = string.IsNullOrWhiteSpace(vulnerability.FixedInRemediation)
                    ? $"There is no fixed version for {vulnerability.Name}" : vulnerability.FixedInRemediation;

                string detaiedIntroducedThroughText = vulnerability.From != null && vulnerability.From.Length != 0
                            ? string.Join(" > ", vulnerability.From) : string.Empty;

                this.detaiedIntroducedThrough.Text = detaiedIntroducedThroughText;

                this.remediation.Text = vulnerability.FixedIn != null && vulnerability.FixedIn.Length != 0
                                         ? "Upgrade to" + string.Join(" > ", vulnerability.FixedIn) : string.Empty;

                this.overview.Html = Markdig.Markdown.ToHtml(vulnerability.Description);

                this.moreAboutThisIssue.NavigateUri = new Uri(vulnerability.Url);
            }
        }

        public Suggestion Suggestion
        {
            set
            {
                this.HideVulnerabilityDetailsPanel();

                var suggestion = value;

                this.descriptionHeaderPanel.Suggestion = suggestion;

                this.snykCodeDescriptionGrid.Visibility = Visibility.Visible;

                this.snykCodeDescription.Text = suggestion.Message;
            }
        }

        private void HideVulnerabilityDetailsPanel()
        {
            this.vulnerableModule.Text = string.Empty;
            this.introducedThrough.Text = string.Empty;
            this.exploitMaturity.Text = string.Empty;
            this.fixedIn.Text = string.Empty;
            this.detaiedIntroducedThrough.Text = string.Empty;
            this.remediation.Text = string.Empty;
            this.overview.Html = string.Empty;
            this.moreAboutThisIssue.NavigateUri = null;

            this.vulnerabilityDescriptionGrid.Visibility = Visibility.Collapsed;
        }

        private void HideSuggestionDetailsPanel()
        {
            this.snykCodeDescriptionGrid.Visibility = Visibility.Collapsed;
        }

        private void MoreAboutThisIssue_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs args)
        {
            Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri));

            args.Handled = true;
        }
    }
}
