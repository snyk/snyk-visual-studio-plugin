namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Seam for the WebView2-backed issue-tree panel. Lets the Language Server message handlers
    /// (<c>$/snyk.treeView</c>) be unit-tested without constructing the WPF control.
    /// </summary>
    public interface ITreeHtmlPanel
    {
        /// <summary>
        /// Total issue count for the currently rendered tree. Set together with the HTML by
        /// <see cref="SetContent"/> and cleared by <see cref="Reset"/>; read by the tool window to
        /// gate the "Clean" command. On the interface so the count can be driven/observed through
        /// the seam rather than only via the concrete control.
        /// </summary>
        int TotalIssues { get; set; }

        /// <summary>
        /// Renders the LS-provided tree HTML and records the total issue count in a single,
        /// race-free update. Used by both the push (<c>$/snyk.treeView</c>) and pull
        /// (<c>snyk.getTreeView</c>) paths.
        /// </summary>
        void SetContent(string html, int totalIssues);

        /// <summary>
        /// Fetches the current tree HTML from the Language Server via <c>snyk.getTreeView</c> and
        /// renders it. Called when the LS signals ready; on the interface so the initial-fetch path
        /// is routed through the seam.
        /// </summary>
        void RequestInitialTree();

        /// <summary>
        /// Clears the panel and invalidates any in-flight <see cref="SetContent"/> navigations.
        /// Must be called on the UI thread.
        /// </summary>
        void Reset();
    }
}
