namespace Snyk.VisualStudio.Extension.UI.Toolwindow
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

            this.ToolWindowControl.overviewGrid.Visibility = Visibility.Visible;
        });

        /// <summary>
        /// Hide overview grid component.
        /// </summary>
        public override void HideComponents() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.ToolWindowControl.overviewGrid.Visibility = Visibility.Collapsed;
        });
    }
}
