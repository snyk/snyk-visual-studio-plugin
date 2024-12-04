using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.UI.Tree;

public class IssueTreeNode : TreeNode
{
    public IssueTreeNode(TreeNode parent)
    {
        this.Parent = parent;
    }

    public virtual Issue Issue { get; set; }
}