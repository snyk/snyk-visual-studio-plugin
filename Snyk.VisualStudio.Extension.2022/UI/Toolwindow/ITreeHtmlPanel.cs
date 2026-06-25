namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Seam for the WebView2-backed issue-tree panel. Lets the Language Server message handlers
    /// (<c>$/snyk.treeView</c>) be unit-tested without constructing the WPF control.
    /// </summary>
    public interface ITreeHtmlPanel
    {
        /// <summary>
        /// Renders the LS-provided tree HTML and records the total issue count in a single,
        /// race-free update. Used by both the push (<c>$/snyk.treeView</c>) and pull
        /// (<c>snyk.getTreeView</c>) paths.
        /// </summary>
        void SetContent(string html, int totalIssues);

        /// <summary>
        /// Clears the panel and invalidates any in-flight <see cref="SetContent"/> navigations.
        /// Must be called on the UI thread.
        /// </summary>
        void Reset();
    }
}
