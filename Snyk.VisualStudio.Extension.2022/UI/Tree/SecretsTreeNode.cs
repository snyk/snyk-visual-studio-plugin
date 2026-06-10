namespace Snyk.VisualStudio.Extension.UI.Tree
{
    public class SecretsTreeNode : IssueTreeNode
    {
        public SecretsTreeNode(TreeNode parent) : base(parent) { }

        public override string Title => this.Issue.GetDisplayTitleWithLineNumber();

        public override string Icon => SnykIconProvider.GetSeverityIcon(this.Issue.Severity);
    }
}
