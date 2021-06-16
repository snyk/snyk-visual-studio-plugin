namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    using CLI;
    using System.Windows;

    /// <summary>
    /// Implements error state for tool window.
    /// </summary>
    public class ErrorState : ToolWindowState
    {

        private CliError cliError;

        /// <summary>
        /// Create new instance of <see cref="ErrorState"/>.
        /// </summary>
        /// <param name="cliError">Cli Error object.</param>
        /// <returns>new ErrorState.</returns>
        public static ErrorState Instance(CliError cliError) => new ErrorState(cliError);

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorState"/> class.
        /// </summary>
        /// <param name="cliError"><see cref="CliError"/> object.</param>
        public ErrorState(CliError cliError) => this.cliError = cliError;

        /// <summary>
        /// Hide tool window error panel.
        /// </summary>
        public override void HideComponents() =>
            this.ToolWindowControl.Dispatcher.Invoke(() => this.ToolWindowControl.errorPanel.Visibility = Visibility.Collapsed);

        /// <summary>
        /// Show tool window error panel and enable all execute actions.
        /// </summary>
        public override void DisplayComponents()
        {
            this.ToolWindowControl.EnableExecuteActions();

            this.ToolWindowControl.DisplayError(this.cliError);
        }
    }
}
