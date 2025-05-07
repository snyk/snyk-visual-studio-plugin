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
        private const string CongratsNoIssuesFound = "✅ Congrats! No issues found!";
        private const string CongratsNoOpenIssuesFound = "✅ Congrats! No open issues found!";
        private const string OpenIssuesAreDisabled = "Open issues are disabled!";
        private const string NoIgnoredIssues = "✋ No ignored issues, open issues are disabled";
        private const string OpenAndIgnoredIssuesAreDisabled = "Open and Ignored issues are disabled!";
        private const string NoFixableIssues = "There are no issues automatically fixable.";
        private const string AllIssueViewOptionsDisabled = "Adjust your settings to view Open or Ignored issues.";
        private const string OpenIssueViewOptionDisabled = "Adjust your settings to view Open issues.";
        private const string IgnoredIssueViewOptionDisabled = "Adjust your settings to view Ignored issues.";

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
            rootNode.Clean();
            // totalIssueCount is the number of issues returned by LS, which pre-filters on Issue View Options and Severity Filters (to be implemented at time of this comment - 6th May 2025).
            var totalIssueCount = 0;
            var criticalSeverityCount = 0;
            var highSeverityCount = 0;
            var mediumSeverityCount = 0;
            var lowSeverityCount = 0;
            var ignoredIssueCount = 0;
            var fixableIssueCount = 0;
            var currentFolder = ThreadHelper.JoinableTaskFactory.Run(async () =>
                await serviceProvider.SolutionService.GetSolutionFolderAsync()).Replace("\\", "/").TrimEnd('/');
            
            var options = serviceProvider.Options;
            var fileTreeNodes = new List<FileTreeNode>();
            foreach (var kv in scanResultDictionary.Where(x => x.Key.Replace("\\", "/").TrimEnd('/').Contains(currentFolder)))
            {
                var issueList = kv.Value.ToList();

                if (additionalFilter != null)
                    issueList = issueList.Where(additionalFilter).ToList();

                var fileNode = TreeNodeProductFactory.GetFileTreeNode(product, rootNode);
                fileNode.IssueList = issueList;
                fileNode.IsExpanded = true;
                fileNode.FileName = kv.Key;
                fileNode.FolderName = currentFolder;

                ignoredIssueCount += issueList.Count(suggestion => suggestion.IsIgnored);

                totalIssueCount += issueList.Count;

                criticalSeverityCount += issueList.Count(suggestion => suggestion.Severity == Severity.Critical);
                highSeverityCount += issueList.Count(suggestion => suggestion.Severity == Severity.High);
                mediumSeverityCount += issueList.Count(suggestion => suggestion.Severity == Severity.Medium);
                lowSeverityCount += issueList.Count(suggestion => suggestion.Severity == Severity.Low);

                fixableIssueCount += issueList.Count(suggestion => suggestion.HasFix());

                issueList.Sort((issue1, issue2) => Severity.ToInt(issue2.Severity) - Severity.ToInt(issue1.Severity));

                foreach (var issue in issueList)
                {
                    var issueNode = TreeNodeProductFactory.GetIssueTreeNode(product, fileNode);
                    issueNode.Issue = issue;
                    fileNode.Items.Add(issueNode);
                }

                if (fileNode.Items.Count > 0)
                {
                    fileTreeNodes.Add(fileNode);
                }
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
            // Depending on Issue View Options, ignored issues might be pre-filtered by the LS and so ignoredIssueCount may be 0.
            // In this case, openIssueCount is the total issue count returned by the LS.
            var openIssueCount = totalIssueCount - ignoredIssueCount;
            bool isCodeNode = rootNode is SnykCodeSecurityRootTreeNode || rootNode is SnykCodeQualityRootTreeNode;

            var text = !isCodeNode ? GetIssueFoundText(options, totalIssueCount) : GetIssueFoundTextForCode(options, totalIssueCount, openIssueCount, ignoredIssueCount);
            rootNode.Items.Add(new InfoTreeNode { Title = text });
            if (totalIssueCount == 0)
            {
                var ivoNode = !isCodeNode ? GetNoIssueViewOptionsSelectedTreeNode(options) : GetNoIssueViewOptionsSelectedTreeNodeForCode(options);
                if (ivoNode != null)
                {
                    rootNode.Items.Add(ivoNode);
                }
            }
            else
            {
                var fixableText = !isCodeNode ? GetFixableIssuesText(options, fixableIssueCount, false) : GetFixableIssuesTextForCode(options, fixableIssueCount);
                if (!string.IsNullOrEmpty(fixableText))
                {
                    rootNode.Items.Add(new InfoTreeNode { Title = fixableText });
                }
            }
        }

        private string GetIssueFoundText(ISnykOptions options, int issueCount) {
            if (options.ConsistentIgnoresEnabled && !options.OpenIssuesEnabled) {
                return OpenIssuesAreDisabled;
            }

            if (issueCount == 0) {
                return CongratsNoIssuesFound;
            } else {
                return $"✋ {issueCount} issue{(issueCount == 1 ? "" : "s" )}";
            }
        }

        private string GetIssueFoundTextForCode(ISnykOptions options, int totalIssueCount, int openIssueCount, int ignoredIssueCount)
        {
            if (!options.ConsistentIgnoresEnabled)
            {
                return GetIssueFoundText(options, totalIssueCount);
            }

            var openIssuesText = $"{openIssueCount} open issue{(openIssueCount == 1 ? "" : "s")}";
            var ignoredIssuesText = $"{ignoredIssueCount} ignored issue{(ignoredIssueCount == 1 ? "" : "s")}";

            if (options.OpenIssuesEnabled && options.IgnoredIssuesEnabled)
            {
                if (totalIssueCount == 0)
                {
                    return CongratsNoIssuesFound;
                }
                else
                {
                    return $"✋ {openIssuesText} & {ignoredIssuesText}";
                }
            }
            if (options.OpenIssuesEnabled)
            {
                if (openIssueCount == 0)
                {
                    return CongratsNoOpenIssuesFound;
                }
                else
                {
                    return $"✋ {openIssuesText}";
                }
            }
            if (options.IgnoredIssuesEnabled)
            {
                if (ignoredIssueCount == 0)
                {
                    return NoIgnoredIssues;
                }
                else
                {
                    return $"✋ {ignoredIssuesText}, open issues are disabled";
                }
            }
            return OpenAndIgnoredIssuesAreDisabled;
        }

        private TreeNode GetNoIssueViewOptionsSelectedTreeNode(ISnykOptions options)
        {
            if (!options.ConsistentIgnoresEnabled)
            {
                return null;
            }

            if (!options.OpenIssuesEnabled)
            {
                return new InfoTreeNode { Title = OpenIssueViewOptionDisabled };
            }

            return null;
        }

        private TreeNode GetNoIssueViewOptionsSelectedTreeNodeForCode(ISnykOptions options)
        {
            if (!options.ConsistentIgnoresEnabled)
            {
                return null;
            }

            if (!options.OpenIssuesEnabled && !options.IgnoredIssuesEnabled)
            {
                return new InfoTreeNode { Title = AllIssueViewOptionsDisabled };
            }

            if (!options.OpenIssuesEnabled)
            {
                return new InfoTreeNode { Title = OpenIssueViewOptionDisabled };
            }

            if (!options.IgnoredIssuesEnabled)
            {
                return new InfoTreeNode { Title = IgnoredIssueViewOptionDisabled };
            }

            return null;
        }

        private string GetFixableIssuesText(ISnykOptions options, int fixableIssueCount, bool sayOpenIssues)
        {
            return fixableIssueCount > 0
                ? $"⚡ {fixableIssueCount}{(sayOpenIssues ? " open" : "")} issue{(fixableIssueCount == 1 ? " is" : "s are")} fixable automatically."
                : NoFixableIssues;
        }

        private string GetFixableIssuesTextForCode(ISnykOptions options, int fixableIssueCount)
        {
            if (options.ConsistentIgnoresEnabled && !options.OpenIssuesEnabled)
            {
                return "";
            }
            return GetFixableIssuesText(options, fixableIssueCount, options.ConsistentIgnoresEnabled);
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
    }
}
