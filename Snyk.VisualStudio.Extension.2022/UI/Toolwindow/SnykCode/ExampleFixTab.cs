namespace Snyk.VisualStudio.Extension.Shared.UI.Toolwindow.SnykCode
{
    using System.Collections.ObjectModel;
    using Snyk.Code.Library.Domain.Analysis;

    /// <summary>
    /// Domain object for Example fix tab.
    /// </summary>
    public class ExampleFixTab
    {
        /// <summary>
        /// Gets or sets a value for tab title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets a lines list.
        /// </summary>
        public ObservableCollection<FixLine> Lines { get; set; }
    }
}
