namespace Snyk.VisualStudio.Extension.Shared.UI.Tree
{
    using System.IO;
    using Snyk.Code.Library.Domain.Analysis;

    /// <summary>
    /// SnykCode file tree node.
    /// </summary>
    public class SnykCodeFileTreeNode : TreeNode
    {
        /// <summary>
        /// Gets a value indicating whether title.
        /// </summary>
        public override string Title => this.FileAnalysis.FileName;

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
        public FileAnalysis FileAnalysis { get; set; }
    }
}
