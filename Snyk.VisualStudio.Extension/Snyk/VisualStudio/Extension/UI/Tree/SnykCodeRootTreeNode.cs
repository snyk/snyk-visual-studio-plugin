namespace Snyk.VisualStudio.Extension.UI.Tree
{

    /// <summary>
    /// SnykCode root tree node.
    /// </summary>
    public class SnykCodeRootTreeNode : RootTreeNode
    {
        /// <summary>
        /// Title text for open source root node.
        /// </summary>
        public const string SnykCodeTitle = "SnykCode";

        /// <summary>
        /// Gets a value indicating whether icon for open source node.
        /// </summary>
        public string Icon => SnykIconProvider.SnykCodeIconPath;

        protected override string GetTitlePrefix() => SnykCodeTitle;
    }
}
