using Snyk.VisualStudio.Extension.CLI;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
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

        private RootVulnerabilityTreeNode rootNode = new RootVulnerabilityTreeNode();

        public SnykFilterableTree()
        {
            InitializeComponent();            

            instance = this;            

            vulnerabilitiesTree.Items.Add(rootNode);
        }

        public ItemCollection Items
        {
            get { return vulnerabilitiesTree.Items; }
        }

        public void AppendVulnerabilities(CliResult cliResult)
        {
            var groupVulnerabilities = cliResult.GroupVulnerabilities;

            groupVulnerabilities.ForEach(delegate (CliGroupedVulnerabilities groupedVulnerabilities)
            {
                var fileNode = new VulnerabilityTreeNode
                {
                    Vulnerabilities = groupedVulnerabilities
                };

                foreach (string key in groupedVulnerabilities.VulnerabilitiesMap.Keys)
                {
                    var node = new VulnerabilityTreeNode
                    {
                        Vulnerability = groupedVulnerabilities.VulnerabilitiesMap[key][0]
                    };

                    fileNode.Items.Add(node);
                }

                rootNode.Items.Add(fileNode);
            });

            rootNode.SetDetailsTitle(
                cliResult.Count, 
                cliResult.HighSeverityCount, 
                cliResult.MediumSeverityCount, 
                cliResult.LowSeverityCount);

            vulnerabilitiesTree.Items.Refresh();            
        }

        public void Clear() => rootNode.Clean();

        public object SelectedItem => vulnerabilitiesTree.SelectedItem;

        private void vulnerabilitiesTree_SelectedItemChanged(object sender, RoutedEventArgs eventArgs)
        {
            SelectedVulnerabilityChanged?.Invoke(this, eventArgs);
        }

        public static object GetControlResource(object resourceKey) => instance.FindResource(resourceKey);

        internal void DisplayAllVulnerabilities()
        {
            this.Dispatcher.Invoke(() =>
            {
                foreach (VulnerabilityTreeNode treeNode in this.rootNode.Items)
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
                foreach (VulnerabilityTreeNode treeNode in rootNode.Items)
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

        private void TreeViewItem_Selected(object sender, RoutedEventArgs eventArgs)
        {
            MessageBox.Show(eventArgs.ToString());
        }
    }

    public class TreeNode
    {
        protected string title;

        public virtual string Title
        {
            get 
            { 
                return title; 
            }
            
            set 
            { 
                title = value; 
            }
        }        
    }

    public class VulnerabilityTreeNode : TreeNode
    {        
        public VulnerabilityTreeNode()
        {
            this.Items = new ObservableCollection<VulnerabilityTreeNode>();
        }

        public Vulnerability Vulnerability { get; set; }
        
        public CliGroupedVulnerabilities Vulnerabilities { get; set; }
        
        public override string Title 
        {
            get
            {
                if (Vulnerabilities != null)
                {
                    title = Vulnerabilities.ProjectName + "\\" + Vulnerabilities.DisplayTargetFile;
                }

                if (Vulnerability != null)
                {
                    title = Vulnerability.GetPackageNameTitle();
                }

                return title;
            }
        }

        public virtual string Icon
        {
            get
            {
                if (Vulnerability != null)
                {
                    return SnykIconProvider.GetSeverityIcon(Vulnerability.severity);
                }

                return SnykIconProvider.GetPackageManagerIcon(Vulnerabilities.PackageManager);
            }
        }

        public ObservableCollection<VulnerabilityTreeNode> Items { get; set; }
    }

    public class RootVulnerabilityTreeNode : VulnerabilityTreeNode
    {
        public const string OpenSourceSecurityTitle = "Open Source Security";

        public const string OpenSourceSecurityDetailsTitle = "Open Source Security - {0} vulnerabilities: {1} high | {2} medium | {3} low";

        public RootVulnerabilityTreeNode()
        {
            Title = OpenSourceSecurityTitle;
        }

        public void SetDetailsTitle(int vulnerabilitiesCount, int highSeverityCount, int mediumSeverityCount, int lowSeverityCount)
        {            
            Title = String.Format(OpenSourceSecurityDetailsTitle, vulnerabilitiesCount, highSeverityCount, mediumSeverityCount, lowSeverityCount);
        }

        public void Clean()
        {
            Items.Clear();

            Title = OpenSourceSecurityTitle;
        }

        public override string Icon => SnykIconProvider.OpenSourceSecurity;
    }

    public class SnykImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => 
            SnykFilterableTree.GetControlResource(value) as BitmapImage;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => "";
    }

    class SnykIconProvider
    {
        public const string OpenSourceSecurity = "OpenSourceSecurity";
        public const string OpenSourceSecurityDark = "OpenSourceSecurityDark";        

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

    public class LeftMarginMultiplierConverter : IValueConverter
    {
        public double Length { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TreeViewItem item = value as TreeViewItem;

            if (item == null)
            {
                return new Thickness(0);
            }
                
            return new Thickness(Length * item.GetDepth(), 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

    public static class TreeViewItemExtensions
    {
        public static int GetDepth(this TreeViewItem item)
        {
            TreeViewItem parent;
            while ((parent = GetParent(item)) != null)
            {
                return GetDepth(parent) + 1;
            }
            return 0;
        }

        private static TreeViewItem GetParent(TreeViewItem item)
        {
            DependencyObject parent = item != null ? VisualTreeHelper.GetParent(item) : null;

            while (parent != null && !(parent is TreeViewItem || parent is TreeView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as TreeViewItem;
        }
    }
}
