namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Implements empty state for tool window.
    /// </summary>
    public class EmptyState : ToolWindowState
    {
        /// <summary>
        /// Gets a value indicating whether new <see cref="EmptyState"/>.
        /// </summary>
        public static EmptyState Instance { get; } = new EmptyState();

        /// <summary>
        /// This method does nothing.
        /// </summary>
        public override void DisplayComponents()
        {
        }

        /// <summary>
        /// This method does nothing.
        /// </summary>
        public override void HideComponents()
        {
        }
    }
}
