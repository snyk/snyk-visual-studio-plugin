using Snyk.VisualStudio.Extension.Services;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.UI;
using System;

namespace Snyk.VisualStudio.Extension
{
    public interface ISnykServiceProvider : IServiceProvider
    {

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

        SnykToolWindowControl GetToolWindow();

        void ShowToolWindow();
    }
}
