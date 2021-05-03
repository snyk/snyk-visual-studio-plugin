using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace Snyk.VisualStudio.Extension.Commands
{
    public abstract class AbstractSnykCommand
    {
        protected readonly AsyncPackage package;

        protected AbstractSnykCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(GetCommandSet(), GetCommandId());
            
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            
            commandService.AddCommand(menuItem);
        }

        protected Guid GetCommandSet() => SnykExtension.Guids.SnykVSPackageCommandSet;

        protected SnykVSPackage VsPackage => this.package as SnykVSPackage;

        protected abstract void Execute(object sender, EventArgs eventArgs);        

        protected abstract int GetCommandId();
    }
}
