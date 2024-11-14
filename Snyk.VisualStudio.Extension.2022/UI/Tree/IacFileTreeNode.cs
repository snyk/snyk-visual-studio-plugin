using System;
using System.IO;

namespace Snyk.VisualStudio.Extension.UI.Tree
{
    /// <summary>
    /// SnykIac file tree node.
    /// </summary>
    public class IacFileTreeNode : FileTreeNode
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
