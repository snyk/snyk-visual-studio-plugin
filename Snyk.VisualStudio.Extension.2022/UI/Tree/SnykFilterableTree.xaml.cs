using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.UI.Tree
{
    /// <summary>
    /// Interaction logic for SnykFilterableComboBox.xaml.
    /// </summary>
    public partial class SnykFilterableTree : UserControl, IRefreshable
    {
        private static SnykFilterableTree instance;

        private readonly RootTreeNode ossRootNode;

        private readonly SnykCodeSecurityRootTreeNode codeSecurityRootNode;

        private readonly SnykCodeQualityRootTreeNode codeQualityRootNode;
        private readonly SnykIacRootTreeNode iacRootNode;

        public TreeNode CurrentTreeNode;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykFilterableTree"/> class.
        /// </summary>
        public SnykFilterableTree()
        {
            this.InitializeComponent();

            instance = this;

            this.ossRootNode = new OssRootTreeNode(this);
            this.codeSecurityRootNode = new SnykCodeSecurityRootTreeNode(this);
            this.codeQualityRootNode = new SnykCodeQualityRootTreeNode(this);
            this.iacRootNode = new SnykIacRootTreeNode(this);

            this.vulnerabilitiesTree.Items.Add(this.ossRootNode);
            this.vulnerabilitiesTree.Items.Add(this.codeSecurityRootNode);
            this.vulnerabilitiesTree.Items.Add(this.codeQualityRootNode);
            this.vulnerabilitiesTree.Items.Add(this.iacRootNode);
        }

        /// <summary>
        /// Selecteted vulnerability node in tree event handler.
        /// </summary>
        public event RoutedEventHandler SelectedVulnerabilityChanged;

        /// <summary>
        /// Gets Cli root node.
        /// </summary>
        public RootTreeNode OssRootNode => this.ossRootNode;

        /// <summary>
        /// Gets code sequrity root node.
        /// </summary>
        public SnykCodeSecurityRootTreeNode CodeSecurityRootNode => this.codeSecurityRootNode;

        /// <summary>
        /// Gets code quality root node.
        /// </summary>
        public SnykCodeQualityRootTreeNode CodeQualityRootNode => this.codeQualityRootNode;
        public SnykIacRootTreeNode IacRootNode => this.iacRootNode;

        /// <summary>
        /// Gets a value indicating whether tree items.
        /// </summary>
        public ItemCollection Items => this.vulnerabilitiesTree.Items;

        /// <summary>
        /// Gets a value indicating whether tree selected node.
        /// </summary>
        public object SelectedItem => this.vulnerabilitiesTree.SelectedItem;

        public void SetCurrentSelectedNode()
        {
            if (this.CurrentTreeNode == null)
                return;
            this.CurrentTreeNode.IsSelected = true;
        }

        /// <summary>
        /// Sets <see cref="OssResult"/> instance.
        /// </summary>
        public IDictionary<string, IEnumerable<Issue>> OssResult
        {
            set => FillIssueRootNode(SnykVSPackage.ServiceProvider, value, ossRootNode, Product.Oss);
        }

        /// <summary>
        /// Sets <see cref="AnalysisResult"/> data to tree.
        /// </summary>
        /// <param name="analysisResult"><see cref="AnalysisResult"/> object.</param>
        public IDictionary<string, IEnumerable<Issue>> AnalysisResults
        {
            set
            {
                if (this.codeSecurityRootNode.Enabled)
                {
                    this.FillIssueRootNode(SnykVSPackage.ServiceProvider, value, this.codeSecurityRootNode, Product.Code,
                        issue => issue.AdditionalData.IsSecurityType);
                }

                if (this.codeQualityRootNode.Enabled)
                {
                    this.FillIssueRootNode(SnykVSPackage.ServiceProvider, value, this.codeQualityRootNode, Product.Code,
                        issue => !issue.AdditionalData.IsSecurityType);
                }
            }
        }

        public IDictionary<string, IEnumerable<Issue>> IacResults
        {
            set => FillIssueRootNode(SnykVSPackage.ServiceProvider, value, iacRootNode, Product.Iac);
        }

        private void FillIssueRootNode(ISnykServiceProvider serviceProvider, IDictionary<string, IEnumerable<Issue>> scanResultDictionary, RootTreeNode rootNode, string product, Func<Issue, bool> additionalFilter = null)
        {
            if (!rootNode.Enabled)
            {
                return;
            }

            rootNode.Items.Clear();

            var criticalSeverityCount = 0;
            var highSeverityCount = 0;
            var mediumSeverityCount = 0;
            var lowSeverityCount = 0;
            var ignoredIssueCount = 0;
            var fixableIssueCount = 0;
            var currentFolder = ThreadHelper.JoinableTaskFactory.Run(async () =>
                await serviceProvider.SolutionService.GetSolutionFolderAsync()).Replace("\\", "/");
            var options = serviceProvider.Options;
            var fileTreeNodes = new List<FileTreeNode>();
            foreach (var kv in scanResultDictionary.Where(x => x.Key.Replace("\\", "/").Contains(currentFolder)))
            {
                var issueList = kv.Value.ToList();

                if (additionalFilter != null)
                    issueList = issueList.Where(additionalFilter).ToList();

                var fileNode = TreeNodeProductFactory.GetFileTreeNode(product);
                fileNode.IssueList = issueList;
                fileNode.IsExpanded = false;
                fileNode.FileName = kv.Key;
                fileNode.FolderName = currentFolder;

                ignoredIssueCount += issueList.Count(suggestion => suggestion.IsIgnored);

                issueList = FilterIgnoredIssues(options, issueList).ToList();
                
                criticalSeverityCount += issueList.Count(suggestion => suggestion.Severity == Severity.Critical);
                highSeverityCount += issueList.Count(suggestion => suggestion.Severity == Severity.High);
                mediumSeverityCount += issueList.Count(suggestion => suggestion.Severity == Severity.Medium);
                lowSeverityCount += issueList.Count(suggestion => suggestion.Severity == Severity.Low);

                fixableIssueCount += issueList.Count(suggestion => suggestion.HasFix());

                issueList.Sort((issue1, issue2) => Severity.ToInt(issue2.Severity) - Severity.ToInt(issue1.Severity));

                foreach (var issue in issueList)
                {
                    var issueNode = TreeNodeProductFactory.GetIssueTreeNode(product);
                    issueNode.Issue = issue;
                    fileNode.Items.Add(issueNode);
                }

                if (fileNode.Items.Count > 0)
                {
                    fileTreeNodes.Add(fileNode);
                }
            }


            var totalIssueCount = scanResultDictionary.Values.SelectMany(value => value).Count();
            var folderConfig = options.FolderConfigs.SingleOrDefault(x=> x.FolderPath.Replace("\\", "/") == currentFolder);
            if (options.EnableDeltaFindings && folderConfig != null)
            {
                rootNode.Items.Add(new BaseBranchTreeNode{Title = $"Base branch: {folderConfig.BaseBranch}"});
            }
            AddInfoTreeNodes(rootNode, totalIssueCount, ignoredIssueCount, fixableIssueCount, options);

            foreach (var ossFileTreeNode in fileTreeNodes)
            {
                rootNode.Items.Add(ossFileTreeNode);
            }

            rootNode.CriticalSeverityCount = criticalSeverityCount;
            rootNode.HighSeverityCount = highSeverityCount;
            rootNode.MediumSeverityCount = mediumSeverityCount;
            rootNode.LowSeverityCount = lowSeverityCount;

            rootNode.State = RootTreeNodeState.ResultDetails;
        }

        private void AddInfoTreeNodes(RootTreeNode rootNode, int totalIssueCount, int ignoredIssueCount,
            int fixableIssueCount, ISnykOptions options)
        {
            var plural = GetPlural(totalIssueCount);
            var text = "✅ Congrats! No issues found!";

            if (totalIssueCount > 0)
            {
                text = $"✋ {totalIssueCount} issue{plural} found by Snyk";
                if (options.ConsistentIgnoresEnabled)
                {
                    text += $", {ignoredIssueCount} ignored";
                }
            }

            rootNode.Items.Add(new InfoTreeNode { Title = text });

            if (fixableIssueCount > 0)
            {
                rootNode.Items.Add(new InfoTreeNode
                    { Title = $"⚡ {fixableIssueCount} issue{plural} can be fixed automatically" });
            }
            else if(totalIssueCount > 0)
            {
                rootNode.Items.Add(new InfoTreeNode
                    { Title = "There are no issues automatically fixable" });
            }

            if (options.ConsistentIgnoresEnabled)
            {
                if (ignoredIssueCount == totalIssueCount && !options.IgnoredIssuesEnabled)
                {
                    rootNode.Items.Add(new InfoTreeNode
                        { Title = "Adjust your Issue View Options to see ignored issues." });
                }
                else if (ignoredIssueCount == 0 && !options.OpenIssuesEnabled)
                {
                    rootNode.Items.Add(new InfoTreeNode
                        { Title = "Adjust your Issue View Options to open issues." });
                }
            }
        }

        private string GetPlural(int count)
        {
            return count > 1 ? "s" : "";
        }

        private IEnumerable<Issue> FilterIgnoredIssues(ISnykOptions options, IEnumerable<Issue> issueList)
        {
            var includeIgnoredIssues = true;
            var includeOpenedIssues = true;
            if (options.ConsistentIgnoresEnabled)
            {
                includeOpenedIssues = options.OpenIssuesEnabled;
                includeIgnoredIssues = options.IgnoredIssuesEnabled;
            }

            return issueList.Where(x => x.IsVisible(includeIgnoredIssues, includeOpenedIssues));
        }

        /// <summary>
        /// Find resource by key.
        /// </summary>
        /// <param name="resourceKey">Resource key.</param>
        /// <returns>object</returns>
        public static object GetControlResource(object resourceKey) => instance.FindResource(resourceKey);

        /// <inheritdoc/>
        public void Refresh() => this.vulnerabilitiesTree.Items.Refresh();

        /// <summary>
        /// Clear tree nodes.
        /// </summary>
        public void Clear()
        {
            this.ossRootNode.Clean();
            this.codeSecurityRootNode.Clean();
            this.codeQualityRootNode.Clean();
            this.iacRootNode.Clean();
        }

        /// <summary>
        /// Display all tree nodes.
        /// </summary>
        internal void DisplayAllVulnerabilities() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var restoreOssItemsTask = this.DisplayAllVulnerabilitiesAsync(this.ossRootNode);
            var restoreCodeSecurityItemsTask = this.DisplayAllVulnerabilitiesAsync(this.codeSecurityRootNode);
            var restoreCodeQualityItemsTask = this.DisplayAllVulnerabilitiesAsync(this.codeQualityRootNode);
            var restoreIacItemsTask = this.DisplayAllVulnerabilitiesAsync(this.iacRootNode);

            await System.Threading.Tasks.Task
                .WhenAll(restoreOssItemsTask, restoreCodeSecurityItemsTask, restoreCodeQualityItemsTask, restoreIacItemsTask);
        });

        /// <summary>
        /// Filter by string. String can contain severity or vulnerability name or both.
        /// </summary>
        /// <param name="filterString">Source filter string.</param>
        internal void FilterBy(string filterString) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var severityFilter = SeverityFilter.ByQueryString(filterString);

            var searchString = severityFilter.GetOnlyQueryString();

            this.FilterTreeItems(this.ossRootNode, severityFilter, searchString);

            this.FilterTreeItems(this.codeQualityRootNode, severityFilter, searchString);
            this.FilterTreeItems(this.codeSecurityRootNode, severityFilter, searchString);
            this.FilterTreeItems(this.iacRootNode, severityFilter, searchString);
        });

        private async Task DisplayAllVulnerabilitiesAsync(RootTreeNode rootTreeNode)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            foreach (var treeNode in rootTreeNode.Items)
            {
                var collectionView = CollectionViewSource.GetDefaultView(treeNode.Items);

                collectionView.Filter = null;
            }
        }

        private void FilterTreeItems(RootTreeNode rootTreeNode, SeverityFilter severityFilter, string searchString)
        {
            foreach (var treeNode in rootTreeNode.Items)
            {
                CollectionViewSource.GetDefaultView(treeNode.Items).Filter = filterObject =>
                {
                    var filteredTreeNode = filterObject as IssueTreeNode;
                    if (filteredTreeNode == null) return false;

                    var issue = filteredTreeNode.Issue;

                    var isVulnIncluded = severityFilter.IsVulnerabilityIncluded(issue.Severity);

                    if (!string.IsNullOrEmpty(searchString))
                    {
                        isVulnIncluded = isVulnIncluded && issue.GetDisplayTitleWithLineNumber().ToLowerInvariant().Contains(searchString.ToLowerInvariant());
                    }

                    return isVulnIncluded;
                };
            }
        }

        private void VulnerabilitiesTree_SelectedItemChanged(object sender, RoutedEventArgs eventArgs) =>
            this.SelectedVulnerabilityChanged?.Invoke(this, eventArgs);

        private void TreeViewItem_Selected(object sender, RoutedEventArgs eventArgs) => MessageBox.Show(eventArgs.ToString());

        private void TreeView_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }
    }
}
