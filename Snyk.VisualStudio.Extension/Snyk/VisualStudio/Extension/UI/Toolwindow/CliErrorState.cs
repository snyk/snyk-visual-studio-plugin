namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    using System.Windows;
    using Microsoft.VisualStudio.Shell;
    using Snyk.VisualStudio.Extension.CLI;

    /// <summary>
    /// Implements error state for tool window.
    /// </summary>
    public class CliErrorState : ToolWindowState
    {
        private CliError cliError;

        /// <summary>
        /// Initializes a new instance of the <see cref="CliErrorState"/> class.
        /// </summary>
        /// <param name="cliError"><see cref="CliError"/> object.</param>
        public CliErrorState(CliError cliError) => this.cliError = cliError;

        /// <summary>
        /// Create new instance of <see cref="CliErrorState"/>.
        /// </summary>
        /// <param name="cliError">Cli Error object.</param>
        /// <returns>new ErrorState.</returns>
        public static CliErrorState Instance(CliError cliError) => new CliErrorState(cliError);

        /// <summary>
        /// Hide tool window error panel.
        /// </summary>
        public override void HideComponents()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.ToolWindowControl.VulnerabilitiesTree.CliRootNode.ResetTitleText();
            });
        }

        /// <summary>
        /// Show tool window error panel and enable all execute actions.
        /// </summary>
        public override void DisplayComponents()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.ToolWindowControl.VulnerabilitiesTree.CliRootNode.State = Tree.RootTreeNodeState.Error;
            });
        }
    }
}
