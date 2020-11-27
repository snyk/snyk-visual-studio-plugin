//------------------------------------------------------------------------------
// <copyright file="SnykRunScanCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Controls;
using System.Windows.Data;

namespace snyk_visual_studio_plugin
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SnykRunScanCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("31b6f1bd-8317-4d93-b023-b60f667b9e76");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykRunScanCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SnykRunScanCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SnykRunScanCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new SnykRunScanCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            SnykVSPackage snykPackage = (SnykVSPackage)package;

            SnykCLI cli = new SnykCLI(snykPackage, this.ServiceProvider);
            CLIResult cliResult = cli.Scan();            

            if (!cliResult.IsSuccessful())
            {
                VsShellUtilities.ShowMessageBox(
                    this.ServiceProvider,
                    cliResult.Error.Message,
                    "Snyk",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            } else
            {
                ToolWindowPane toolWindowPane = this.package.FindToolWindow(typeof(SnykToolWindow), 0, true);

                if ((null == toolWindowPane) || (null == toolWindowPane.Frame))
                {
                    throw new NotSupportedException("Cannot create window.");
                }

                IVsWindowFrame windowFrame = (IVsWindowFrame)toolWindowPane.Frame;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                
                SnykToolWindowControl toolWindowControl = (SnykToolWindowControl)toolWindowPane.Content;
                DataGrid resultsDataGrid = toolWindowControl.resultsDataGrid;

                resultsDataGrid.Columns.Clear();
                resultsDataGrid.Items.Clear();                

                DataGridTextColumn titleTextColumn = new DataGridTextColumn();
                titleTextColumn.Header = "Title";
                titleTextColumn.Binding = new Binding("title");

                DataGridTextColumn versionTextColumn = new DataGridTextColumn();
                versionTextColumn.Header = "Version";
                versionTextColumn.Binding = new Binding("version");

                DataGridTextColumn descriptionTextColumn = new DataGridTextColumn();
                descriptionTextColumn.Header = "Description";
                descriptionTextColumn.Binding = new Binding("description");

                DataGridTextColumn severityTextColumn = new DataGridTextColumn();
                severityTextColumn.Header = "Severity";
                severityTextColumn.Binding = new Binding("severity");

                DataGridTextColumn fixedInTextColumn = new DataGridTextColumn();
                fixedInTextColumn.Header = "Fixed In";
                fixedInTextColumn.Binding = new Binding("fixedIn");

                DataGridTextColumn moduleNameTextColumn = new DataGridTextColumn();
                moduleNameTextColumn.Header = "Module Name";
                moduleNameTextColumn.Binding = new Binding("moduleName");
               
                resultsDataGrid.Columns.Add(titleTextColumn);
                resultsDataGrid.Columns.Add(moduleNameTextColumn);
                resultsDataGrid.Columns.Add(versionTextColumn);
                resultsDataGrid.Columns.Add(severityTextColumn);
                //resultsDataGrid.Columns.Add(fixedInTextColumn);
                resultsDataGrid.Columns.Add(descriptionTextColumn);

                foreach (CLIVulnerabilities cliVulnerabilities in cliResult.CLIVulnerabilities)
                {
                    foreach (Vulnerability vulnerability in cliVulnerabilities.vulnerabilities)
                    {
                        resultsDataGrid.Items.Add(vulnerability);
                    }
                }                
            }            
        }
    }
}
