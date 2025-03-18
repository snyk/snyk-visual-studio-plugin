using System;
using System.IO;
using System.Linq;

namespace Snyk.VisualStudio.Extension.UI.Tree
{
    public class OssFileTreeNode : FileTreeNode
    {
        public OssFileTreeNode(TreeNode parent) : base(parent) { }

        public override string Title
        {
            get
            {
                var mainDirUri = new Uri(FolderName);
                var fileUri = new Uri(FileName);
                var relativeUri = mainDirUri.MakeRelativeUri(fileUri);
                return Uri.UnescapeDataString(relativeUri.ToString()).Replace("/", "\\");
            }
        }

        public override string Icon => SnykIconProvider.GetPackageManagerIcon(this.PackageManager);
        private string PackageManager => this.IssueList.FirstOrDefault()?.AdditionalData?.PackageManager ?? "";
    }
}
