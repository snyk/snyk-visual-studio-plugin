//------------------------------------------------------------------------------
// <copyright file="SnykVSPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.UI;
using Snyk.VisualStudio.Extension.Settings;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Threading;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    /// [PackageRegistration(UseManagedResourcesOnly = true)]    
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(SnykVSPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideService(typeof(ISnykService), IsAsyncQueryable = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(SnykToolWindow), Style = VsDockStyle.Tabbed)]
    [ProvideOptionPage(typeof(SnykGeneralOptionsDialogPage), "Snyk", "General settings", 1000, 1001, true)]
    [ProvideOptionPage(typeof(SnykProjectOptionsDialogPage), "Snyk", "Project settings", 1000, 1002, true)]
    public sealed class SnykVSPackage : AsyncPackage
    {
        /// <summary>
        /// SnykVSPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "5ddf9abb-42ec-49b9-b201-b3e2fc2f8f89";

        private SnykGeneralOptionsDialogPage generalOptionsDialogPage;

        private ISnykServiceProvider serviceProvider;

        private SnykToolWindow toolWindow;

        private SnykToolWindowControl toolWindowControl;

        private SnykActivityLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykVSPackage"/> class.
        /// </summary>
        public SnykVSPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            this.AddService(typeof(SnykService), CreateSnykService, true);

            serviceProvider = await this.GetServiceAsync(typeof(SnykService)) as SnykService;

            logger = serviceProvider.ActivityLogger;

            logger.LogInformation("Get SnykService as ServiceProvider.");
            logger.LogInformation("Start InitializeGeneralOptionsAsync.");

            await InitializeGeneralOptionsAsync();

            logger.LogInformation("Start Initialize tool window. Before call GetToolWindowControl() method.");

            await InitializeToolWindowAsync();

            logger.LogInformation("Before call toolWindowControl.InitializeEventListenersAsync() method.");

            await toolWindowControl.InitializeEventListenersAsync(serviceProvider);

            logger.LogInformation("Call await base.InitializeAsync() method");

            await base.InitializeAsync(cancellationToken, progress);

            logger.LogInformation("Leave SnykVSPackage.InitializeAsync()");
        }       

        public async Task<object> CreateSnykService(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
        {
            var service = new SnykService(this);

            await service.InitializeAsync(cancellationToken);

            return service;
        }        
        
        public ISnykServiceProvider ServiceProvider
        {
            get
            {
                return serviceProvider;
            }
        }      

        public async Task InitializeToolWindowAsync()
        {
            logger.LogInformation("Enter InitializeToolWindowAsync() method");

            if (toolWindow == null)
            {
                logger.LogInformation("ToolWindow is not initialized. Call await JoinableTaskFactory.SwitchToMainThreadAsync().");

                await JoinableTaskFactory.SwitchToMainThreadAsync();

                logger.LogInformation("Call FindToolWindow().");

                toolWindow = FindToolWindow(typeof(SnykToolWindow), 0, true) as SnykToolWindow;

                logger.LogInformation($"Check ToolWindow is not null {toolWindow}.");

                if ((null == toolWindow) || (null == toolWindow.Frame))
                {
                    logger.LogError("Exception: Cannot find Snyk tool window.");

                    throw new NotSupportedException("Cannot find Snyk tool window.");
                }

                logger.LogInformation("Initialize ToolWindow.Content. Call await JoinableTaskFactory.SwitchToMainThreadAsync().");

                await JoinableTaskFactory.SwitchToMainThreadAsync();

                logger.LogInformation("Call ToolWindow.Content.");

                this.toolWindowControl = (SnykToolWindowControl)toolWindow.Content;

                logger.LogInformation("Leave InitializeToolWindowControlAsync() method");
            }

            logger.LogInformation("Leave InitializeToolWindowAsync() method");
        }

        public void ShowToolWindow() => toolWindowControl.ShowToolWindow();

        public SnykGeneralOptionsDialogPage GeneralOptionsDialogPage
        {
            get
            {
                return generalOptionsDialogPage;
            }
        }        

        private async Task InitializeGeneralOptionsAsync()
        {
            serviceProvider.ActivityLogger.LogInformation("Enter InitializeGeneralOptionsAsync() method.");

            if (generalOptionsDialogPage == null)
            {
                serviceProvider.ActivityLogger.LogInformation("Call GetDialogPage to create. await JoinableTaskFactory.SwitchToMainThreadAsync().");

                await JoinableTaskFactory.SwitchToMainThreadAsync();

                serviceProvider.ActivityLogger.LogInformation("GeneralOptionsDialogPage not created yet. Call GetDialogPage to create.");

                generalOptionsDialogPage = (SnykGeneralOptionsDialogPage)GetDialogPage(typeof(SnykGeneralOptionsDialogPage));

                serviceProvider.ActivityLogger.LogInformation("Call generalOptionsDialogPage.Initialize()");

                generalOptionsDialogPage.Initialize(serviceProvider);
            }

            serviceProvider.ActivityLogger.LogInformation("Leave InitializeGeneralOptionsAsync() method.");
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>        
        #endregion
    }                    
}
