using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace Snyk.VisualStudio.Extension.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SnykOpenSettingsCommand : AbstractSnykCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykOpenSettingsCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SnykOpenSettingsCommand(AsyncPackage package, OleMenuCommandService commandService) : base(package, commandService)
        {
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SnykOpenSettingsCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in SnykOpenSettingsCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new SnykOpenSettingsCommand(package, commandService);
        }

        protected override void Execute(object sender, EventArgs eventArgs) => VsPackage.ShowOptionPage();

        protected override int GetCommandId() => SnykExtension.Guids.OptionsCommandId;
    }
}
