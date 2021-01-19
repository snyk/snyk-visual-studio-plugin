using EnvDTE;
using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Services
{
    public class SnykSolutionService : IVsSolutionLoadManager
    {
        private static SnykSolutionService instance;

        private SnykSolutionSettingsService solutionSettingsService;

        private SnykActivityLogger logger;

        private SnykSolutionService() { }

        private SnykSolutionService(ISnykServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;

            this.logger = serviceProvider.ActivityLogger;            
        }

        public static SnykSolutionService Initialize(ISnykServiceProvider serviceProvider)
        {
            if (instance == null)
            {
                instance = new SnykSolutionService(serviceProvider);

                instance.InitializeSolutionEvents();
            }

            return instance;
        }

        public static SnykSolutionService Instance
        {
            get
            {
                return instance;
            }
        }

        public SnykActivityLogger Logger
        {
            get
            {
                return logger;
            }
        }

        public SnykSolutionSettingsService SolutionSettingsService
        {
            get
            {
                if (solutionSettingsService == null)
                {
                    solutionSettingsService = new SnykSolutionSettingsService(this);
                }

                return solutionSettingsService;
            }
        }

        public SnykVsSolutionLoadEvents SolutionEvents { get; set; }

        public Projects GetProjects() => GetDTE().Solution.Projects;

        public bool IsSolutionOpen() => GetDTE().Solution.IsOpen;

        public String GetSolutionPath()
        {
            logger.LogInformation("Enter GetSolutionPath method");

            DTE dte = GetDTE();
            var dteSolution = dte.Solution;

            if (dteSolution.IsDirty)
            {
                logger.LogInformation("Solution is 'dirty'. Get solution path from first project full name");

                return dteSolution.Projects.Item(1).FullName;
            }
            else
            {
                logger.LogInformation("Get solution path from solution full name");

                return Directory.GetParent(dteSolution.FullName).FullName;
            }            
        }

        public ISnykServiceProvider ServiceProvider { get; set; }

        public int OnBeforeOpenProject(ref Guid guidProjectID,
            ref Guid guidProjectType,
            string pszFileName,
            IVsSolutionLoadManagerSupport pSLMgrSupport) => VSConstants.S_OK;

        public int OnDisconnect() => VSConstants.S_OK;

        private DTE GetDTE() => (DTE) this.ServiceProvider.GetService(typeof(DTE));

        private void InitializeSolutionEvents()
        {
            logger.LogInformation("Enter InitializeSolutionEvents method");

            IVsSolution vsSolution = ServiceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            vsSolution.SetProperty((int)__VSPROPID4.VSPROPID_ActiveSolutionLoadManager, this);

            SolutionEvents = new SnykVsSolutionLoadEvents();

            uint pdwCookie;
            vsSolution.AdviseSolutionEvents(SolutionEvents, out pdwCookie);

            logger.LogInformation("Leave InitializeSolutionEvents method");
        }
    }
    
    public class SnykVsSolutionLoadEvents : IVsSolutionLoadEvents, IVsSolutionEvents
    {
        public event EventHandler<EventArgs> AfterBackgroundSolutionLoadComplete;
        public event EventHandler<EventArgs> AfterCloseSolution;

        public int OnAfterBackgroundSolutionLoadComplete()
        {
            AfterBackgroundSolutionLoadComplete?.Invoke(this, EventArgs.Empty);

            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            AfterCloseSolution?.Invoke(this, EventArgs.Empty);

            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;

        public int OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch) => VSConstants.S_OK;

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) => VSConstants.S_OK;

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution) => VSConstants.S_OK;

        public int OnBeforeBackgroundSolutionLoadBegins() => VSConstants.S_OK;

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.S_OK;

        public int OnBeforeCloseSolution(object pUnkReserved) => VSConstants.S_OK;

        public int OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch) => VSConstants.S_OK;

        public int OnBeforeOpenSolution(string pszSolutionFilename) => VSConstants.S_OK;

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;

        public int OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
        {
            pfShouldDelayLoadToNextIdle = false;

            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;
    }
}
