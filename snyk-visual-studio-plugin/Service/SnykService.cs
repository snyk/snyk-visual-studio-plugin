using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.Theme;
using Snyk.VisualStudio.Extension.UI;
using System;
using System.Threading;
using System.Threading.Tasks;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace Snyk.VisualStudio.Extension.Service
{
    public interface ISnykService
    {
    }

    public class SnykService : ISnykServiceProvider, ISnykService
    {
        private IAsyncServiceProvider serviceProvider;

        private SnykActivityLogger activityLogger;

        private SettingsManager settingsManager;

        private SnykVsThemeService vsThemeService;

        private SnykTasksService tasksService;

        private SnykSolutionService solutionService;

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
                return Package.GeneralOptionsDialogPage;
            }
        }

        public SnykSolutionService SolutionService
        {
            get
            {
                return solutionService;
            }
        }

        public SnykTasksService TasksService
        {
            get
            {
                return tasksService;
            }
        }

        public SettingsManager SettingsManager
        {
            get
            {
                return settingsManager;
            }
        }

        public DTE DTE
        {
            get
            {
                return dte;
            }
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

        public SnykVsThemeService VsThemeService
        {
            get
            {
                return vsThemeService;
            }
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            IVsActivityLog activityLog = await serviceProvider.GetServiceAsync(typeof(SVsActivityLog)) as IVsActivityLog;

            activityLogger = new SnykActivityLogger(activityLog);

            activityLogger.LogInformation("Initialize Snyk services");

            await Package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            this.settingsManager = new ShellSettingsManager(Package);

            this.vsThemeService = new SnykVsThemeService(this);
            await this.vsThemeService.InitializeAsync();

            await SnykToolWindowCommand.InitializeAsync(this);

            await SnykTasksService.InitializeAsync(this);

            await SnykSolutionService.InitializeAsync(this);

            this.dte = await serviceProvider.GetServiceAsync(typeof(DTE)) as DTE;

            tasksService = SnykTasksService.Instance;
            solutionService = SnykSolutionService.Instance;

            activityLogger.LogInformation("Initialize ToolWindow Display Event Listeners");
            activityLogger.LogInformation("Leave SnykService.InitializeAsync");
        }

        public SnykCli NewCli() => new SnykCli
        {
            Options = Options,
            Logger = ActivityLogger
        };
    }    
}
