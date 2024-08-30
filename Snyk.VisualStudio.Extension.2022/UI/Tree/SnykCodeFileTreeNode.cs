using System.Collections.Generic;
using System.IO;
using Snyk.Code.Library.Domain.Analysis;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.UI.Tree
{
    /// <summary>
    /// SnykCode file tree node.
    /// </summary>
    public class SnykCodeFileTreeNode : TreeNode
    {
        /// <summary>
        /// Gets a value indicating whether title.
        /// </summary>
        public override string Title { get; set; }

        /// <summary>
        /// Gets a value indicating whether icon for node.
        /// </summary>
        public override string Icon
        {
            get
            {
                string fileExtension = Path.GetExtension(this.FileAnalysis.FileName);

                return SnykIconProvider.GetFileIconByExtension(fileExtension);
            }
        }

        /// <summary>
        /// Gets or sets SnykCode <see cref="FileAnalysis"/> object.
        /// </summary>
        public IEnumerable<FileAnalysis> FileAnalysis { get; set; }
    }
}
