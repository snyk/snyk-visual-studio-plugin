﻿using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.Service
{
    /// <summary>
    /// Visual Studio solution load events implementation.
    /// </summary>
    public class SnykVsSolutionLoadEvents : IVsSolutionLoadEvents, IVsSolutionEvents
    {
        private ISolutionService solutionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykVsSolutionLoadEvents"/> class.
        /// </summary>
        /// <param name="solutionService">Solution serviceinstance.</param>
        /// <param name="sentryService">Sentry service instance.</param>
        public SnykVsSolutionLoadEvents(ISolutionService solutionService)
        {
            this.solutionService = solutionService;
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

            return VSConstants.S_OK;
        }

        /// <summary>
        /// AfterCloseFolder event handler. Invoke AfterCloseSolution.
        /// </summary>
        /// <param name="folderPath">Folder path.</param>
        public void OnAfterCloseFolder(string folderPath)
        {
            this.AfterCloseSolution?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// AfterCloseSolution event handler. Invoke AfterCloseSolution.
        /// </summary>
        /// <param name="pUnkReserved">Reversed.</param>
        /// <returns>VSConstants.S_OK</returns>
        public int OnAfterCloseSolution(object pUnkReserved)
        {
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
        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// AfterLoadProjectBatch event handler.
        /// </summary>
        /// <param name="fIsBackgroundIdleBatch">Is Background Idle Batch.</param>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// AfterBackgroundSolutionLoadComplete event handler. Invoke AfterBackgroundSolutionLoadComplete.
        /// </summary>
        /// <param name="folderPath">Folder path.</param>
        public void OnAfterOpenFolder(string folderPath)
        {
            this.AfterBackgroundSolutionLoadComplete?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// AfterOpenProject event handler.
        /// </summary>
        /// <param name="vsHierarchy">Hierarchy.</param>
        /// <param name="fAdded">Added.</param>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnAfterOpenProject(IVsHierarchy vsHierarchy, int fAdded)
        {
            if (SnykVSPackage.Instance == null || SnykVSPackage.ServiceProvider?.SolutionService == null)
            {
                return VSConstants.S_OK;
            }
            // Reset solution folder cache to force loading Solution Folder from VS API
            SnykVSPackage.ServiceProvider.SolutionService.SolutionFolderCache = "";

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
