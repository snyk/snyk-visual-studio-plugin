namespace Snyk.VisualStudio.Extension.UI.Tree
{

    /// <summary>
    /// IaC root tree node.
    /// </summary>
    public class SnykIacRootTreeNode : RootTreeNode
    {
        /// <summary>
        /// Title text for open source root node.
        /// </summary>
        public const string SnykIacTitle = "Configuration";

        public SnykIacRootTreeNode(IRefreshable tree)
            : base(tree)
        {
        }

        /// <summary>
        /// Gets a value indicating whether icon for open source node.
        /// </summary>
        public override string Icon => SnykIconProvider.SnykIacIconPath;

        protected override string GetIssuesTypeName() => "issues";

        protected override string GetTitlePrefix() => SnykIacTitle;
    }
}
