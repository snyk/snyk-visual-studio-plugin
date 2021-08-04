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

        private RootTreeNode cliRootNode = new ScaRootTreeNode();

        private RootTreeNode snykCodeRootNode = new SnykCodeRootTreeNode();

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
                var fileNode = new ScaVulnerabilityTreeNode
                {
                    Vulnerabilities = groupedVulnerabilities,
                };

                foreach (string key in groupedVulnerabilities.VulnerabilitiesMap.Keys)
                {
                    var node = new ScaVulnerabilityTreeNode
                    {
                        Vulnerability = groupedVulnerabilities.VulnerabilitiesMap[key][0],
                    };

                    fileNode.Items.Add(node);
                }

                this.cliRootNode.Items.Add(fileNode);
            });

            this.cliRootNode.SetDetails(
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
            int crititcalSeverityCount = 0; // TODO: move this to proper place.
            int highSeverityCount = 0;
            int mediumSeverityCount = 0;
            int lowSeverityCount = 0;

            foreach (var fileAnalyses in analysisResult.FileAnalyses)
            {
                var fileNode = new SnykCodeFileTreeNode{ FileAnalysis = fileAnalyses, };

                foreach (var suggestion in fileAnalyses.Suggestions)
                {
                    fileNode.Items.Add(new SnykCodeVulnerabilityTreeNode { Suggestion = suggestion, });

                    string severity = Severity.FromInt(suggestion.Severity);

                    if (severity == Severity.Critical)
                    {
                        crititcalSeverityCount++;
                    }

                    if (severity == Severity.High)
                    {
                        highSeverityCount++;
                    }

                    if (severity == Severity.Medium)
                    {
                        mediumSeverityCount++;
                    }

                    if (severity == Severity.Low)
                    {
                        lowSeverityCount++;
                    }
                }

                this.snykCodeRootNode.Items.Add(fileNode);
            }

            this.snykCodeRootNode.SetDetails(
                analysisResult.FileAnalyses.Count,
                crititcalSeverityCount,
                highSeverityCount,
                mediumSeverityCount,
                lowSeverityCount);

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
                foreach (TreeNode treeNode in this.snykCodeRootNode.Items)
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

                foreach (TreeNode treeNode in this.snykCodeRootNode.Items)
                {
                    CollectionViewSource.GetDefaultView(treeNode.Items).Filter = filterObject =>
                    {
                        var filteredTreeNode = filterObject as ScaVulnerabilityTreeNode;
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
