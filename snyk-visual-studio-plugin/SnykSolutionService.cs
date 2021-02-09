using EnvDTE;
using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using Snyk.VisualStudio.Extension.Settings;
using Task = System.Threading.Tasks.Task;

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

        public async static Task InitializeAsync(ISnykServiceProvider serviceProvider)
        {
            if (instance == null)
            {
                instance = new SnykSolutionService(serviceProvider);

                await instance.InitializeSolutionEventsAsync();
            }  
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

        public Projects GetProjects()
        {           
            return ServiceProvider.DTE.Solution.Projects;
        }

        public bool IsSolutionOpen()
        {
            return ServiceProvider.DTE.Solution.IsOpen;
        }

        public String GetSolutionPath()
        {
            logger.LogInformation("Enter GetSolutionPath method");

            var dteSolution = ServiceProvider.DTE.Solution;

            var solutionPath = "";

            if (dteSolution.IsDirty)
            {
                logger.LogInformation("Solution is 'dirty'. Get solution path from first project full name");

                solutionPath = dteSolution.Projects.Item(1).FullName;
            }
            else
            {
                logger.LogInformation("Get solution path from solution full name");

                solutionPath = Directory.GetParent(dteSolution.FullName).FullName;
            }
            
            FileAttributes fileAttributes = File.GetAttributes(solutionPath);

            if (!fileAttributes.HasFlag(FileAttributes.Directory))
            {
                logger.LogInformation("Solution path referene to project file name, getting solution directory path.");

                solutionPath = Directory.GetParent(solutionPath).FullName;
            }

            logger.LogInformation($"Result solution path {solutionPath}");

            return solutionPath;
        }

        public ISnykServiceProvider ServiceProvider { get; set; }

        public int OnBeforeOpenProject(ref Guid guidProjectID,
            ref Guid guidProjectType,
            string pszFileName,
            IVsSolutionLoadManagerSupport pSLMgrSupport) => VSConstants.S_OK;

        public int OnDisconnect() => VSConstants.S_OK;

        private async Task InitializeSolutionEventsAsync()
        {
            logger.LogInformation("Enter InitializeSolutionEvents method");

            IVsSolution vsSolution = await ServiceProvider.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

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
