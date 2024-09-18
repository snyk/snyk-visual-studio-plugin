using System.Windows;
using Microsoft.VisualStudio.Shell;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Implements Overview state for tool window.
    /// </summary>
    public class InitializingState : ToolWindowState
    {
        /// <summary>
        /// Gets a value indicating whether new <see cref="OverviewState"/> object.
        /// </summary>
        public static InitializingState Instance { get; } = new InitializingState();

        /// <summary>
        /// Display overview grid component.
        /// </summary>
        public override void DisplayComponents() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.ToolWindowControl.messagePanel.Visibility = Visibility.Visible;

            this.ToolWindowControl.messagePanel.ShowInitializingScreenMessage();

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
