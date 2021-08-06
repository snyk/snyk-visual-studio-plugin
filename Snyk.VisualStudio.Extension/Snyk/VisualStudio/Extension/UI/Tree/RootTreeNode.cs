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
        /// <summary>
        /// Initializes a new instance of the <see cref="RootTreeNode"/> class.
        /// </summary>
        public RootTreeNode() => this.Title = this.GetTitlePrefix();

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
                .Append(string.Format("{0} vulnerabilities", totalIssuesCount));

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
    }
}
