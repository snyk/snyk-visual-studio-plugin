using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.SnykAnalytics;
using Snyk.VisualStudio.Extension.Theme;
using System;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.Service
{
    public interface ISnykServiceProvider
    {
        string GetApiToken();

        DTE DTE
        {
            get;
        }

        SnykVSPackage Package
        {
            get;
        }

        Task<object> GetServiceAsync(Type serviceType);

        IAsyncServiceProvider AsyncServiceProvider
        {
            get;
        }

        SnykSolutionService SolutionService
        {
            get;
        }

        SnykTasksService TasksService
        {
            get;
        }

        ISnykOptions Options
        {
            get;
        }

        SnykActivityLogger ActivityLogger
        {
            get;
        }

        SettingsManager SettingsManager
        {
            get;
        }

        SnykVsThemeService VsThemeService
        {
            get;
        }

        SnykAnalyticsService AnalyticsService
        {
            get;
        }

        SnykCli NewCli();

        void ShowToolWindow();        
    }
}
