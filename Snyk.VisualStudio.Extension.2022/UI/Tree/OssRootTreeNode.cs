namespace Snyk.VisualStudio.Extension.UI.Tree
{
    /// <summary>
    /// Open source root node for vulnerability tree.
    /// </summary>
    public class OssRootTreeNode : RootTreeNode
    {
        /// <summary>
        /// Title text for open source root node.
        /// </summary>
        public const string OpenSourceSecurityTitle = "Open Source Security";

        public OssRootTreeNode(IRefreshable tree)
            : base(tree)
        {
        }

        /// <summary>
        /// Gets a value indicating whether icon for open source node.
        /// </summary>
        public override string Icon => SnykIconProvider.OpenSourceSecurityIconPath;

        protected override string GetIssuesTypeName() => "vulnerabilities";

        protected override string GetTitlePrefix() => OpenSourceSecurityTitle;
    }
}
