namespace Snyk.VisualStudio.Extension.UI.Tree
{
    /// <summary>
    /// Vulnerability tree node.
    /// </summary>
    public class TreeNode
    {

        /// <summary>
        /// Title for this node.
        /// </summary>
        protected string title;

        /// <summary>
        /// Gets or sets a value indicating whether title.
        /// </summary>
        public virtual string Title
        {
            get
            {
                return this.title;
            }

            set
            {
                this.title = value;
            }
        }
    }
}
