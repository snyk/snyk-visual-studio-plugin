namespace Microsoft.UI
{    
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Markup;
    using Microsoft.HtmlConverter;

    /// <summary>
    /// Defines the <see cref="HtmlRichTextBoxBehavior" />.
    /// Source example could be found here:
    /// https://www.codeproject.com/Articles/1097390/Displaying-HTML-in-a-WPF-RichTextBox.
    /// </summary>
    public class HtmlRichTextBoxBehavior : DependencyObject
    {
        /// <summary>
        /// Defines the TextProperty.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty
            .RegisterAttached("Text", typeof(string), typeof(HtmlRichTextBoxBehavior), new UIPropertyMetadata(null, OnValueChanged));

        /// <summary>
        /// The GetText.
        /// </summary>
        /// <param name="richTextBox">The richTextBox<see cref="RichTextBox"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public static string GetText(RichTextBox richTextBox)
        {
            return (string)richTextBox.GetValue(TextProperty);
        }

        /// <summary>
        /// The SetText.
        /// </summary>
        /// <param name="richTextBox">The richTextBox<see cref="RichTextBox"/>.</param>
        /// <param name="value">The value<see cref="string"/>.</param>
        public static void SetText(RichTextBox richTextBox, string value)
        {
            richTextBox.SetValue(TextProperty, value);
        }

        /// <summary>
        /// The OnValueChanged.
        /// </summary>
        /// <param name="dependencyObject">The dependencyObject<see cref="DependencyObject"/>.</param>
        /// <param name="changedEventArgs">The changedEventArgs<see cref="DependencyPropertyChangedEventArgs"/>.</param>
        private static void OnValueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs changedEventArgs)
        {
            var richTextBox = (RichTextBox)dependencyObject;

            var text = (changedEventArgs.NewValue ?? string.Empty).ToString();
            var xaml = HtmlToXamlConverter.ConvertHtmlToXaml(text, true);
            var flowDocument = XamlReader.Parse(xaml) as FlowDocument;

            HyperlinksSubscriptions(flowDocument);

            richTextBox.Document = flowDocument;
        }

        /// <summary>
        /// The HyperlinksSubscriptions.
        /// </summary>
        /// <param name="flowDocument">The flowDocument<see cref="FlowDocument"/>.</param>
        private static void HyperlinksSubscriptions(FlowDocument flowDocument)
        {
            if (flowDocument == null)
            {
                return;
            }

            GetVisualChildren(flowDocument).OfType<Hyperlink>().ToList().ForEach(i => i.RequestNavigate += HyperlinkNavigate);
        }

        /// <summary>
        /// The GetVisualChildren.
        /// </summary>
        /// <param name="root">The root<see cref="DependencyObject"/>.</param>
        /// <returns>The <see cref="IEnumerable{DependencyObject}"/>.</returns>
        private static IEnumerable<DependencyObject> GetVisualChildren(DependencyObject root)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            {
                yield return child;
                foreach (var descendants in GetVisualChildren(child)) yield return descendants;
            }
        }

        /// <summary>
        /// The HyperlinkNavigate.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="navigateEventArgs">The navigateEventArgs<see cref="System.Windows.Navigation.RequestNavigateEventArgs"/>.</param>
        private static void HyperlinkNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs navigateEventArgs)
        {
            Process.Start(new ProcessStartInfo(navigateEventArgs.Uri.ToString()));

            navigateEventArgs.Handled = true;
        }
    }
}
