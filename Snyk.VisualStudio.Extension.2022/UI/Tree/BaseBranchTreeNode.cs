using Snyk.VisualStudio.Extension.Theme;

namespace Snyk.VisualStudio.Extension.UI.Tree
{
    public class BaseBranchTreeNode : TreeNode
    {
        public override string Icon =>
            ThemeInfo.IsDarkTheme()
                ? SnykIconProvider.DarkThemeBranchIconPath
                : SnykIconProvider.LightThemeBranchIconPath;
    }
}
