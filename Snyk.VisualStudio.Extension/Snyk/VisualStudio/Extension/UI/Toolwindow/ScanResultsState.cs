namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    using System.Windows;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Implements Scan results state for tool window.
    /// </summary>
    public class ScanResultsState : ToolWindowState
    {
        /// <summary>
        /// Gets a value indicating whether new <see cref="ScanResultsState"/> object.
        /// </summary>
        public static ScanResultsState Instance => new ScanResultsState();

        /// <summary>
        /// Display results grid, tree and enable execute actions.
        /// </summary>
        public override void DisplayComponents() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.ToolWindowControl.selectIssueMessageGrid.Visibility = Visibility.Visible;

            this.ToolWindowControl.resultsGrid.Visibility = Visibility.Visible;
        });

        /// <summary>
        /// Hide results grid, tree and disable execute actions.
        /// </summary>
        public override void HideComponents() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.ToolWindowControl.resultsGrid.Visibility = Visibility.Collapsed;

            this.ToolWindowControl.HideIssueMessages();
        });
    }
}
