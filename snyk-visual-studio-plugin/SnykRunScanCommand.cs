//------------------------------------------------------------------------------
// <copyright file="SnykRunScanCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Snyk.VisualStudio.Extension.CLI;
using EnvDTE;

namespace Snyk.VisualStudio.Extension.UI
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
            System.Threading.Tasks.Task.Run(() =>
            {
                var snykPackage = (SnykVSPackage)package;
                var toolWindow = snykPackage.GetToolWindow();

                Projects projects = snykPackage.SolutionService.GetProjects();

                if (projects.Count == 0)
                {
                    var error = new CliError
                    {
                        Message = "No open solution"
                    };

                    toolWindow.DisplayError(error);

                    return;
                }                

                toolWindow.HideError();
                toolWindow.ShowIndeterminateProgressBar("Scanning...");

                toolWindow.ClearDataGrid();

                var cli = new SnykCli
                {
                    Options = snykPackage.Options
                };                

                for (int index = 1; index <= projects.Count; index++)
                {
                    try
                    {
                        Project project = projects.Item(index);

                        string projectPath = project.Properties.Item("LocalPath").Value.ToString();

                        CliResult cliResult = cli.Scan(projectPath);

                        if (!cliResult.IsSuccessful())
                        {
                            toolWindow.DisplayError(cliResult.Error);
                        }
                        else
                        {
                            toolWindow.DisplayDataGrid();

                            toolWindow.AddCliResultToDataGrid(cliResult);
                        }
                    } catch (Exception exception)
                    {
                        var error = new CliError
                        {
                            Message = exception.Message
                        };

                        toolWindow.DisplayError(error);
                    }                    
                }                                   

                toolWindow.HideProgressBar();
            });                        
        }
        
        private void ShowErrorMessage(string message)
        {
            VsShellUtilities.ShowMessageBox(
                            this.ServiceProvider,
                            message,
                            "Snyk",
                            OLEMSGICON.OLEMSGICON_WARNING,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }                
    }
}
