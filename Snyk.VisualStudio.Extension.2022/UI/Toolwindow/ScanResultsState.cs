namespace Snyk.VisualStudio.Extension.Shared.UI.Toolwindow
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
        public static ScanResultsState Instance { get; } = new ScanResultsState();

        /// <summary>
        /// Display results grid, tree and enable execute actions.
        /// </summary>
        public override void DisplayComponents() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.ToolWindowControl.messagePanel.ShowSelectIssueMessage();

            this.ToolWindowControl.messagePanel.Visibility = Visibility.Visible;

            await this.ToolWindowControl.UpdateActionsStateAsync();
        });

        /// <summary>
        /// Hide results grid, tree and disable execute actions.
        /// </summary>
        public override void HideComponents() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.ToolWindowControl.messagePanel.Visibility = Visibility.Collapsed;

            await this.ToolWindowControl.UpdateActionsStateAsync();
        });
    }
}
