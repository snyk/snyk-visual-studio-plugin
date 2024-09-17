using System;
using System.Collections.Generic;
using System.IO;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.UI.Tree
{
    /// <summary>
    /// SnykIac file tree node.
    /// </summary>
    public class SnykIacFileTreeNode : TreeNode
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

        public string FolderName { get; set; }
        public string FileName { get; set; }

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

        /// <summary>
        /// Gets or sets SnykCode <see cref="IssueList"/> object.
        /// </summary>
        public IEnumerable<Issue> IssueList { get; set; }
    }
}
