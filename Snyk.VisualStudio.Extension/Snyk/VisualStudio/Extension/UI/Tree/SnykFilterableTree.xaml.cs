namespace Snyk.VisualStudio.Extension.UI.Tree
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using Microsoft.VisualStudio.Shell;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.VisualStudio.Extension.CLI;

    /// <summary>
    /// Interaction logic for SnykFilterableComboBox.xaml.
    /// </summary>
    public partial class SnykFilterableTree : UserControl, IRefreshable
    {
        private static SnykFilterableTree instance;

        private readonly RootTreeNode cliRootNode;

        private readonly RootTreeNode codeSequrityRootNode;

        private readonly RootTreeNode codeQualityRootNode;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykFilterableTree"/> class.
        /// </summary>
        public SnykFilterableTree()
        {
            this.InitializeComponent();

            instance = this;

            this.cliRootNode = new OssRootTreeNode(this);
            this.codeSequrityRootNode = new SnykCodeSecurityRootTreeNode(this);
            this.codeQualityRootNode = new SnykCodeQualityRootTreeNode(this);

            this.vulnerabilitiesTree.Items.Add(this.cliRootNode);
            this.vulnerabilitiesTree.Items.Add(this.codeSequrityRootNode);
            this.vulnerabilitiesTree.Items.Add(this.codeQualityRootNode);
        }

        /// <summary>
        /// Selecteted vulnerability node in tree event handler.
        /// </summary>
        public event RoutedEventHandler SelectedVulnerabilityChanged;

        /// <summary>
        /// Gets Cli root node.
        /// </summary>
        public RootTreeNode CliRootNode => this.cliRootNode;

        /// <summary>
        /// Gets code sequrity root node.
        /// </summary>
        public RootTreeNode CodeSequrityRootNode => this.codeSequrityRootNode;

        /// <summary>
        /// Gets code quality root node.
        /// </summary>
        public RootTreeNode CodeQualityRootNode => this.codeQualityRootNode;

        /// <summary>
        /// Set cli root node title to error.
        /// </summary>
        public void SetCliRootNodeTitleToError() => this.cliRootNode.SetErrorTitle();

        /// <summary>
        /// Set cli root node title to error.
        /// </summary>
        public void SetSnykCodeRootNodesTitleToError()
        {
            this.codeQualityRootNode.SetErrorTitle();
            this.codeSequrityRootNode.SetErrorTitle();
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
            if (!this.cliRootNode.Enabled)
            {
                return;
            }

            var groupVulnerabilities = cliResult.GroupVulnerabilities;

            groupVulnerabilities.ForEach(delegate (CliGroupedVulnerabilities groupedVulnerabilities)
            {
                var fileNode = new OssVulnerabilityTreeNode
                {
                    Vulnerabilities = groupedVulnerabilities,
                };

                foreach (string key in groupedVulnerabilities.VulnerabilitiesMap.Keys)
                {
                    var node = new OssVulnerabilityTreeNode
                    {
                        Vulnerability = groupedVulnerabilities.VulnerabilitiesMap[key][0],
                    };

                    fileNode.Items.Add(node);
                }

                this.cliRootNode.Items.Add(fileNode);
            });

            this.cliRootNode.SetDetails(
                cliResult.CriticalSeverityCount,
                cliResult.HighSeverityCount,
                cliResult.MediumSeverityCount,
                cliResult.LowSeverityCount);
        }

        /// <summary>
        /// Append <see cref="AnalysisResult"/> data to tree.
        /// </summary>
        /// <param name="analysisResult"><see cref="AnalysisResult"/> object.</param>
        public void AppendIssues(AnalysisResult analysisResult)
        {
            if (this.codeSequrityRootNode.Enabled)
            {
                this.AppendSnykCodeIssues(this.codeSequrityRootNode, analysisResult, suggestion => suggestion.Categories.Contains("Security"));
            }

            if (this.codeQualityRootNode.Enabled)
            {
                this.AppendSnykCodeIssues(this.codeQualityRootNode, analysisResult, suggestion => !suggestion.Categories.Contains("Security"));
            }
        }

        /// <inheritdoc/>
        public void Refresh() => this.vulnerabilitiesTree.Items.Refresh();

        /// <summary>
        /// Clear tree nodes.
        /// </summary>
        public void Clear()
        {
            this.cliRootNode.Clean();
            this.codeSequrityRootNode.Clean();
            this.codeQualityRootNode.Clean();
        }

        /// <summary>
        /// Display all tree nodes.
        /// </summary>
        internal void DisplayAllVulnerabilities() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            foreach (TreeNode treeNode in this.codeSequrityRootNode.Items)
            {
                ICollectionView collectionView = CollectionViewSource.GetDefaultView(treeNode.Items);

                collectionView.Filter = null;
            }
        });

        /// <summary>
        /// Filter by string. String can contain severity or vulnerability name or both.
        /// </summary>
        /// <param name="filterString">Source filter string.</param>
        internal void FilterBy(string filterString) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var severityFilter = SeverityFilter.ByQueryString(filterString);

            string searchString = severityFilter.GetOnlyQueryString();

            this.FilterOssItems(this.cliRootNode, severityFilter, searchString);

            this.FilterSnykCodeItems(this.codeQualityRootNode, severityFilter, searchString);
            this.FilterSnykCodeItems(this.codeSequrityRootNode, severityFilter, searchString);
        });

        private void FilterOssItems(RootTreeNode rootTreeNode, SeverityFilter severityFilter, string searchString)
        {
            foreach (var treeNode in rootTreeNode.Items)
            {
                CollectionViewSource.GetDefaultView(treeNode.Items).Filter = filterObject =>
                {
                    var filteredTreeNode = filterObject as OssVulnerabilityTreeNode;
                    var vulnerability = filteredTreeNode.Vulnerability;

                    bool isVulnIncluded = severityFilter.IsVulnerabilityIncluded(vulnerability.Severity);

                    if (searchString != null && searchString != string.Empty)
                    {
                        isVulnIncluded = isVulnIncluded && vulnerability.GetPackageNameTitle().Contains(searchString);
                    }

                    return isVulnIncluded;
                };
            }
        }

        private void FilterSnykCodeItems(RootTreeNode rootTreeNode, SeverityFilter severityFilter, string searchString)
        {
            foreach (var treeNode in rootTreeNode.Items)
            {
                CollectionViewSource.GetDefaultView(treeNode.Items).Filter = filterObject =>
                {
                    var filteredTreeNode = filterObject as SnykCodeVulnerabilityTreeNode;
                    var suggestion = filteredTreeNode.Suggestion;

                    bool isVulnIncluded = severityFilter.IsVulnerabilityIncluded(Severity.FromInt(suggestion.Severity));

                    if (searchString != null && searchString != string.Empty)
                    {
                        isVulnIncluded = isVulnIncluded && suggestion.GetDisplayTitleWithLineNumber().Contains(searchString);
                    }

                    return isVulnIncluded;
                };
            }
        }

        private void VulnerabilitiesTree_SelectedItemChanged(object sender, RoutedEventArgs eventArgs) =>
            this.SelectedVulnerabilityChanged?.Invoke(this, eventArgs);

        private void TreeViewItem_Selected(object sender, RoutedEventArgs eventArgs) => MessageBox.Show(eventArgs.ToString());

        private void AppendSnykCodeIssues(RootTreeNode rootNode, AnalysisResult analysisResult, Func<Suggestion, bool> conditionFunction)
        {
            int crititcalSeverityCount = 0;
            int highSeverityCount = 0;
            int mediumSeverityCount = 0;
            int lowSeverityCount = 0;

            foreach (var fileAnalyses in analysisResult.FileAnalyses)
            {
                var issueNode = new SnykCodeFileTreeNode { FileAnalysis = fileAnalyses, };

                var suggestions = fileAnalyses.Suggestions.Where(conditionFunction).ToList();

                crititcalSeverityCount += suggestions.Count(suggestion => Severity.FromInt(suggestion.Severity) == Severity.Critical);
                highSeverityCount += suggestions.Count(suggestion => Severity.FromInt(suggestion.Severity) == Severity.High);
                mediumSeverityCount += suggestions.Count(suggestion => Severity.FromInt(suggestion.Severity) == Severity.Medium);
                lowSeverityCount += suggestions.Count(suggestion => Severity.FromInt(suggestion.Severity) == Severity.Low);

                suggestions.Sort((suggestion1, suggestion2) => suggestion2.Severity.CompareTo(suggestion1.Severity));

                foreach (var suggestion in suggestions)
                {
                    issueNode.Items.Add(new SnykCodeVulnerabilityTreeNode { Suggestion = suggestion, });
                }

                rootNode.Items.Add(issueNode);
            }

            rootNode.SetDetails(
                crititcalSeverityCount,
                highSeverityCount,
                mediumSeverityCount,
                lowSeverityCount);
        }
    }
}
