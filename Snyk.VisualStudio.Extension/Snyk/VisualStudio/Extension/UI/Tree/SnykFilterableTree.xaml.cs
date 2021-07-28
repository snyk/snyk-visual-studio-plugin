namespace Snyk.VisualStudio.Extension.UI.Tree
{    
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.VisualStudio.Extension.CLI;

    /// <summary>
    /// Interaction logic for SnykFilterableComboBox.xaml.
    /// </summary>
    public partial class SnykFilterableTree : UserControl
    {
        private static SnykFilterableTree instance;

        private RootVulnerabilityTreeNode cliRootNode = new RootVulnerabilityTreeNode();

        private RootVulnerabilityTreeNode snykCodeRootNode = new RootVulnerabilityTreeNode();

        /// <summary>
        /// Selecteted vulnerability node in tree event handler.
        /// </summary>
        public event RoutedEventHandler SelectedVulnerabilityChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykFilterableTree"/> class.
        /// </summary>
        public SnykFilterableTree()
        {
            this.InitializeComponent();

            instance = this;

            this.vulnerabilitiesTree.Items.Add(this.cliRootNode);
            this.vulnerabilitiesTree.Items.Add(this.snykCodeRootNode);
        }

        /// <summary>
        /// Gets a value indicating whether tree items.
        /// </summary>
        public ItemCollection Items => this.vulnerabilitiesTree.Items;

        /// <summary>
        /// Gets a value indicating whether tree selected node.
        /// </summary>
        public object SelectedItem => this.vulnerabilitiesTree.SelectedItem;

        /// <summary>
        /// Append <see cref="CliResult"/> data to tree.
        /// </summary>
        /// <param name="cliResult"><see cref="CliResult"/> object.</param>
        public void AppendVulnerabilities(CliResult cliResult)
        {
            var groupVulnerabilities = cliResult.GroupVulnerabilities;

            groupVulnerabilities.ForEach(delegate (CliGroupedVulnerabilities groupedVulnerabilities)
            {
                var fileNode = new VulnerabilityTreeNode
                {
                    Vulnerabilities = groupedVulnerabilities,
                };

                foreach (string key in groupedVulnerabilities.VulnerabilitiesMap.Keys)
                {
                    var node = new VulnerabilityTreeNode
                    {
                        Vulnerability = groupedVulnerabilities.VulnerabilitiesMap[key][0],
                    };

                    fileNode.Items.Add(node);
                }

                this.snykCodeRootNode.Items.Add(fileNode);
            });

            this.snykCodeRootNode.SetDetailsTitle(
                cliResult.Count,
                cliResult.CriticalSeverityCount,
                cliResult.HighSeverityCount,
                cliResult.MediumSeverityCount,
                cliResult.LowSeverityCount);

            this.vulnerabilitiesTree.Items.Refresh();
        }

        /// <summary>
        /// Append <see cref="AnalysisResult"/> data to tree.
        /// </summary>
        /// <param name="analysisResult"><see cref="AnalysisResult"/> object.</param>
        public void AppendVulnerabilities(AnalysisResult analysisResult)
        {
            foreach (var fileAnalyses in analysisResult.FileAnalyses)
            {
                var fileNode = new VulnerabilityTreeNode
                {
                    Vulnerabilities = new CliGroupedVulnerabilities
                    {
                        ProjectName = fileAnalyses.FileName,
                    },
                };

                foreach (var suggestion in fileAnalyses.Suggestions)
                {
                    var node = new VulnerabilityTreeNode
                    {
                        Vulnerability = new Vulnerability
                        {
                            Id = suggestion.Id,
                            Description = suggestion.Message,
                            Title = suggestion.Rule,
                        },
                    };

                    fileNode.Items.Add(node);
                }

                this.snykCodeRootNode.Items.Add(fileNode);
            }

            this.snykCodeRootNode.SetDetailsTitle(
                analysisResult.FileAnalyses.Count,
                0,
                0,
                0,
                0);

            this.vulnerabilitiesTree.Items.Refresh();
        }

        /// <summary>
        /// Clear tree nodes.
        /// </summary>
        public void Clear() => this.snykCodeRootNode.Clean();

        /// <summary>
        /// Find resource by key.
        /// </summary>
        /// <param name="resourceKey">Resource key.</param>
        /// <returns>object</returns>
        public static object GetControlResource(object resourceKey) => instance.FindResource(resourceKey);

        /// <summary>
        /// Display all tree nodes.
        /// </summary>
        internal void DisplayAllVulnerabilities()
        {
            this.Dispatcher.Invoke(() =>
            {
                foreach (VulnerabilityTreeNode treeNode in this.snykCodeRootNode.Items)
                {
                    ICollectionView collectionView = CollectionViewSource.GetDefaultView(treeNode.Items);

                    collectionView.Filter = null;
                }
            });
        }

        /// <summary>
        /// Filter by string. String can contain severity or vulnerability name or both.
        /// </summary>
        /// <param name="filterString">Source filter string.</param>
        internal void FilterBy(string filterString)
        {
            this.Dispatcher.Invoke(() =>
            {
                var severityFilter = SeverityFilter.ByQueryString(filterString);

                string searchString = severityFilter.GetOnlyQueryString();

                foreach (VulnerabilityTreeNode treeNode in this.snykCodeRootNode.Items)
                {
                    CollectionViewSource.GetDefaultView(treeNode.Items).Filter = filterObject =>
                    {
                        var filteredTreeNode = filterObject as VulnerabilityTreeNode;
                        var vulnerability = filteredTreeNode.Vulnerability;

                        bool isVulnIncluded = severityFilter.IsVulnerabilityIncluded(vulnerability.Severity);

                        if (searchString != null && searchString != string.Empty)
                        {
                            isVulnIncluded = isVulnIncluded && vulnerability.GetPackageNameTitle().Contains(searchString);
                        }

                        return isVulnIncluded;
                    };
                }
            });
        }

        private void VulnerabilitiesTree_SelectedItemChanged(object sender, RoutedEventArgs eventArgs) =>
            this.SelectedVulnerabilityChanged?.Invoke(this, eventArgs);

        private void TreeViewItem_Selected(object sender, RoutedEventArgs eventArgs) => MessageBox.Show(eventArgs.ToString());
    }

    //public class SnykImageConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => 
    //        SnykFilterableTree.GetControlResource(value) as BitmapImage;

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => string.Empty;
    //}

    

    //public class LeftMarginMultiplierConverter : IValueConverter
    //{
    //    public double Length { get; set; }

    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        TreeViewItem item = value as TreeViewItem;

    //        if (item == null)
    //        {
    //            return new Thickness(0);
    //        }
                
    //        return new Thickness(Length * item.GetDepth(), 0, 0, 0);
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new System.NotImplementedException();
    //    }
    //}

    //public static class TreeViewItemExtensions
    //{
    //    public static int GetDepth(this TreeViewItem item)
    //    {
    //        TreeViewItem parent;
    //        while ((parent = GetParent(item)) != null)
    //        {
    //            return GetDepth(parent) + 1;
    //        }
    //        return 0;
    //    }

    //    private static TreeViewItem GetParent(TreeViewItem item)
    //    {
    //        DependencyObject parent = item != null ? VisualTreeHelper.GetParent(item) : null;

    //        while (parent != null && !(parent is TreeViewItem || parent is TreeView))
    //        {
    //            parent = VisualTreeHelper.GetParent(parent);
    //        }

    //        return parent as TreeViewItem;
    //    }
    //}
}
