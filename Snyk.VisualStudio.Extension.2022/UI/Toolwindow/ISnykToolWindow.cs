using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Seam over <see cref="SnykToolWindowControl"/> exposing only the members reached through
    /// <c>ISnykServiceProvider.ToolWindow</c>. Lets collaborators (the Language Server message
    /// handlers, the auth flow) be unit-tested with a mocked tool window instead of the WPF control.
    /// </summary>
    public interface ISnykToolWindow
    {
        /// <summary>The server-rendered issue tree panel.</summary>
        ITreeHtmlPanel TreeHtmlPanel { get; }

        /// <summary>The scan-summary panel.</summary>
        IHtmlPanel SummaryPanel { get; }

        /// <summary>Shows the issue detail panel for the given issue (tree-node click).</summary>
        void SelectedItemInTree(string issueId, string product);

        /// <summary>Brings the tool window to the foreground.</summary>
        void Show();

        Task UpdateScreenStateAsync();
    }
}
