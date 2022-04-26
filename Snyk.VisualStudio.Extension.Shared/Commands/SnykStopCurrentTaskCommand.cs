namespace Snyk.VisualStudio.Extension.Shared.Commands
{
    using System;
    using System.ComponentModel.Design;
    using Microsoft.VisualStudio.Shell;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Command handler.
    /// </summary>
    internal sealed class SnykStopCurrentTaskCommand : AbstractTaskCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykStopCurrentTaskCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file).
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SnykStopCurrentTaskCommand(AsyncPackage package, OleMenuCommandService commandService)
            : base(package, commandService)
        {
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SnykStopCurrentTaskCommand Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// /// <returns>Task.</returns>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in SnykStopCurrentTaskCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new SnykStopCurrentTaskCommand(package, commandService);
        }

        /// <inheritdoc/>
        public override void UpdateState() => ThreadHelper.JoinableTaskFactory.RunAsync(this.UpdateStateAsync);

        /// <inheritdoc/>
        public override async Task UpdateStateAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            this.MenuCommand.Enabled = this.IsButtonAvailable() && !SnykScanCommand.Instance.Enabled;
        }

        /// <summary>
        /// Cancel current running task.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        protected override void Execute(object sender, EventArgs eventArgs) => SnykTasksService.Instance.CancelTasks();

        /// <summary>
        /// Get command Id.
        /// </summary>
        /// <returns>Stop Command Id.</returns>
        protected override int GetCommandId() => SnykGuids.StopCommandId;
    }
}