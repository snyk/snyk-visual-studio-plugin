namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Abstract class for tool window state.
    /// </summary>
    public abstract class ToolWindowState
    {
        /// <summary>
        /// Gets or sets a value indicating whether tool window context.
        /// </summary>
        public ToolWindowContext Context { get; set; }

        /// <summary>
        /// Gets a value indicating whether tool window control.
        /// </summary>
        public SnykToolWindowControl ToolWindowControl => this.Context.ToolWindowControl;

        /// <summary>
        /// Hide components for previous state.
        /// </summary>
        public abstract void HideComponents();

        /// <summary>
        /// Display components for next state.
        /// </summary>
        public abstract void DisplayComponents();
    }
}
