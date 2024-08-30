using Snyk.VisualStudio.Extension.Language;
using System.Collections.ObjectModel;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow.SnykCode
{
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
        public ObservableCollection<LineData> Lines { get; set; }
    }
}
