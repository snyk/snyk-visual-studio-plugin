namespace Snyk.VisualStudio.Extension.UI.Tree
{

    /// <summary>
    /// SnykCode Security root tree node.
    /// </summary>
    public class SnykCodeSecurityRootTreeNode : RootTreeNode
    {
        /// <summary>
        /// Title text for open source root node.
        /// </summary>
        public const string SnykCodeTitle = "Code Security";

        public SnykCodeSecurityRootTreeNode(IRefreshable parent)
            : base(parent)
        {
        }

        /// <summary>
        /// Gets a value indicating whether icon for open source node.
        /// </summary>
        public string Icon => SnykIconProvider.SnykCodeIconPath;

        protected override string GetIssuesTypeName() => "issues";

        protected override string GetTitlePrefix() => SnykCodeTitle;
    }
}
