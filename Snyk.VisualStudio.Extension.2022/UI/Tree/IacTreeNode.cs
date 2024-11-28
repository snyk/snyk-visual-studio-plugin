namespace Snyk.VisualStudio.Extension.UI.Tree
{
    /// <summary>
    /// SnykCode vulnerability tree node.
    /// </summary>
    public class IacTreeNode : IssueTreeNode
    {
        public IacTreeNode(TreeNode parent): base(parent)
        {
        }
        /// <summary>
        /// Gets a value indicating whether title.
        /// </summary>
        public override string Title => this.Issue.GetDisplayTitleWithLineNumber();

        /// <summary>
        /// Gets a value indicating whether icon for node.
        /// </summary>
        public override string Icon => SnykIconProvider.GetSeverityIcon(this.Issue.Severity);
    }
}
