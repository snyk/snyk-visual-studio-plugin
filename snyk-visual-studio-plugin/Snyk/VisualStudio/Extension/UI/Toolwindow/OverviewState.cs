namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    using System.Windows;

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
        public override void DisplayComponents()
        {
            this.ToolWindowControl.Dispatcher.Invoke(() =>
            {
                this.ToolWindowControl.overviewGrid.Visibility = Visibility.Visible;
            });

            this.ToolWindowControl.DisableAllActions();
        }

        /// <summary>
        /// Hide overview grid component.
        /// </summary>
        public override void HideComponents()
        {
            this.ToolWindowControl.Dispatcher.Invoke(() =>
            {
                this.ToolWindowControl.overviewGrid.Visibility = Visibility.Collapsed;
            });
        }
    }
}
