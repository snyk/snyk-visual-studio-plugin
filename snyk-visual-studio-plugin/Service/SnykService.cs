using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.SnykAnalytics;
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

        private SnykAnalyticsService analyticsService;

        public SnykService(IAsyncServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public SnykActivityLogger ActivityLogger => activityLogger;

        public ISnykOptions Options => Package.GeneralOptionsDialogPage;

        public SnykSolutionService SolutionService => solutionService;

        public SnykTasksService TasksService => tasksService;

        public SettingsManager SettingsManager => settingsManager;

        public DTE DTE => dte;

        public void ShowToolWindow() => Package.ShowToolWindow();

        public async Task<object> GetServiceAsync(Type serviceType) => await serviceProvider.GetServiceAsync(serviceType);

        public object GetService(Type serviceType) => null;

        public SnykVSPackage Package => serviceProvider as SnykVSPackage;

        public IAsyncServiceProvider AsyncServiceProvider => serviceProvider;

        public SnykVsThemeService VsThemeService => vsThemeService;

        public SnykAnalyticsService AnalyticsService 
        {
            get
            {
                if (analyticsService == null)
                {
                    activityLogger.LogInformation("Initialize Snyk Segment Analytics Service.");

                    analyticsService = new SnykAnalyticsService
                    {
                        Logger = activityLogger
                    };

                    analyticsService.Initialize();
                }

                return analyticsService;
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

            activityLogger.LogInformation("Leave SnykService.InitializeAsync");
        }

        public SnykCli NewCli() => new SnykCli
        {
            Options = Options,
            Logger = ActivityLogger
        };

        public string GetApiToken()
        {
            if (!string.IsNullOrEmpty(Options.ApiToken)) return Options.ApiToken;

            return NewCli().GetApiToken();
        }
    }    
}
