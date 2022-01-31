namespace Snyk.VisualStudio.Extension.Shared.Service
{
    using System;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.SnykCode;

    /// <summary>
    /// Visual Studio solution load events implementation.
    /// </summary>
    public class SnykVsSolutionLoadEvents : IVsSolutionLoadEvents, IVsSolutionEvents
    {
        private ISolutionService solutionService;

        private ISentryService sentryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykVsSolutionLoadEvents"/> class.
        /// </summary>
        /// <param name="solutionService">Solution serviceinstance.</param>
        /// <param name="sentryService">Sentry service instance.</param>
        public SnykVsSolutionLoadEvents(ISolutionService solutionService, ISentryService sentryService)
        {
            this.solutionService = solutionService;
            this.sentryService = sentryService;
        }

        /// <summary>
        /// After Background Solution Load Complete event handler.
        /// </summary>
        public event EventHandler<EventArgs> AfterBackgroundSolutionLoadComplete;

        /// <summary>
        /// After Close Solution event handler.
        /// </summary>
        public event EventHandler<EventArgs> AfterCloseSolution;

        /// <summary>
        /// AfterBackgroundSolutionLoadComplete event handler. Invoke AfterBackgroundSolutionLoadComplete.
        /// </summary>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnAfterBackgroundSolutionLoadComplete()
        {
            this.AfterBackgroundSolutionLoadComplete?.Invoke(this, EventArgs.Empty);

            this.solutionService.Clean();

            this.sentryService.SetSolutionType(SolutionType.Solution);

            return VSConstants.S_OK;
        }

        /// <summary>
        /// AfterCloseFolder event handler. Invoke AfterCloseSolution.
        /// </summary>
        /// <param name="folderPath">Folder path.</param>
        public void OnAfterCloseFolder(string folderPath)
        {
            this.solutionService.Clean();

            this.sentryService.SetSolutionType(SolutionType.NoOpenSolution);

            this.AfterCloseSolution?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// AfterCloseSolution event handler. Invoke AfterCloseSolution.
        /// </summary>
        /// <param name="pUnkReserved">Reversed.</param>
        /// <returns>VSConstants.S_OK</returns>
        public int OnAfterCloseSolution(object pUnkReserved)
        {
            this.solutionService.Clean();

            this.sentryService.SetSolutionType(SolutionType.NoOpenSolution);

            this.AfterCloseSolution?.Invoke(this, EventArgs.Empty);

            return VSConstants.S_OK;
        }

        /// <summary>
        /// AfterLoadAllDeferredProjects event handler.
        /// </summary>
        public void OnAfterLoadAllDeferredProjects() { }

        /// <summary>
        /// AfterLoadProject event handler.
        /// </summary>
        /// <param name="pStubHierarchy">Stub Hierarchy.</param>
        /// <param name="pRealHierarchy">Real Hierarchy.</param>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;

        /// <summary>
        /// AfterLoadProjectBatch event handler.
        /// </summary>
        /// <param name="fIsBackgroundIdleBatch">Is Background Idle Batch.</param>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch) => VSConstants.S_OK;

        /// <summary>
        /// AfterBackgroundSolutionLoadComplete event handler. Invoke AfterBackgroundSolutionLoadComplete.
        /// </summary>
        /// <param name="folderPath">Folder path.</param>
        public void OnAfterOpenFolder(string folderPath)
        {
            this.AfterBackgroundSolutionLoadComplete?.Invoke(this, EventArgs.Empty);

            this.sentryService.SetSolutionType(SolutionType.Folder);
        }

        /// <summary>
        /// AfterOpenProject event handler.
        /// </summary>
        /// <param name="vsHierarchy">Hierarchy.</param>
        /// <param name="fAdded">Added.</param>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnAfterOpenProject(IVsHierarchy vsHierarchy, int fAdded)
        {
            var hierarchyEvents = new CodeCacheHierarchyEvents(vsHierarchy, this.solutionService.FileProvider);

            vsHierarchy.AdviseHierarchyEvents(hierarchyEvents, out _);

            this.sentryService.SetSolutionType(SolutionType.Project);

            return VSConstants.S_OK;
        }

        /// <summary>
        /// AfterOpenSolution event handler.
        /// </summary>
        /// <param name="pUnkReserved">Unk reserved.</param>
        /// <param name="fNewSolution">New solution.</param>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            this.sentryService.SetSolutionType(SolutionType.Solution);

            return VSConstants.S_OK;
        }

        /// <summary>
        /// BeforeBackgroundSolutionLoadBegins event handler.
        /// </summary>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnBeforeBackgroundSolutionLoadBegins() => VSConstants.S_OK;

        /// <summary>
        /// BeforeCloseFolder event handler.
        /// </summary>
        /// <param name="folderPath">Folder path.</param>
        public void OnBeforeCloseFolder(string folderPath) { }

        /// <summary>
        /// BeforeCloseProject event handler.
        /// </summary>
        /// <param name="pHierarchy">Hierarchy.</param>
        /// <param name="fRemoved">Removed.</param>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.S_OK;

        /// <summary>
        /// BeforeCloseSolution event handler.
        /// </summary>
        /// <param name="pUnkReserved">Unk reserved.</param>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnBeforeCloseSolution(object pUnkReserved) => VSConstants.S_OK;


        /// <summary>
        /// BeforeLoadProjectBatch event handler.
        /// </summary>
        /// <param name="fIsBackgroundIdleBatch">Is Background Idle Batch</param>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch) => VSConstants.S_OK;

        /// <summary>
        /// BeforeOpenSolution event handler.
        /// </summary>
        /// <param name="pszSolutionFilename">Solution filename.</param>
        /// <returns>VSConstants.S_OK</returns>
        public int OnBeforeOpenSolution(string pszSolutionFilename) => VSConstants.S_OK;

        /// <summary>
        /// BeforeUnloadProject event handler.
        /// </summary>
        /// <param name="pRealHierarchy">Real hierarchy.</param>
        /// <param name="pStubHierarchy">Stub hierarchy.</param>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;

        /// <summary>
        /// QueryBackgroundLoadProjectBatch event handler.
        /// </summary>
        /// <param name="pfShouldDelayLoadToNextIdle">Set ShouldDelayLoadToNextIdle to false.</param>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
        {
            pfShouldDelayLoadToNextIdle = false;

            return VSConstants.S_OK;
        }

        /// <summary>
        /// QueryCloseFolder event handler.
        /// </summary>
        /// <param name="folderPath">Folder path.</param>
        /// <param name="pfCancel">Cancel.</param>
        public void OnQueryCloseFolder(string folderPath, ref int pfCancel) { }

        /// <summary>
        /// QueryCloseProject event handler.
        /// </summary>
        /// <param name="pHierarchy">Hierarchy.</param>
        /// <param name="fRemoving">Removing.</param>
        /// <param name="pfCancel">Cancel.</param>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;

        /// <summary>
        /// QueryCloseSolution event handler.
        /// </summary>
        /// <param name="pUnkReserved">Unk reserved.</param>
        /// <param name="pfCancel">Cnalce.</param>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;

        /// <summary>
        /// QueryUnloadProject event handler.
        /// </summary>
        /// <param name="pRealHierarchy">Real hierarchy.</param>
        /// <param name="pfCancel">Cancel.</param>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;
    }
}
