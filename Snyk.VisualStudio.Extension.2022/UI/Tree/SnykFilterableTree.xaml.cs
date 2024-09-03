using System.Collections.Generic;
using System.IO;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.UI.Tree
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using Microsoft.VisualStudio.Shell;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.VisualStudio.Extension.CLI;
    using Snyk.VisualStudio.Extension.Model;

    /// <summary>
    /// Interaction logic for SnykFilterableComboBox.xaml.
    /// </summary>
    public partial class SnykFilterableTree : UserControl, IRefreshable
    {
        private static SnykFilterableTree instance;

        private readonly RootTreeNode ossRootNode;

        private readonly SnykCodeSecurityRootTreeNode codeSecurityRootNode;

        private readonly SnykCodeQualityRootTreeNode codeQualityRootNode;

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

            this.vulnerabilitiesTree.Items.Add(this.ossRootNode);
            this.vulnerabilitiesTree.Items.Add(this.codeSecurityRootNode);
            this.vulnerabilitiesTree.Items.Add(this.codeQualityRootNode);
        }

        /// <summary>
        /// Selecteted vulnerability node in tree event handler.
        /// </summary>
        public event RoutedEventHandler SelectedVulnerabilityChanged;

        /// <summary>
        /// Gets Cli root node.
        /// </summary>
        public RootTreeNode CliRootNode => this.ossRootNode;

        /// <summary>
        /// Gets code sequrity root node.
        /// </summary>
        public SnykCodeSecurityRootTreeNode CodeSecurityRootNode => this.codeSecurityRootNode;

        /// <summary>
        /// Gets code quality root node.
        /// </summary>
        public SnykCodeQualityRootTreeNode CodeQualityRootNode => this.codeQualityRootNode;

        /// <summary>
        /// Gets a value indicating whether tree items.
        /// </summary>
        public ItemCollection Items => this.vulnerabilitiesTree.Items;

        /// <summary>
        /// Gets a value indicating whether tree selected node.
        /// </summary>
        public object SelectedItem => this.vulnerabilitiesTree.SelectedItem;

        /// <summary>
        /// Sets <see cref="OssResult"/> instance.
        /// </summary>
        public IDictionary<string, IEnumerable<Issue>> OssResult
        {
            set
            {
                if (!this.ossRootNode.Enabled)
                {
                    return;
                }

                this.ossRootNode.Items.Clear();

                var criticalSeverityCount = 0;
                var highSeverityCount = 0;
                var mediumSeverityCount = 0;
                var lowSeverityCount = 0;

                foreach (var kv in value)
                {
                    var filePath = kv.Key;
                    var issueList = kv.Value.ToList();

                    var fileNode = new OssVulnerabilityTreeNode { IssueList = issueList };
                    if (issueList.Any() && issueList.Any() && issueList.First().AdditionalData != null)
                    {
                        var firstIssue = issueList.First();
                        fileNode.PackageManager = firstIssue.AdditionalData.PackageManager;
                        fileNode.DisplayTargetFile = Path.GetFileName(firstIssue.AdditionalData.DisplayTargetFile);
                        fileNode.ProjectName = firstIssue.AdditionalData.ProjectName;
                    }
                    criticalSeverityCount += issueList.Count(suggestion => suggestion.Severity == Severity.Critical);
                    highSeverityCount += issueList.Count(suggestion => suggestion.Severity == Severity.High);
                    mediumSeverityCount += issueList.Count(suggestion => suggestion.Severity == Severity.Medium);
                    lowSeverityCount += issueList.Count(suggestion => suggestion.Severity == Severity.Low);

                    issueList.Sort((issue1, issue2) => Severity.ToInt(issue2.Severity) - Severity.ToInt(issue1.Severity));

                    foreach (var issue in issueList)
                    {
                        var vulNode = new OssVulnerabilityTreeNode { Issue = issue };
                        fileNode.Items.Add(vulNode);
                        if (issue.AdditionalData == null) continue;
                        vulNode.PackageManager = issue.AdditionalData.PackageManager;
                        vulNode.DisplayTargetFile = issue.AdditionalData.DisplayTargetFile;
                        vulNode.ProjectName = issue.AdditionalData.ProjectName;
                    }

                    if (fileNode.Items.Count > 0)
                    {
                        this.ossRootNode.Items.Add(fileNode);
                    }
                }

                this.ossRootNode.CriticalSeverityCount = criticalSeverityCount;
                this.ossRootNode.HighSeverityCount = highSeverityCount;
                this.ossRootNode.MediumSeverityCount = mediumSeverityCount;
                this.ossRootNode.LowSeverityCount = lowSeverityCount;

                this.ossRootNode.State = RootTreeNodeState.ResultDetails;
            }
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
                    this.AppendSnykCodeIssues(this.codeSecurityRootNode, value, issue => issue.AdditionalData.IsSecurityType);
                }

                if (this.codeQualityRootNode.Enabled)
                {
                    this.AppendSnykCodeIssues(this.codeQualityRootNode, value, issue => !issue.AdditionalData.IsSecurityType);
                }
            }
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

            await System.Threading.Tasks.Task
                .WhenAll(restoreOssItemsTask, restoreCodeSecurityItemsTask, restoreCodeQualityItemsTask);
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

            this.FilterOssItems(this.ossRootNode, severityFilter, searchString);

            this.FilterSnykCodeItems(this.codeQualityRootNode, severityFilter, searchString);
            this.FilterSnykCodeItems(this.codeSecurityRootNode, severityFilter, searchString);
        });

        private async System.Threading.Tasks.Task DisplayAllVulnerabilitiesAsync(RootTreeNode rootTreeNode)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            foreach (var treeNode in rootTreeNode.Items)
            {
                var collectionView = CollectionViewSource.GetDefaultView(treeNode.Items);

                collectionView.Filter = null;
            }
        }

        private void FilterOssItems(RootTreeNode rootTreeNode, SeverityFilter severityFilter, string searchString)
        {
            foreach (var treeNode in rootTreeNode.Items)
            {
                CollectionViewSource.GetDefaultView(treeNode.Items).Filter = filterObject =>
                {
                    var filteredTreeNode = filterObject as OssVulnerabilityTreeNode;
                    var vulnerability = filteredTreeNode.Issue;

                    bool isVulnIncluded = severityFilter.IsVulnerabilityIncluded(vulnerability.Severity);

                    if (searchString != null && searchString != string.Empty)
                    {
                        isVulnIncluded = isVulnIncluded && vulnerability.GetPackageNameTitle().ToLowerInvariant().Contains(searchString.ToLowerInvariant());
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

        private void AppendSnykCodeIssues(RootTreeNode rootNode, IDictionary<string, IEnumerable<Issue>> analysisResult, Func<Issue, bool> conditionFunction)
        {
            var criticalSeverityCount = 0;
            var highSeverityCount = 0;
            var mediumSeverityCount = 0;
            var lowSeverityCount = 0;

            rootNode.Clean();

            foreach (var kv in analysisResult)
            {
                var filePath = kv.Key;
                var issueList = kv.Value.ToList();

                var issueNode = new SnykCodeFileTreeNode { IssueList = issueList, FileName = filePath};

                var issues = issueList.Where(conditionFunction).ToList();

                criticalSeverityCount += issues.Count(suggestion => suggestion.Severity == Severity.Critical);
                highSeverityCount += issues.Count(suggestion => suggestion.Severity == Severity.High);
                mediumSeverityCount += issues.Count(suggestion => suggestion.Severity == Severity.Medium);
                lowSeverityCount += issues.Count(suggestion => suggestion.Severity == Severity.Low);

                issues.Sort((issue1,issue2)=> Severity.ToInt(issue2.Severity) - Severity.ToInt(issue1.Severity));

                foreach (var suggestion in issues)
                {
                    issueNode.Items.Add(new SnykCodeVulnerabilityTreeNode { Issue = suggestion });
                }

                if (issueNode.Items.Count > 0)
                {
                    rootNode.Items.Add(issueNode);
                }
            }

            rootNode.CriticalSeverityCount = criticalSeverityCount;
            rootNode.HighSeverityCount = highSeverityCount;
            rootNode.MediumSeverityCount = mediumSeverityCount;
            rootNode.LowSeverityCount = lowSeverityCount;

            rootNode.State = RootTreeNodeState.ResultDetails;
        }
    }
}
