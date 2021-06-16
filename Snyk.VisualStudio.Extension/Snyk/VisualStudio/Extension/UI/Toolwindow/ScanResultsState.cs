namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    using System.Windows;

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
        public override void DisplayComponents()
        {
            this.ToolWindowControl.Dispatcher.Invoke(() =>
            {
                this.ToolWindowControl.EnableExecuteActions();

                this.ToolWindowControl.selectIssueMessageGrid.Visibility = Visibility.Visible;

                this.ToolWindowControl.resultsGrid.Visibility = Visibility.Visible;
            });
        }

        /// <summary>
        /// Hide results grid, tree and disable execute actions.
        /// </summary>
        public override void HideComponents()
        {
            this.ToolWindowControl.CleanVulnerabilitiesTree();

            this.ToolWindowControl.Dispatcher.Invoke(() =>
            {
                this.ToolWindowControl.resultsGrid.Visibility = Visibility.Collapsed;

                this.ToolWindowControl.HideIssueMessages();

                this.ToolWindowControl.CleanAndHideVulnerabilityDetailsPanel();
            });
        }
    }
}
