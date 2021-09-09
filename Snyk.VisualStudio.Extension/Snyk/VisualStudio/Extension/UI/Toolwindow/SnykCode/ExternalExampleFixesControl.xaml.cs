namespace Snyk.VisualStudio.Extension.UI.Toolwindow.SnykCode
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
        /// <param name="fixes">Suggestion fixes.</param>
        internal void DisplayFixes(IList<SuggestionFix> fixes)
        {
            this.externalExampleFixesTab.Items.Clear();

            if (fixes == null || fixes.Count == 0)
            {
                this.shortDescription.Text = "No example fixes available.";

                return;
            }

            var examplesCount = fixes.Count > 3 ? 3 : fixes.Count;

            this.shortDescription.Text = $"This issue was fixed by {fixes.Count} projects. Here are {examplesCount} example fixes.";

            for (int i = 0; i < examplesCount; i++)
            {
                var fix = fixes.ElementAt(i);

                var exampleLinesTextBox = new RichTextBox();
                exampleLinesTextBox.IsReadOnly = true;
                exampleLinesTextBox.Document.Blocks.Clear();

                foreach (var fixLine in fix.Lines)
                {
                    string lineChange = string.Empty;
                    SolidColorBrush backgroundBrush = null;

                    switch (fixLine.LineChange)
                    {
                        case "added":
                            lineChange = "+";
                            backgroundBrush = Brushes.LightGreen;

                            break;
                        case "removed":
                            lineChange = "-";
                            backgroundBrush = Brushes.LightCoral;

                            break;
                        default:
                            break;
                    }

                    var lineText = $"   {fixLine.LineNumber} {lineChange}    {fixLine.Line}";

                    lineText = lineText + new string(' ', 300 - lineText.Length);

                    var document = exampleLinesTextBox.Document;

                    Paragraph paragraph = new Paragraph();
                    paragraph.Inlines.Clear();
                    paragraph.Margin = new Thickness(0);
                    paragraph.Padding = new Thickness(0);

                    paragraph.Inlines.Add(lineText);
                    document.Blocks.Add(paragraph);

                    if (backgroundBrush != null)
                    {
                        var textRange = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);

                        textRange.ApplyPropertyValue(TextElement.BackgroundProperty, backgroundBrush);
                    }
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

            return fixUrl.Length > 50 ? fixUrl.Substring(0, 50) : fixUrl;
        }
    }
}
