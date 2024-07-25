namespace Snyk.VisualStudio.Extension.UI.Toolwindow.SnykCode
{
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;
    using Microsoft.VisualStudio.PlatformUI;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.VisualStudio.Extension.Theme;

    /// <summary>
    /// Extended <see cref="RichTextBox"/> for display code example with red/green highlite rows.
    /// </summary>
    public class ExternalExampleFixRichTextBox : RichTextBox
    {
        public static readonly DependencyProperty LinesProperty =
            DependencyProperty.Register("Lines", typeof(ObservableCollection<FixLine>), typeof(ExternalExampleFixRichTextBox), new PropertyMetadata(OnLinesChanged));
        private static readonly SolidColorBrush redBrush = VSColorTheme.GetThemedColor(EnvironmentColors.VizSurfaceRedDarkBrushKey).ToBrush();
        private static readonly SolidColorBrush greenBrush = VSColorTheme.GetThemedColor(EnvironmentColors.VizSurfaceGreenDarkBrushKey).ToBrush();

        /// <summary>
        /// Gets or sets lines of code in editor.
        /// </summary>
        public ObservableCollection<FixLine> Lines
        {
            get => this.GetValue(LinesProperty) as ObservableCollection<FixLine>;
            set => this.SetValue(LinesProperty, value);
        }

        /// <summary>
        /// Create new flow document with code lines using <see cref="Paragraph"/> with proper background brush.
        /// </summary>
        /// <param name="dependencyObject">Must be ExternalExampleFixRichTextBox instance.</param>
        /// <param name="evenArgs">Event args with new value as ObservableCollection<FixLine>.</param>
        private static void OnLinesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs evenArgs)
        {
            if (dependencyObject is ExternalExampleFixRichTextBox richTextBox)
            {
                richTextBox.Document.Blocks.Clear();

                var lines = evenArgs.NewValue as ObservableCollection<FixLine>;

                if (lines == null) return;

                FlowDocument flowDocument = new FlowDocument();

                foreach (var line in lines)
                {
                    var lineDecorations = GetLineDecorations(line.LineChange);
                    
                    var paragraph = new Paragraph();
                    paragraph.Margin = new Thickness(0);
                    paragraph.Padding = new Thickness(2);

                    if (lineDecorations.Item2 != null)
                    {
                        paragraph.Background = lineDecorations.Item2;
                    }

                    paragraph.Inlines.Add($"   {line.LineNumber} {lineDecorations.Item1}    {line.Line}");

                    flowDocument.Blocks.Add(paragraph);
                }

                richTextBox.Document = flowDocument;
            }
        }

        /// <summary>
        /// This method transform this line to "+" or "-" and return brush for each case.
        /// </summary>
        /// <param name="lineChange">Could be "added" or "removed". </param>
        /// <returns>Return tuple with "+" or "-" and brush for each case.</returns>
        private static (string, SolidColorBrush) GetLineDecorations(string lineChange)
        {
            string lineChangeSymbol = string.Empty;
            SolidColorBrush backgroundBrush = null;
            switch (lineChange)
            {
                case "added":
                    lineChangeSymbol = "+";
                    backgroundBrush = greenBrush;
                    break;
                case "removed":
                    lineChangeSymbol = "-";
                    backgroundBrush = redBrush;
                    break;
                default:
                    break;
            }

            return (lineChangeSymbol, backgroundBrush);
        }
    }
}
