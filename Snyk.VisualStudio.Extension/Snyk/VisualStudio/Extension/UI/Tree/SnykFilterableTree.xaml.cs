namespace Snyk.VisualStudio.Extension.UI.Tree
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
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
        /// Selecteted vulnerability node in tree event handler.
        /// </summary>
        public event RoutedEventHandler SelectedVulnerabilityChanged;

        /// <summary>
        /// Gets a value indicating whether tree items.
        /// </summary>
        public ItemCollection Items => this.vulnerabilitiesTree.Items;

        /// <summary>
        /// Gets a value indicating whether tree selected node.
        /// </summary>
        public object SelectedItem => this.vulnerabilitiesTree.SelectedItem;

        /// <summary>
        /// Find resource by key.
        /// </summary>
        /// <param name="resourceKey">Resource key.</param>
        /// <returns>object</returns>
        public static object GetControlResource(object resourceKey) => instance.FindResource(resourceKey);

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
                            Severity = Severity.FromInt(suggestion.Severity),
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
}
