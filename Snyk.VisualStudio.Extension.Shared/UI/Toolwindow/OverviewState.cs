namespace Snyk.VisualStudio.Extension.Shared.UI.Toolwindow
{
    using System.Windows;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Implements Overview state for tool window.
    /// </summary>
    public class OverviewState : ToolWindowState
    {
        /// <summary>
        /// Gets a value indicating whether new <see cref="OverviewState"/> object.
        /// </summary>
        public static OverviewState Instance => new OverviewState();

        /// <summary>
        /// Display overview grid component.
        /// </summary>
        public override void DisplayComponents() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.ToolWindowControl.messagePanel.Visibility = Visibility.Visible;

            this.ToolWindowControl.messagePanel.ShowOverviewScreenMessage();

            await this.ToolWindowControl.UpdateActionsStateAsync();
        });

        /// <summary>
        /// Hide overview grid component.
        /// </summary>
        public override void HideComponents() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.ToolWindowControl.messagePanel.Visibility = Visibility.Collapsed;

            await this.ToolWindowControl.UpdateActionsStateAsync();
        });
    }
}
