//------------------------------------------------------------------------------
// <copyright file="SnykToolWindowCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Snyk.VisualStudio.Extension.UI
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SnykToolWindowCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4129;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("31b6f1bd-8317-4d93-b023-b60f667b9e76");

        private ISnykServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykToolWindowCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SnykToolWindowCommand(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;  
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SnykToolWindowCommand Instance
        {
            get;
            private set;
        }        

        private SnykActivityLogger Logger
        {
            get
            {
                return serviceProvider.ActivityLogger;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(ISnykServiceProvider serviceProvider)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Instance = new SnykToolWindowCommand(serviceProvider);

            OleMenuCommandService commandService = await serviceProvider.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(Instance.ShowToolWindow, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }        

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            Logger.LogInformation("Enter ShowToolWindow method");

            serviceProvider.ShowToolWindow();
        }
    }
}
