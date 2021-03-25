using Microsoft.HtmlConverter;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace Snyk.VisualStudio.Extension.UI
{
    public class HtmlRichTextBox : RichTextBox
    {
        #region RichText Dependency Property

        public static readonly DependencyProperty HtmlProperty = 
            DependencyProperty.Register("Html", typeof(string), typeof(HtmlRichTextBox), 
                new PropertyMetadata(string.Empty, HtmlChangedCallback), HtmlValidateCallback);

        private static void HtmlChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            (dependencyObject as HtmlRichTextBox).Document = GetHtmlFlowDocument(eventArgs.NewValue as string);
        }

        private static bool HtmlValidateCallback(object value)
        {
            return value != null;
        }

        public string Html
        {
            get
            {
                return (string) GetValue(HtmlProperty);
            }
            set
            {
                SetValue(HtmlProperty, value == null ? "" : value);
            }
        }

        #endregion

        public HtmlRichTextBox()
        {
            this.Loaded += OnInitialized;
        }

        public void OnInitialized(object source, RoutedEventArgs eventArgs)
        {
            base.OnInitialized(eventArgs);

            AdaptForeground();
        }        

        public void AdaptForeground()
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

        private static FlowDocument GetHtmlFlowDocument(string text)
        {           
            var xaml = HtmlToXamlConverter.ConvertHtmlToXaml(text, true);
            var flowDocument = XamlReader.Parse(xaml) as FlowDocument;

            HyperlinksSubscriptions(flowDocument);

            return flowDocument;
        }

        private static void HyperlinksSubscriptions(FlowDocument flowDocument)
        {
            if (flowDocument == null)
            {
                return;
            }

            GetVisualChildren(flowDocument).OfType<Hyperlink>().ToList().ForEach(i => i.RequestNavigate += HyperlinkNavigate);
        }

        private static IEnumerable<DependencyObject> GetVisualChildren(DependencyObject root)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            {
                yield return child;
                foreach (var descendants in GetVisualChildren(child)) yield return descendants;
            }
        }

        private static void HyperlinkNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs navigateEventArgs)
        {
            Process.Start(new ProcessStartInfo(navigateEventArgs.Uri.ToString()));

            navigateEventArgs.Handled = true;
        }
    }
}
