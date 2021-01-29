using System;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

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

        public LinksRichTextBox() { }

        private static FlowDocument GetCustomDocument(string text)
        {
            FlowDocument document = new FlowDocument();
            Paragraph paragraph = new Paragraph();

            paragraph.Margin = new Thickness(0); // remove indent between paragraphs

            foreach (string word in text.Split(' ').ToList())
            {
                Uri uriResult;
                bool isUrlWord = Uri.TryCreate(word, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (isUrlWord)
                {
                    Hyperlink link = new Hyperlink();

                    link.IsEnabled = true;
                    link.Inlines.Add(word);
                    link.NavigateUri = new Uri(word);
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
