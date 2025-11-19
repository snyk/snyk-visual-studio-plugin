using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snyk.VisualStudio.Extension.UI.Tree
{
    /// <summary>
    /// Root tree node for different types of issues (Open source, Code or IaC).
    /// </summary>
    public abstract class RootTreeNode : TreeNode
    {
        private RootTreeNodeState state;

        private IRefreshable tree;

        /// <summary>
        /// Initializes a new instance of the <see cref="RootTreeNode"/> class.
        /// </summary>
        /// <param name="tree">Refreshable reference.</param>
        public RootTreeNode(IRefreshable tree)
        {
            this.tree = tree;

            this.State = RootTreeNodeState.Disabled;
        }

        /// <summary>
        /// Gets or sets custom error suffix from Language Server.
        /// </summary>
        public string ErrorSuffix { get; set; }

        /// <summary>
        /// Gets or sets presentable error from Language Server.
        /// </summary>
        public Language.PresentableError PresentableError { get; set; }

        /// <inheritdoc/>
        public override string Title
        {
            get
            {
                string title = string.Empty;

                switch (this.state)
                {
                    case RootTreeNodeState.Enabled:
                        title = this.GetDefaultTitle();
                        break;
                    case RootTreeNodeState.DisabledForOrganization:
                        title = this.GetTitlePrefix() + " (disabled in Snyk.io)";
                        break;
                    case RootTreeNodeState.Scanning:
                        title = this.GetTitlePrefix() + " (scanning...)";
                        break;
                    case RootTreeNodeState.ResultDetails:
                        title = this.GetResultDetailsTitle();
                        break;
                    case RootTreeNodeState.Error:
                        var errorSuffix = string.IsNullOrEmpty(this.ErrorSuffix) ? "(error)" : this.ErrorSuffix;
                        title = this.GetTitlePrefix() + " " + errorSuffix;
                        break;
                    case RootTreeNodeState.NoFilesForSnykCodeScan:
                        title = this.GetTitlePrefix() + " (no supported code available)";
                        break;
                    case RootTreeNodeState.Disabled:
                    default:
                        title = this.GetTitlePrefix() + " (disabled)";
                        break;
                }

                return title;
            }
        }

        /// <inheritdoc/>
        public override bool IsRoot => true;

        /// <inheritdoc/>
        public override string FontWeight => "Bold";

        /// <summary>
        /// Gets a value indicating whether is root node enabled.
        /// </summary>
        public override bool Enabled => this.State == RootTreeNodeState.Enabled
            || this.State == RootTreeNodeState.Scanning
            || this.State == RootTreeNodeState.Error
            || this.State == RootTreeNodeState.ResultDetails;

        /// <summary>
        /// Gets or sets node state.
        /// </summary>
        public RootTreeNodeState State
        {
            get => this.state;
            set
            {
                if ((this.State == RootTreeNodeState.ResultDetails
                    || this.State == RootTreeNodeState.NoFilesForSnykCodeScan)
                    && value == RootTreeNodeState.Enabled)
                {
                    return;
                }

                this.state = value;

                this.tree.Refresh();
            }
        }

        /// <summary>
        /// Gets or sets total issues with critical severity.
        /// </summary>
        public int CriticalSeverityCount { get; set; }

        /// <summary>
        /// Gets or sets total issues with high severity.
        /// </summary>
        public int HighSeverityCount { get; set; }

        /// <summary>
        /// Gets or sets total issues with medium severity.
        /// </summary>
        public int MediumSeverityCount { get; set; }

        /// <summary>
        /// Gets or sets total issues with low severity.
        /// </summary>
        public int LowSeverityCount { get; set; }

        /// <summary>
        /// Gets a value indicating whether node content not empty.
        /// </summary>
        public bool HasContent => this.State == RootTreeNodeState.ResultDetails || this.State == RootTreeNodeState.Error;

        /// <summary>
        /// Set node text to {Prefix} without additional text.
        /// </summary>
        public void ResetTitleText() => this.Title = this.GetDefaultTitle();

        /// <summary>
        /// Clear all items in this node and title.
        /// </summary>
        public void Clean()
        {
            var baseBranchTreeNode = Items.SingleOrDefault(x => x is BaseBranchTreeNode);
            this.Items.Clear();
            if(baseBranchTreeNode != null)
                this.Items.Add(baseBranchTreeNode);
            this.Title = this.GetTitlePrefix();
        }

        /// <summary>
        /// Title text for open source root node.
        /// </summary>
        /// <returns>Title prefix string.</returns>
        protected abstract string GetTitlePrefix();

        /// <summary>
        /// Get issues type name (vulnerabilities for OSS or issues for SnykCode).
        /// </summary>
        /// <returns>Issues type name.</returns>
        protected abstract string GetIssuesTypeName();

        private string GetDefaultTitle() => this.GetTitlePrefix();

        private string GetResultDetailsTitle()
        {
            var titleBuilder = new StringBuilder(this.GetTitlePrefix());

            int totalIssuesCount = this.CriticalSeverityCount
                + this.HighSeverityCount
                + this.MediumSeverityCount
                + this.LowSeverityCount;

            titleBuilder
                .Append(" - ")
                .Append(string.Format("{0} {1}", totalIssuesCount, this.GetIssuesTypeName()));

            var severityDict = new Dictionary<string, int>
            {
                { "critical", this.CriticalSeverityCount },
                { "high", this.HighSeverityCount },
                { "medium", this.MediumSeverityCount},
                { "low", this.LowSeverityCount},
            };

            if (severityDict.Sum(pair => pair.Value) > 0)
            {
                titleBuilder.Append(":");

                foreach (var severityNameToIntPair in severityDict)
                {
                    if (severityNameToIntPair.Value <= 0)
                    {
                        continue;
                    }

                    titleBuilder.Append(string.Format(" {0} {1},", severityNameToIntPair.Value, severityNameToIntPair.Key));
                }

                titleBuilder = titleBuilder.Remove(titleBuilder.Length - 1, 1);
            }

            return titleBuilder.ToString();
        }
    }
}
