using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Snyk.VisualStudio.Extension.UI
{
    /// <summary>
    /// Interaction logic for SnykFilterableComboBox.xaml
    /// </summary>
    public partial class SnykFilterableTree : UserControl
    {
        public event RoutedEventHandler SelectedVulnerabilityChanged;

        private static SnykFilterableTree instance;

        public SnykFilterableTree()
        {
            InitializeComponent();

            instance = this;
        }

        public ItemCollection Items
        {
            get
            {
                return vulnerabilitiesTree.Items;
            }
        }
        
        public object SelectedItem
        {
            get
            {
                return vulnerabilitiesTree.SelectedItem;
            }
        }        
        
        private void vulnerabilitiesTree_SelectedItemChanged(object sender, RoutedEventArgs eventArgs)
        {
            SelectedVulnerabilityChanged?.Invoke(this, eventArgs);
        }

        public static object GetControlResource(object resourceKey) => instance.FindResource(resourceKey);
    }

    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => 
            SnykFilterableTree.GetControlResource(value) as BitmapImage;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => "";
    }
}
