namespace Snyk.VisualStudio.Extension.Shared.Commands
{
    using System;
    using System.ComponentModel.Design;
    using Microsoft.VisualStudio.Shell;
    using Snyk.VisualStudio.Extension.Shared.UI.Notifications;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Command handler.
    /// </summary>
    internal sealed class SnykCleanPanelCommand : AbstractSnykCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCleanPanelCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file).
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SnykCleanPanelCommand(AsyncPackage package, OleMenuCommandService commandService)
            : base(package, commandService)
        {
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SnykCleanPanelCommand Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in SnykCleanPanelCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

            Instance = new SnykCleanPanelCommand(package, commandService);
        }

        public override void UpdateState() =>
            this.MenuCommand.Enabled = Common.Guid.IsValid(SnykVSPackage.ServiceProvider.Options.ApiToken)
                && this.VsPackage.ToolWindowControl.IsTreeContentNotEmpty();

        /// <summary>
        /// Run clean of tool window content.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        protected override void Execute(object sender, EventArgs eventArgs) => this.VsPackage.ToolWindowControl.Clean();

        /// <summary>
        /// Get command Id.
        /// </summary>
        /// <returns>Command Id.</returns>
        protected override int GetCommandId() => SnykGuids.CleanCommandId;

        /// <inheritdoc/>
        protected override void OnBeforeQueryStatus(object sender, EventArgs e) => this.UpdateState();
    }
}