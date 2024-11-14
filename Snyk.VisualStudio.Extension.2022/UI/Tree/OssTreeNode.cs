namespace Snyk.VisualStudio.Extension.UI.Tree
{
    public class OssTreeNode : IssueTreeNode
    {
        public override string Title => this.Issue.GetPackageNameTitle();

        /// <summary>
        /// Gets a value indicating whether icon for node.
        /// If it's parent node it will get package manager icon.
        /// If it's leaf node it will display vulnerability severity icon.
        /// </summary>
        public override string Icon => SnykIconProvider.GetSeverityIcon(this.Issue.Severity);
    }
}
