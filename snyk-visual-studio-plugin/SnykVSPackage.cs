//------------------------------------------------------------------------------
// <copyright file="SnykVSPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Snyk.VisualStudio.Extension.UI;
using Snyk.VisualStudio.Extension.Settings;
using Microsoft.VisualStudio;
using EnvDTE80;
using EnvDTE;
using System.Threading;
using System.Threading.Tasks;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Theme;

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
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(SnykVSPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideService(typeof(ISnykService), IsAsyncQueryable = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
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

        private static SnykVSPackage instance;        

        private SnykGeneralOptionsDialogPage generalOptionsDialogPage;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykVSPackage"/> class.
        /// </summary>
        public SnykVSPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
            instance = this;
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            this.AddService(typeof(SnykService), CreateSnykService, true);            

            ISnykService snykService = await this.GetServiceAsync(typeof(SnykService)) as ISnykService;

            Console.WriteLine(snykService.ToString());

            await Task.FromResult<object>(null);            
        }       

        public async Task<object> CreateSnykService(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
        {
            var service = new SnykService(this);

            await service.InitializeAsync(cancellationToken);

            return service;
        }

        public SnykSolutionService SolutionService
        {
            get
            {
                return SnykSolutionService.Instance;
            }
        }

        public SnykTasksService TasksService
        {
            get
            {
                return SnykTasksService.Instance();
            }
        }               

        public ISnykOptions Options
        {
            get
            {
                return GetGeneralOptionsDialogPage();
            }
        }           

        public void ShowToolWindow()
        {
            ToolWindowPane toolWindowPane = FindToolWindow(typeof(SnykToolWindow), 0, true);

            if ((null == toolWindowPane) || (null == toolWindowPane.Frame))
            {
                throw new NotSupportedException("Cannot create window.");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)toolWindowPane.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public SnykToolWindowControl GetToolWindow()
        {
            ToolWindowPane toolWindowPane = FindToolWindow(typeof(SnykToolWindow), 0, true);

            if ((null == toolWindowPane) || (null == toolWindowPane.Frame))
            {
                throw new NotSupportedException("Cannot find Snyk tool window.");
            }

            return (SnykToolWindowControl)toolWindowPane.Content;
        }

        public SnykGeneralOptionsDialogPage GetGeneralOptionsDialogPage()
        {
            JoinableTaskFactory.SwitchToMainThreadAsync();

            if (generalOptionsDialogPage == null)
            {
                generalOptionsDialogPage = (SnykGeneralOptionsDialogPage)GetDialogPage(typeof(SnykGeneralOptionsDialogPage));
            }            

            return generalOptionsDialogPage;
        }        

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>        
        #endregion
    }            

    public interface ISnykService
    {
    }

    public class SnykService : ISnykServiceProvider, ISnykService
    {
        private IAsyncServiceProvider serviceProvider;

        private SnykActivityLogger activityLogger;

        private SettingsManager settingsManager;

        private SnykVsThemeService vsThemeService;

        private DTE dte;

        public SnykService(IAsyncServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public SnykActivityLogger ActivityLogger
        {
            get
            {
                return activityLogger;
            }
        }

        public ISnykOptions Options
        {
            get
            {
                return Package.GetGeneralOptionsDialogPage();
            }
        }

        public SnykSolutionService SolutionService
        {
            get
            {
                return Package.SolutionService;
            }
        }

        public SnykTasksService TasksService
        {
            get
            {
                return Package.TasksService;
            }
        }    
        
        public SettingsManager SettingsManager
        {
            get
            {
                return settingsManager;
            }
        }

        public SnykToolWindowControl GetToolWindow()
        {
            return Package.GetToolWindow();
        }

        public DTE DTE
        {
            get
            {
                return dte;
            }
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {            
            IVsActivityLog activityLog = await serviceProvider.GetServiceAsync(typeof(SVsActivityLog)) as IVsActivityLog;

            activityLogger = new SnykActivityLogger(activityLog);

            activityLogger.LogInformation("Initialize Snyk services");

            this.settingsManager = new ShellSettingsManager(Package);

            await SnykToolWindowCommand.InitializeAsync(this);            

            await SnykTasksService.InitializeAsync(this);

            await SnykSolutionService.InitializeAsync(this);
            
            Package.GetGeneralOptionsDialogPage().Initialize(this);

            GetToolWindow().ServiceProvider = this;

            this.vsThemeService = new SnykVsThemeService(this);

            await this.vsThemeService.InitializeAsync();

            await InitializeEventListeners();           

            activityLogger.LogInformation("Leave InitializeAsync");
        }

        private async Task InitializeEventListeners()
        {
            activityLogger.LogInformation("InitializeEventListeners method");

            var toolWindow = GetToolWindow();
            var tasksService = SnykTasksService.Instance();
            var solutionEvents = SnykSolutionService.Instance.SolutionEvents;

            activityLogger.LogInformation("Initialize Solultion Event Listeners");

            solutionEvents.AfterBackgroundSolutionLoadComplete += toolWindow.OnAfterBackgroundSolutionLoadComplete;
            solutionEvents.AfterCloseSolution += toolWindow.OnAfterCloseSolution;

            activityLogger.LogInformation("Initialize CLI Event Listeners");

            tasksService.ScanError += toolWindow.OnDisplayError;
            tasksService.ScanningCancelled += toolWindow.OnScanningCancelled;
            tasksService.ScanningStarted += toolWindow.OnScanningStarted;
            tasksService.ScanningUpdate += toolWindow.OnScanningUpdate;
            tasksService.ScanningFinished += toolWindow.OnScanningFinished;

            activityLogger.LogInformation("Initialize Download Event Listeners");

            tasksService.DownloadStarted += toolWindow.OnDownloadStarted;
            tasksService.DownloadFinished += toolWindow.OnDownloadFinished;
            tasksService.DownloadUpdate += toolWindow.OnDownloadUpdate;
            tasksService.DownloadCancelled += toolWindow.OnDownloadCancelled;

            activityLogger.LogInformation("Initialize ToolWindow Display Event Listeners");

            this.dte = await serviceProvider.GetServiceAsync(typeof(DTE)) as DTE;

            WindowVisibilityEvents visibilityEvents = (dte.Events as Events2)?.WindowVisibilityEvents;

            if (visibilityEvents != null)
            {
                visibilityEvents.WindowShowing += (window) => SnykTasksService.Instance().Download();
            }

            this.vsThemeService.ThemeChanged += toolWindow.OnVsThemeChanged;
        }

        public void ShowToolWindow()
        {
            Package.ShowToolWindow();
        }

        public async Task<object> GetServiceAsync(Type serviceType)
        {
            return await serviceProvider.GetServiceAsync(serviceType);
        }

        public object GetService(Type serviceType)
        {
            return null;
        }

        public SnykVSPackage Package
        {
            get
            {
                return serviceProvider as SnykVSPackage;
            }
        }

        public IAsyncServiceProvider AsyncServiceProvider
        {
            get
            {
                return serviceProvider;
            }
        }        
    }            
}
