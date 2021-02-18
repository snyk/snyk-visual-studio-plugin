using System;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Snyk.VisualStudio.Extension.UI
{
    public class LinksRichTextBox : RichTextBox
    {
        #region CustomText Dependency Property

        public static readonly DependencyProperty RichTextProperty = 
            DependencyProperty.Register("CustomText", typeof(string), typeof(LinksRichTextBox), 
                new PropertyMetadata(string.Empty, CustomTextChangedCallback), RichTextValidateCallback);

        private static void CustomTextChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            (dependencyObject as LinksRichTextBox).Document = GetCustomDocument(eventArgs.NewValue as string);
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
                SetValue(RichTextProperty, value);
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

        private static FlowDocument GetCustomDocument(string text)
        {
            FlowDocument document = new FlowDocument();
            Paragraph paragraph = new Paragraph();

            paragraph.Margin = new Thickness(0); // remove indent between paragraphs

            foreach (string word in text.Split(' ').ToList())
            {
                Uri uriResult;

                string urlWord = word;

                if (word.Contains("(") && word.Contains(")"))
                {
                    int startIndex = word.IndexOf("(") + 1;
                    int lastIndex = word.IndexOf(")");

                    if (lastIndex < startIndex)
                    {
                        lastIndex = word.LastIndexOf(")");
                    }
                    
                    urlWord = word.Substring(startIndex, lastIndex - startIndex);
                }

                bool isUrlWord = Uri.TryCreate(urlWord, UriKind.Absolute, out uriResult) 
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (isUrlWord)
                {
                    Hyperlink link = new Hyperlink();

                    link.IsEnabled = true;
                    link.Inlines.Add(urlWord);
                    link.NavigateUri = new Uri(urlWord);
                    link.RequestNavigate += (sender, args) => Process.Start(args.Uri.ToString());

                    paragraph.Inlines.Add(link);
                }
                else
                {
                    paragraph.Inlines.Add(word);
                }

                paragraph.Inlines.Add(" ");
            }

            document.Blocks.Add(paragraph);

            return document;
        }        
    }
}
