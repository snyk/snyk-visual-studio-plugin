using Snyk.VisualStudio.Extension.CLI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            get { return vulnerabilitiesTree.Items; }
        }

        public void AppendVulnerabilities(List<CliVulnerabilities> cliVulnerabilitiesList)
        {
            foreach (CliVulnerabilities cliVulnerabilities in cliVulnerabilitiesList)
            {
                var rootNode = new VulnerabilityTreeNode
                {
                    CliVulnerabilities = cliVulnerabilities
                };

                Array.Sort(cliVulnerabilities.vulnerabilities);

                foreach (Vulnerability vulnerability in cliVulnerabilities.vulnerabilities)
                {
                    var node = new VulnerabilityTreeNode
                    {
                        Vulnerability = vulnerability
                    };

                    rootNode.Items.Add(node);                    
                }                

                vulnerabilitiesTree.Items.Add(rootNode);
            }
        }

        public void Clear() => vulnerabilitiesTree.Items.Clear();

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

        internal void DisplayAllVulnerabilities()
        {
            this.Dispatcher.Invoke(() =>
            {
                foreach (VulnerabilityTreeNode treeNode in this.vulnerabilitiesTree.Items)
                {
                    ICollectionView collectionView = CollectionViewSource.GetDefaultView(treeNode.Items);

                    collectionView.Filter = null;
                }
            });
        }        

        internal void FilterBy(string filterString)
        {
            this.Dispatcher.Invoke(() =>
            {               
                foreach (VulnerabilityTreeNode treeNode in this.vulnerabilitiesTree.Items)
                {
                    ICollectionView collectionView = CollectionViewSource.GetDefaultView(treeNode.Items);
                    collectionView.Filter = filterObj =>
                    {
                        var filterTreeNode = filterObj as VulnerabilityTreeNode;
                        var vulnerability = filterTreeNode.Vulnerability;

                        if (vulnerability != null && vulnerability.GetPackageNameTitle().Contains(filterString))
                        {
                            return true;
                        }                        

                        return false;
                    };
                }
            });
        }
    }

    public class VulnerabilityTreeNode
    {
        private const string SeverityHighIcon = "SeverityHighIcon";
        private const string SeverityMediumIcon = "SeverityMediumIcon";
        private const string SeverityLowIcon = "SeverityLowIcon";
        private const string NugetIcon = "NugetIcon";

        public VulnerabilityTreeNode()
        {
            this.Items = new ObservableCollection<VulnerabilityTreeNode>();
        }

        public Vulnerability Vulnerability { get; set; }
        
        public CliVulnerabilities CliVulnerabilities { get; set; }

        public string Title
        {
            get
            {
                if (CliVulnerabilities != null)
                {
                    return CliVulnerabilities.projectName + "\\" + CliVulnerabilities.displayTargetFile;
                }

                if (Vulnerability != null)
                {
                    return Vulnerability.GetPackageNameTitle();
                }

                return "";
            }
        }

        public string Icon
        {
            get
            {
                if (Vulnerability != null)
                {
                    string severityBitmap;

                    switch (Vulnerability.severity)
                    {
                        case "high":
                            severityBitmap = SeverityHighIcon;

                            break;
                        case "medium":
                            severityBitmap = SeverityMediumIcon;

                            break;
                        case "low":
                            severityBitmap = SeverityLowIcon;

                            break;
                        default:
                            severityBitmap = NugetIcon;

                            break;
                    }

                    return severityBitmap;
                }

                return NugetIcon;
            }
        }

        public ObservableCollection<VulnerabilityTreeNode> Items { get; set; }
    }

    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => 
            SnykFilterableTree.GetControlResource(value) as BitmapImage;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => "";
    }
}
