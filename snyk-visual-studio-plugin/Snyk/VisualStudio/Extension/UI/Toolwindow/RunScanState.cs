namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Implement Run scan state for tool window.
    /// </summary>
    public class RunScanState : ToolWindowState
    {
        /// <summary>
        /// Gets a value indicating whether new <see cref="RunScanState"/> object.
        /// </summary>
        public static RunScanState Instance => new RunScanState();

        /// <summary>
        /// Hide run scan message and disable stop actions.
        /// </summary>
        public override void HideComponents()
        {
            this.ToolWindowControl.HideRunScanMessage();

            this.ToolWindowControl.EnableStopActions();
        }

        /// <summary>
        /// Display run scan message and enable stop actions.
        /// </summary>
        public override void DisplayComponents()
        {
            this.ToolWindowControl.DisplayRunScanMessage();

            this.ToolWindowControl.EnableExecuteActions();
        }
    }
}
