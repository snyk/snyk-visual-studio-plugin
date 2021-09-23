namespace Snyk.VisualStudio.Extension.Commands
{
    using System;
    using System.ComponentModel.Design;
    using Microsoft.VisualStudio.Shell;
    using Snyk.VisualStudio.Extension.CLI;
    using Snyk.VisualStudio.Extension.Service;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Command handler.
    /// </summary>
    internal sealed class SnykScanCommand : AbstractSnykCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykScanCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file).
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SnykScanCommand(AsyncPackage package, OleMenuCommandService commandService)
            : base(package, commandService)
        {
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SnykScanCommand Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// /// <returns>Task.</returns>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in SnykScanCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new SnykScanCommand(package, commandService);
        }

        /// <summary>
        /// Run scan.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        protected override void Execute(object sender, EventArgs eventArgs) => SnykTasksService.Instance.Scan();

        /// <summary>
        /// Get command Id.
        /// </summary>
        /// <returns>Id int.</returns>
        protected override int GetCommandId() => SnykGuids.RunScanCommandId;

        /// <inheritdoc/>
        protected override void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;

            if (menuCommand != null)
            {
                menuCommand.Enabled = SnykSolutionService.Instance.IsSolutionOpen && !SnykTasksService.Instance.IsScanRunning();
            }
        }
    }
}
