namespace Snyk.VisualStudio.Extension.Shared.UI.Toolwindow
{
    /// <summary>
    /// Represents tool window context.
    /// </summary>
    public class ToolWindowContext
    {
        private SnykToolWindowControl toolWindowControl;

        private ToolWindowState state = EmptyState.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindowContext"/> class.
        /// </summary>
        /// <param name="control">Tool window control.</param>
        public ToolWindowContext(SnykToolWindowControl control) => this.toolWindowControl = control;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindowContext"/> class.
        /// </summary>
        /// <param name="control">Tool window control.</param>
        /// <param name="state">Initial state.</param>
        public ToolWindowContext(SnykToolWindowControl control, ToolWindowState state) : this(control) => this.TransitionTo(state);

        /// <summary>
        /// Gets a value indicating whether ToolWindowControl.
        /// </summary>
        public SnykToolWindowControl ToolWindowControl => toolWindowControl;

        /// <summary>
        /// Transition context state to new. It will call HideComponents() for previous state and DisplayComponents() for new state.
        /// </summary>
        /// <param name="state">New state.</param>
        public void TransitionTo(ToolWindowState state)
        {
            if (this.state == state)
            {
                return;
            }

            this.state.HideComponents();

            this.state = state;

            this.state.Context = this;

            this.state.DisplayComponents();
        }

        /// <summary>
        /// Request update UI for current state. Call DisplayComponents() for current state.
        /// </summary>
        public void RequestUpdateUI() => this.state.DisplayComponents();

        /// <summary>
        /// Check is current state is <see cref="EmptyState"/>.
        /// </summary>
        /// <returns>True if current state type is EmptyState.</returns>
        public bool IsEmptyState() => this.state.GetType() == typeof(EmptyState);
    }
}
