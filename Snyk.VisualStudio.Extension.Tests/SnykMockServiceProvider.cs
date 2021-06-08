using System;
using Snyk.VisualStudio.Extension.Settings;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Settings;
using EnvDTE;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Theme;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.SnykAnalytics;

namespace Snyk.VisualStudio.Extension.Tests
{
    class SnykMockServiceProvider : ISnykServiceProvider
    {
        public SnykActivityLogger ActivityLogger
        {
            get
            {
                return new SnykMockActivityLogger();
            }
        }

        public IAsyncServiceProvider AsyncServiceProvider
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public DTE DTE
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ISnykOptions Options
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public SnykVSPackage Package
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public SettingsManager SettingsManager
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public SnykSolutionService SolutionService
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public SnykTasksService TasksService
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public SnykVsThemeService VsThemeService
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public SnykAnalyticsService AnalyticsService => new SnykAnalyticsService();

        public SnykUserStorageSettingsService UserStorageSettingsService => throw new NotImplementedException();

        Microsoft.VisualStudio.Shell.IAsyncServiceProvider ISnykServiceProvider.AsyncServiceProvider
        {
            get
            {
                return null;
            }
        }

        public string GetApiToken()
        {
            throw new NotImplementedException();
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(SVsSolution))
            {
                return new SnykMockVsSolution();
            }

            return null;
        }

        public Task<object> GetServiceAsync(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public SnykCli NewCli()
        {
            throw new NotImplementedException();
        }

        public void ShowToolWindow()
        {
        }
    }

    public class SnykMockActivityLogger : SnykActivityLogger
    {
        public SnykMockActivityLogger() : base(null)
        {
        }

        public override void LogInformation(string message) => Console.WriteLine(message);

        public override void LogError(string message) => Console.WriteLine(message);
    }

    public class SnykMockVsSolution : IVsSolution
    {
        public int AddVirtualProject(IVsHierarchy pHierarchy, uint grfAddVPFlags)
        {
            throw new NotImplementedException();
        }

        public int AddVirtualProjectEx(IVsHierarchy pHierarchy, uint grfAddVPFlags, ref Guid rguidProjectID)
        {
            throw new NotImplementedException();
        }

        public int AdviseSolutionEvents(IVsSolutionEvents pSink, out uint pdwCookie)
        {
            pdwCookie = 0;

            return 0;
        }

        public int CanCreateNewProjectAtLocation(int fCreateNewSolution, string pszFullProjectFilePath, out int pfCanCreate)
        {
            throw new NotImplementedException();
        }

        public int CloseSolutionElement(uint grfCloseOpts, IVsHierarchy pHier, uint docCookie)
        {
            throw new NotImplementedException();
        }

        public int CreateNewProjectViaDlg(string pszExpand, string pszSelect, uint dwReserved)
        {
            throw new NotImplementedException();
        }

        public int CreateProject(ref Guid rguidProjectType, string lpszMoniker, string lpszLocation, string lpszName, uint grfCreateFlags, ref Guid iidProject, out IntPtr ppProject)
        {
            throw new NotImplementedException();
        }

        public int CreateSolution(string lpszLocation, string lpszName, uint grfCreateFlags)
        {
            throw new NotImplementedException();
        }

        public int GenerateNextDefaultProjectName(string pszBaseName, string pszLocation, out string pbstrProjectName)
        {
            throw new NotImplementedException();
        }

        public int GenerateUniqueProjectName(string lpszRoot, out string pbstrProjectName)
        {
            throw new NotImplementedException();
        }

        public int GetGuidOfProject(IVsHierarchy pHierarchy, out Guid pguidProjectID)
        {
            throw new NotImplementedException();
        }

        public int GetItemInfoOfProjref(string pszProjref, int propid, out object pvar)
        {
            throw new NotImplementedException();
        }

