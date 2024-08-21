namespace Snyk.VisualStudio.Extension.UI.Tree
{

    /// <summary>
    /// SnykCode Quality root tree node.
    /// </summary>
    public class SnykCodeQualityRootTreeNode : RootTreeNode
    {
        /// <summary>
        /// Title text for open source root node.
        /// </summary>
        public const string SnykCodeTitle = "Code Quality";

        public SnykCodeQualityRootTreeNode(IRefreshable parent)
            : base(parent)
        {
        }

        /// <summary>
        /// Gets a value indicating whether icon for open source node.
        /// </summary>
        public override string Icon => SnykIconProvider.SnykCodeIconPath;

        protected override string GetIssuesTypeName() => "issues";

        protected override string GetTitlePrefix() => SnykCodeTitle;
    }
}
