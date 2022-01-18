namespace Snyk.VisualStudio.Extension.Shared.Commands
{
    using System;
    using System.ComponentModel.Design;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Abstract class for Snyk commands.
    /// </summary>
    public abstract class AbstractSnykCommand
    {
        /// <summary>
        /// Package instance.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractSnykCommand"/> class.
        /// </summary>
        /// <param name="package">Extension package.</param>
        /// <param name="commandService">Command service.</param>
        protected AbstractSnykCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(this.GetCommandSet(), this.GetCommandId());

            this.MenuCommand = new OleMenuCommand(this.Execute, menuCommandID);

            commandService.AddCommand(this.MenuCommand);

            this.MenuCommand.BeforeQueryStatus += this.OnBeforeQueryStatus;
        }

        /// <summary>
        /// Gets or sets menu command.
        /// </summary>
        protected OleMenuCommand MenuCommand { get; set; }

        /// <summary>
        /// Gets a value indicating whether VS package.
        /// </summary>
        protected SnykVSPackage VsPackage => this.package as SnykVSPackage;

        /// <summary>
        /// Update command state.
        /// </summary>
        public abstract void UpdateState();

        /// <summary>
        /// Get command set GUID.
        /// </summary>
        /// <returns>Guid.</returns>
        protected Guid GetCommandSet() => SnykGuids.SnykVSPackageCommandSet;

        /// <summary>
        /// Abstract method for execute command.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        protected abstract void Execute(object sender, EventArgs eventArgs);

        /// <summary>
        /// Get command Id.
        /// </summary>
        /// <returns>Command id.</returns>
        protected abstract int GetCommandId();

        /// <summary>
        /// On before query status event handler for button (to change button state enabled/disabled).
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event object.</param>
        protected abstract void OnBeforeQueryStatus(object sender, EventArgs e);
    }
}
