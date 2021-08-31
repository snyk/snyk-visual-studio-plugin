namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.VisualStudio.Extension.CLI;
    using Snyk.VisualStudio.Extension.UI.Toolwindow.SnykCode;

    /// <summary>
    /// Interaction logic for DescriptionPanel.xaml.
    /// </summary>
    public partial class DescriptionPanel : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptionPanel"/> class. For OSS scan result.
        /// </summary>
        public DescriptionPanel()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Sets <see cref="Vulnerability"/> information and update corresponding UI elements.
        /// </summary>
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

        /// <summary>
        /// Sets <see cref="Suggestion"/> information and update corresponding UI elements. For SnykCode scan result.
        /// </summary>
        public Suggestion Suggestion
        {
            set
            {
                this.HideVulnerabilityDetailsPanel();

                var suggestion = value;

                this.descriptionHeaderPanel.Suggestion = suggestion;

                this.snykCodeDescriptionGrid.Visibility = Visibility.Visible;

                this.snykCodeDescription.Text = suggestion.Message;

                var index = 1;

                this.dataFlowStepsControl.Clear();

                var markers = suggestion.Markers.Distinct(new MarkerByPositionLineComparer());

                foreach (var marker in markers)
                {
                    foreach (var position in marker.Positions)
                    {
                        string filePosition = position.FileName;
                        int fileSeparatorIndex = filePosition.LastIndexOf("/") + 1;
                        filePosition = filePosition.Substring(fileSeparatorIndex, filePosition.Length - fileSeparatorIndex);

                        long startLineNumber = position.Rows.ElementAt(0);

                        filePosition = filePosition + ":" + startLineNumber;

                        var dataFlowStep = new DataFlowStep
                        {
                            FileName = filePosition,
                            RowNumber = index.ToString(),
                            LineContent = this.GetLineContent(position.FileName, startLineNumber),
                            NabigationInformation = this.GetNavigationInformation(
                                position.FileName,
                                position.Rows.ElementAt(0) - 1,
                                position.Rows.ElementAt(1) - 1,
                                position.Columns.ElementAt(0),
                                position.Columns.ElementAt(1)),
                        };

                        index++;

                        this.dataFlowStepsControl.AddStep(dataFlowStep);
                    }
                }
            }
        }

        private string GetNavigationInformation(string file, long startLine, long endLine, long startColumn, long endColumn)
        {
            string filePath = this.GetFullPath(file);

            return $"{filePath}|{startLine}|{endLine}|{startColumn}|{endColumn}";
        }

        private string GetLineContent(string file, long lineNumber)
        {
            string filePath = this.GetFullPath(file);

            string line = string.Empty;

            try
            {
                int fileLineNumber = 0;

                using (var reader = new StreamReader(filePath))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (fileLineNumber == (lineNumber - 1))
                        {
                            return line.Trim();
                        }

                        fileLineNumber++;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e); // todo;
            }

            return line;
        }

        private string GetFullPath(string file)
        {
            string partialPath = file.Substring(1, file.Length - 1);

            string solutionPath = SnykSolutionService.Instance.GetSolutionPath();

            return Path.Combine(solutionPath, partialPath);
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
