using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Snyk.VisualStudio.Extension.UI
{
    public class LinksRichTextBox : RichTextBox
    {
        #region RichText Dependency Property

        public static readonly DependencyProperty RichTextProperty = 
            DependencyProperty.Register("RichText", typeof(string), typeof(LinksRichTextBox), 
                new PropertyMetadata(string.Empty, RichTextChangedCallback), RichTextValidateCallback);

        private static void RichTextChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            (dependencyObject as LinksRichTextBox).Document = GetRichTextDocument(eventArgs.NewValue as string);
        }

        private static bool RichTextValidateCallback(object value)
        {
            return value != null;
        }

        public string RichText
        {
            get
            {
                return (string) GetValue(RichTextProperty);
            }
            set
            {
                SetValue(RichTextProperty, value == null ? "" : value);
            }
        }

        #endregion

        public LinksRichTextBox()
        {
            this.Loaded += OnInitialized;
        }

        public void OnInitialized(object source, RoutedEventArgs eventArgs)
        {
            base.OnInitialized(eventArgs);

            SetupForeground();
        }        

        public void SetupForeground()
        {
            SolidColorBrush backgroundBrush = this.Background as SolidColorBrush;

            var resultBrushColor = new SolidColorBrush(Colors.Black); ;

            if (backgroundBrush != null)
            {
                Color mediaBackgroundColor = backgroundBrush.Color;

                var backgroundColor = System.Drawing.Color
                    .FromArgb(mediaBackgroundColor.A, mediaBackgroundColor.R, mediaBackgroundColor.G, mediaBackgroundColor.B);

                if (backgroundColor.GetBrightness() < 0.5)
                {
                    resultBrushColor = new SolidColorBrush(Colors.White);
                }
            }

            this.Foreground = resultBrushColor;
        }

        private static FlowDocument GetRichTextDocument(string text)
        {
            FlowDocument document = new FlowDocument();
            Paragraph paragraph = new Paragraph();

            paragraph.Margin = new Thickness(0); // remove indent between paragraphs

            //foreach (string word in text.Split(' ').ToList())
            //{               
            //    paragraph.Inlines.Add(word);
            //    paragraph.Inlines.Add(" ");
            //}

            paragraph.Inlines.Add(text);

            document.Blocks.Add(paragraph);

            return document;
        }        
    }
}
