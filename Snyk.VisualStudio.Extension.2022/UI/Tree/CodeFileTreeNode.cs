using System;
using System.IO;

namespace Snyk.VisualStudio.Extension.UI.Tree
{
    /// <summary>
    /// SnykCode file tree node.
    /// </summary>
    public class CodeFileTreeNode : FileTreeNode
    {
        /// <summary>
        /// Gets a value indicating whether title.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether icon for node.
        /// </summary>
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
