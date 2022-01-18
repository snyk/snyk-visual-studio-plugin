namespace Snyk.VisualStudio.Extension.Shared.UI.Toolwindow.SnykCode
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;
    using Snyk.Code.Library.Domain.Analysis;

    /// <summary>
    /// Interaction logic for ExternalExampleFixesControl.xaml.
    /// </summary>
    public partial class ExternalExampleFixesControl : UserControl
    {
        private const int ShowExamplesCount = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalExampleFixesControl"/> class.
        /// </summary>
        public ExternalExampleFixesControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Display fixes in tabs.
        /// </summary>
        /// <param name="repoDatasetSize">Count of repositories with fixed this issue.</param>
        /// <param name="fixes">Suggestion fixes.</param>
        internal void Display(int repoDatasetSize, IList<SuggestionFix> fixes)
        {
            this.externalExampleFixesTab.Items.Clear();

            this.Visibility = fixes != null && fixes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (fixes == null || fixes.Count == 0)
            {
                this.shortDescription.Text = "No example fixes available.";

                return;
            }

            var examplesCount = fixes.Count > ShowExamplesCount ? ShowExamplesCount : fixes.Count;

            this.shortDescription.Text = $"This issue was fixed by {repoDatasetSize} projects. Here are {examplesCount} example fixes.";

            for (int i = 0; i < examplesCount; i++)
            {
                var fix = fixes.ElementAt(i);

                var exampleLinesTextBox = new HtmlRichTextBox();
                exampleLinesTextBox.IsReadOnly = true;
                exampleLinesTextBox.Document.Blocks.Clear();
                exampleLinesTextBox.FontFamily = new FontFamily("Consolas");


                foreach (var fixLine in fix.Lines)
                {
                    string lineChangeSymbol = string.Empty;
                    SolidColorBrush backgroundBrush = null;

                    switch (fixLine.LineChange)
                    {
                        case "added":
                            lineChangeSymbol = "+";
                            backgroundBrush = Brushes.LightGreen;

                            break;
                        case "removed":
                            lineChangeSymbol = "-";
                            backgroundBrush = Brushes.LightCoral;

                            break;
                        default:
                            break;
                    }

                    var lineText = $"   {fixLine.LineNumber} {lineChangeSymbol}    {fixLine.Line}";

                    var document = exampleLinesTextBox.Document;

                    Paragraph paragraph = new Paragraph();
                    paragraph.Inlines.Clear();
                    paragraph.Margin = new Thickness(0);
                    paragraph.Padding = new Thickness(2);

                    if (backgroundBrush != null)
                    {
                        paragraph.Background = backgroundBrush;
                    }

                    paragraph.Inlines.Add(lineText);

                    document.Blocks.Add(paragraph);
                }

                this.externalExampleFixesTab.Items.Add(new TabItem
                {
                    Header = new TextBlock { Text = this.GetGithubRepositoryName(fix.CommitURL), },
                    Content = exampleLinesTextBox,
                });
            }
        }

        private string GetGithubRepositoryName(string url)
        {
            var fixUrl = url.Replace("https://", string.Empty);

            fixUrl = fixUrl.IndexOf("/commit/") != -1 ? fixUrl.Substring(0, fixUrl.IndexOf("/commit/")) : fixUrl;

            return fixUrl.Length > 50 ? fixUrl.Substring(0, 50) + "..." : fixUrl;
        }
    }
}
