namespace Snyk.VisualStudio.Extension.UI.Tree
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Root tree node for different types of issues (Open source, security or quality).
    /// </summary>
    public abstract class RootTreeNode : TreeNode
    {
        private bool enabled;

        private IRefreshable parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="RootTreeNode"/> class.
        /// <para name="parent">Refreshable reference</para>
        /// </summary>
        public RootTreeNode(IRefreshable parent)
        {
            this.parent = parent;

            base.Title = this.GetDefaultDisabledTitle();
        }

        /// <inheritdoc/>
        public override string Title
        {
            set
            {
                if (this.enabled)
                {
                    base.Title = value;
                }
                else
                {
                    base.Title = this.GetDefaultDisabledTitle();
                }

                this.parent.Refresh();
            }
        }

        /// <inheritdoc/>
        public override bool IsRoot => true;

        /// <summary>
        /// Gets or sets a value indicating whether is root node enabled.
        /// </summary>
        public bool Enabled
        {
            get => this.enabled;
            set
            {
                this.enabled = value;

                if (this.enabled)
                {
                    this.Title = this.GetDefaultTitle();
                } 
                else
                {
                    this.Title = this.GetDefaultDisabledTitle();
                }
            }
        }

        /// <summary>
        /// Set detailed title (with information by severity).
        /// </summary>
        /// <param name="criticalSeverityCount">Total issues with critical severity.</param>
        /// <param name="highSeverityCount">Total issues with high severity.</param>
        /// <param name="mediumSeverityCount">Total issues with medium severity.</param>
        /// <param name="lowSeverityCount">Total issues with low severity.</param>
        public void SetDetails(
            int criticalSeverityCount,
            int highSeverityCount,
            int mediumSeverityCount,
            int lowSeverityCount)
        {
            var titleBuilder = new StringBuilder(this.GetTitlePrefix());

            int totalIssuesCount = criticalSeverityCount + highSeverityCount + mediumSeverityCount + lowSeverityCount;

            titleBuilder
                .Append(" - ")
                .Append(string.Format("{0} {1}", totalIssuesCount, this.GetIssuesTypeName()));

            var severityDict = new Dictionary<string, int>
                {
                    { "critical" , criticalSeverityCount },
                    { "high" , highSeverityCount },
                    { "medium" , mediumSeverityCount},
                    { "low" , lowSeverityCount},
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

                    titleBuilder.Append(string.Format(" {0} {1} |", severityNameToIntPair.Value, severityNameToIntPair.Key));
                }

                titleBuilder = titleBuilder.Remove(titleBuilder.Length - 2, 2);
            }

            this.Title = titleBuilder.ToString();
        }

        /// <summary>
        /// Set node text to {Prefix} + (scanninng...).
        /// </summary>
        public void SetScanningTitle() => this.Title = this.GetTitlePrefix() + " (scanning...)";

        /// <summary>
        /// Set node text to {Prefix} + (error).
        /// </summary>
        public void SetErrorTitle() => this.Title = this.GetTitlePrefix() + " (error)";

        /// <summary>
        /// Set node text to {Prefix} without additional text.
        /// </summary>
        public void ResetTitleText() => this.Title = this.GetDefaultTitle();

        /// <summary>
        /// Clear all items in this node and title.
        /// </summary>
        public void Clean()
        {
            this.Items.Clear();

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

        private string GetDefaultDisabledTitle() => this.GetTitlePrefix() + " (disabled)";
    }
}
