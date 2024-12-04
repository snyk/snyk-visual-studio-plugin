using System.IO;
using System.Linq;

namespace Snyk.VisualStudio.Extension.UI.Tree
{
    public class OssFileTreeNode : FileTreeNode
    {
        public OssFileTreeNode(TreeNode parent) : base(parent) { }
        public override string Title => this.ProjectName.Replace("/", "\\") + "\\" + this.DisplayTargetFile;
        public override string Icon => SnykIconProvider.GetPackageManagerIcon(this.PackageManager);
        private string ProjectName => this.IssueList.FirstOrDefault()?.AdditionalData?.ProjectName ?? "";
        private string PackageManager => this.IssueList.FirstOrDefault()?.AdditionalData?.PackageManager ?? "";
        private string DisplayTargetFile => Path.GetFileName(this.IssueList.FirstOrDefault()?.AdditionalData?.DisplayTargetFile ?? "");
    }
}
