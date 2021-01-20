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

        internal void FilterBy(string filterString, SeverityCaseOptions severityOptions)
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

                        bool isIncluded = true;

                        if (severityOptions != null)
                        {
                            switch (vulnerability.severity)
                            {
                                case Severity.High:
                                    isIncluded = severityOptions.IsHighIncluded;

                                    break;
                                case Severity.Medium:
                                    isIncluded = severityOptions.IsMediumIncluded;

                                    break;
                                case Severity.Low:
                                    isIncluded = severityOptions.IsLowIncluded;
                                    break;
                                default:
                                    isIncluded = false;

                                    break;
                            }
                        }

                        if (filterString != null && filterString != "")
                        {
                            isIncluded = isIncluded && vulnerability.GetPackageNameTitle().Contains(filterString);
                        }

                        return isIncluded;
                    };
                }
            });
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(e.ToString());
        }
    }

    public class VulnerabilityTreeNode
    {           
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
                    return SnykIconProvider.GetSeverityIcon(Vulnerability.severity);
                }

                return SnykIconProvider.GetPackageManagerIcon(CliVulnerabilities.packageManager);
            }
        }

        public ObservableCollection<VulnerabilityTreeNode> Items { get; set; }
    }

    public class SnykImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => 
            SnykFilterableTree.GetControlResource(value) as BitmapImage;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => "";
    }

    class SnykIconProvider
    {
        private const string NugetIcon = "NugetIcon";
        private const string NpmIcon = "NpmIcon";
        private const string JsIcon = "JsIcon";
        private const string JavaIcon = "JavaIcon";
        private const string PythonIcon = "PythonIcon";
        private const string DefaultIcon = "DefaultIcon";

        private const string SeverityHighIcon = "SeverityHighIcon";
        private const string SeverityMediumIcon = "SeverityMediumIcon";
        private const string SeverityLowIcon = "SeverityLowIcon";

        public static string GetPackageManagerIcon(string packageManager)
        {
            string icon = "";

            switch (packageManager)
            {
                case "nuget":
                    icon = NugetIcon;
                    break;
                case "paket":
                    icon = NugetIcon;
                    break;
                case "npm":
                    icon = NpmIcon;
                    break;
                case "yarn":
                    icon = JsIcon;
                    break;
                case "pip":
                    icon = PythonIcon;
                    break;
                case "yarn-workspace":
                    icon = JsIcon;
                    break;
                case "maven":
                    icon = JavaIcon;
                    break;
                case "gradle":
                    icon = JavaIcon;
                    break;
                default:
                    icon = DefaultIcon;
                    break;
            }

            return icon;
        }

        public static string GetSeverityIcon(string severity)
        {
            string icon;

            switch (severity)
            {
                case Severity.High:
                    icon = SeverityHighIcon;

                    break;
                case Severity.Medium:
                    icon = SeverityMediumIcon;

                    break;
                case Severity.Low:
                    icon = SeverityLowIcon;

                    break;
                default:
                    icon = DefaultIcon;

                    break;
            }

            return icon;
        }
    }
}
