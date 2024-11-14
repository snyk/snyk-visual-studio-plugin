using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.UI.Tree;

public class IssueTreeNode : TreeNode
{
    public virtual Issue Issue { get; set; }
}