using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using Microsoft.HtmlConverter;

namespace Microsoft.UI
{
	public class HtmlRichTextBoxBehavior : DependencyObject
	{
		public static readonly DependencyProperty TextProperty = DependencyProperty
			.RegisterAttached("Text", typeof(string), typeof(HtmlRichTextBoxBehavior), new UIPropertyMetadata(null, OnValueChanged));

		public static string GetText(RichTextBox richTextBox) 
		{ 
			return (string)richTextBox.GetValue(TextProperty); 
		}

		public static void SetText(RichTextBox richTextBox, string value) 
		{ 
			richTextBox.SetValue(TextProperty, value); 
		}

		private static void OnValueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs changedEventArgs)
		{
			var richTextBox = (RichTextBox)dependencyObject;

			var text = (changedEventArgs.NewValue ?? string.Empty).ToString();
			var xaml = HtmlToXamlConverter.ConvertHtmlToXaml(text, true);
			var flowDocument = XamlReader.Parse(xaml) as FlowDocument;

			HyperlinksSubscriptions(flowDocument);

			richTextBox.Document = flowDocument;
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

