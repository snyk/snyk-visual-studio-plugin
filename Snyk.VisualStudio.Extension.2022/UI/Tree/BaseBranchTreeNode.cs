using Snyk.VisualStudio.Extension.Theme;

namespace Snyk.VisualStudio.Extension.UI.Tree
{
    public class BaseBranchTreeNode : TreeNode
    {
        public BaseBranchTreeNode(TreeNode parent)
        {
            this.Parent = parent;
        }

        public override string Icon =>
            ThemeInfo.IsDarkTheme()
                ? SnykIconProvider.DarkThemeBranchIconPath
                : SnykIconProvider.LightThemeBranchIconPath;
    }
}
