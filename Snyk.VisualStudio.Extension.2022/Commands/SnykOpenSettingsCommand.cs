namespace Snyk.VisualStudio.Extension.Commands
{
    using System;
    using System.ComponentModel.Design;
    using Microsoft.VisualStudio.Shell;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Command handler.
    /// </summary>
    internal sealed class SnykOpenSettingsCommand : AbstractSnykCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykOpenSettingsCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file).
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SnykOpenSettingsCommand(AsyncPackage package, OleMenuCommandService commandService)
            : base(package, commandService)
        {
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SnykOpenSettingsCommand Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <returns>Task.</returns>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in SnykOpenSettingsCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

            Instance = new SnykOpenSettingsCommand(package, commandService);
        }

        public override Task UpdateStateAsync() => Task.CompletedTask;

        /// <summary>
        /// Open Snyk HTML Settings window.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        protected override void Execute(object sender, EventArgs eventArgs)
        {
            base.Execute(sender, eventArgs);

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var serviceProvider = SnykVSPackage.ServiceProvider;
                var options = serviceProvider.Options;
                var languageServerRpc = serviceProvider.LanguageClientManager?.Rpc;
                var optionsManager = serviceProvider.SnykOptionsManager;

                using (Settings.DpiContextScope.EnterUnawareGdiScaled())
                {
                    var settingsWindow = new Settings.HtmlSettingsWindow(
                        options,
                        languageServerRpc,
                        optionsManager,
                        serviceProvider);

                    var hlp = new System.Windows.Interop.WindowInteropHelper(settingsWindow);
                    hlp.Owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                    settingsWindow.Show();
                }
            });
        }

        /// <summary>
        /// Get command Id.
        /// </summary>
        /// <returns>Command Id.</returns>
        protected override int GetCommandId() => SnykGuids.OptionsCommandId;

        /// <inheritdoc/>
        protected override void OnBeforeQueryStatus(object sender, EventArgs e)
        {
        }
    }
}