        public int GetItemOfProjref(string pszProjref, out IVsHierarchy ppHierarchy, out uint pitemid, out string pbstrUpdatedProjref, VSUPDATEPROJREFREASON[] puprUpdateReason)
        {
            throw new NotImplementedException();
        }

        public int GetProjectEnum(uint grfEnumFlags, ref Guid rguidEnumOnlyThisType, out IEnumHierarchies ppenum)
        {
            throw new NotImplementedException();
        }

        public int GetProjectFactory(uint dwReserved, Guid[] pguidProjectType, string pszMkProject, out IVsProjectFactory ppProjectFactory)
        {
            throw new NotImplementedException();
        }

        public int GetProjectFilesInSolution(uint grfGetOpts, uint cProjects, string[] rgbstrProjectNames, out uint pcProjectsFetched)
        {
            throw new NotImplementedException();
        }

        public int GetProjectInfoOfProjref(string pszProjref, int propid, out object pvar)
        {
            throw new NotImplementedException();
        }

        public int GetProjectOfGuid(ref Guid rguidProjectID, out IVsHierarchy ppHierarchy)
        {
            throw new NotImplementedException();
        }

        public int GetProjectOfProjref(string pszProjref, out IVsHierarchy ppHierarchy, out string pbstrUpdatedProjref, VSUPDATEPROJREFREASON[] puprUpdateReason)
        {
            throw new NotImplementedException();
        }

        public int GetProjectOfUniqueName(string pszUniqueName, out IVsHierarchy ppHierarchy)
        {
            throw new NotImplementedException();
        }

        public int GetProjectTypeGuid(uint dwReserved, string pszMkProject, out Guid pguidProjectType)
        {
            throw new NotImplementedException();
        }

        public int GetProjrefOfItem(IVsHierarchy pHierarchy, uint itemid, out string pbstrProjref)
        {
            throw new NotImplementedException();
        }

        public int GetProjrefOfProject(IVsHierarchy pHierarchy, out string pbstrProjref)
        {
            throw new NotImplementedException();
        }

        public int GetProperty(int propid, out object pvar)
        {
            throw new NotImplementedException();
        }

        public int GetSolutionInfo(out string pbstrSolutionDirectory, out string pbstrSolutionFile, out string pbstrUserOptsFile)
        {
            throw new NotImplementedException();
        }

        public int GetUniqueNameOfProject(IVsHierarchy pHierarchy, out string pbstrUniqueName)
        {
            throw new NotImplementedException();
        }

        public int GetVirtualProjectFlags(IVsHierarchy pHierarchy, out uint pgrfAddVPFlags)
        {
            throw new NotImplementedException();
        }

        public int OnAfterRenameProject(IVsProject pProject, string pszMkOldName, string pszMkNewName, uint dwReserved)
        {
            throw new NotImplementedException();
        }

        public int OpenSolutionFile(uint grfOpenOpts, string pszFilename)
        {
            throw new NotImplementedException();
        }

        public int OpenSolutionViaDlg(string pszStartDirectory, int fDefaultToAllProjectsFilter)
        {
            throw new NotImplementedException();
        }

        public int QueryEditSolutionFile(out uint pdwEditResult)
        {
            throw new NotImplementedException();
        }

        public int QueryRenameProject(IVsProject pProject, string pszMkOldName, string pszMkNewName, uint dwReserved, out int pfRenameCanContinue)
        {
            throw new NotImplementedException();
        }

        public int RemoveVirtualProject(IVsHierarchy pHierarchy, uint grfRemoveVPFlags)
        {
            throw new NotImplementedException();
        }

        public int SaveSolutionElement(uint grfSaveOpts, IVsHierarchy pHier, uint docCookie)
        {
            throw new NotImplementedException();
        }

        public int SetProperty(int propid, object var)
        {
            return 0;
        }

        public int UnadviseSolutionEvents(uint dwCookie)
        {
            throw new NotImplementedException();
        }
    }
}
