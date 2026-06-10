using System;
using System.IO;

namespace Snyk.VisualStudio.Extension.UI.Tree
{
    public class SecretsFileTreeNode : FileTreeNode
    {
        public SecretsFileTreeNode(TreeNode parent) : base(parent) { }

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

        public override string Icon
        {
            get
            {
                var fileExtension = Path.GetExtension(this.FileName);
                return SnykIconProvider.GetFileIconByExtension(fileExtension);
            }
        }
    }
}
