namespace Snyk.VisualStudio.Extension.Shared.CLI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using EnvDTE;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Serilog;
    using Snyk.Code.Library.Service;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Task = System.Threading.Tasks.Task;
    using Toolkit = Community.VisualStudio.Toolkit;

    /// <summary>
    /// Incapsulate logic for work with Visual Studio solutions.
    /// </summary>
    public class SnykSolutionService : ISolutionService
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykSolutionService>();

        private static SnykSolutionService instance;

        private IFileProvider fileProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykSolutionService"/> class.
        /// </summary>
        public SnykSolutionService()
        {
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="SnykSolutionService"/> singleton instance.
        /// </summary>
        public static SnykSolutionService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SnykSolutionService();
                }

                return instance;
            }
        }

        /// <summary>
        /// Gets a value indicating whether is solution open.
        /// </summary>
        public bool IsSolutionOpen() => this.ServiceProvider.DTE.Solution.IsOpen;

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="ISnykServiceProvider"/> instance.
        /// </summary>
        public ISnykServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Solution events instance.
        /// </summary>
        public SnykVsSolutionLoadEvents SolutionEvents { get; set; }

        /// <inheritdoc/>
        public IFileProvider FileProvider
        {
            get
            {
                if (this.fileProvider == null)
                {
                    this.fileProvider = new SnykCodeFileProvider(this);
                }

                return this.fileProvider;
            }
        }

        /// <summary>
        /// Initialize service.
        /// </summary>
        /// <param name="serviceProvider">Service provider implementation.</param>
        /// <returns>Task.</returns>
        public async Task InitializeAsync(ISnykServiceProvider serviceProvider)
        {
            Instance.ServiceProvider = serviceProvider;

            await Instance.InitializeSolutionEventsAsync();
        }

        /// <summary>
        /// Get solution projects.
        /// </summary>
        /// <returns>Projects instance.</returns>
        public Projects GetProjects() => this.ServiceProvider.DTE.Solution.Projects;

        /// <inheritdoc/>
        /// If DTE.Solution.FullName - path to solution file (*.sln file), then it's Visual Studio solution.
        /// If DTE.Solution.FullName - path to folder (not *.sln file), then project opened as folder.
        public bool IsSolutionOpenedAsFolder() => Directory.Exists(this.ServiceProvider.DTE.Solution.FullName);

        /// <summary>
        /// Get full file path by relative file path.
        /// </summary>
        /// <param name="file">Relative file path.</param>
        /// <returns>Full file path.</returns>
        public async System.Threading.Tasks.Task<string> GetFileFullPathAsync(string file)
        {
            string relativePath = file
                .Replace("/", "\\")
                .Substring(1, file.Length - 1);

            string baseDirPath = await this.GetSolutionFolderAsync();

            return Path.Combine(baseDirPath, relativePath);
        }

        /// <summary>
        /// Handle Disconnect event.
        /// </summary>
        /// <returns>Return Ok constant.</returns>
        public int OnDisconnect() => VSConstants.S_OK;

        /// <summary>
        /// Get solution path. First try to get path by VS solution (solution with projects or folder).
        /// If no success, try to get path for flat project (without solution) or web site (in case VS2015).
        /// </summary>
        /// <returns>Solution path string.</returns>
        public async System.Threading.Tasks.Task<string> GetSolutionFolderAsync()
        {
            string solutionFolder = await this.FindRootDirectoryForSolutionAsync();

            Logger.Information("Solution folder from is {SolutionFolder}", solutionFolder);

            if (solutionFolder == null || solutionFolder.IsNullOrEmpty())
            {
                solutionFolder = await this.FindRootDirectoryForSolutionFromDteAsync();
            }

            Logger.Information("Result solution folder from is {SolutionFolder}", solutionFolder);

            return solutionFolder;
        }

        /// <inheritdoc/>
        public string FindRootDirectoryForSolutionProjects(string rootDir, IList<string> projectDirs)
        {
            if (rootDir == null || rootDir.IsNullOrEmpty())
            {
                return null;
            }

            if (projectDirs.All(dir => dir.StartsWith(rootDir)))
            {
                return rootDir;
            }

            string newRootDir = Directory.GetParent(rootDir)?.FullName;

            return this.FindRootDirectoryForSolutionProjects(newRootDir, projectDirs);
        }

        /// <summary>
        /// Get solution files using VS API.
        /// </summary>
        /// <returns>List of file paths.</returns>
        public async System.Threading.Tasks.Task<IEnumerable<string>> GetFilesAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // If solution is folder type when just get all files in solution directory.
            if (this.IsSolutionOpenedAsFolder())
            {
                return await this.GetSolutionDirectoryFilesAsync();
            }

            // If normal solution, then get all files in solution projects.
            return await this.GetSolutionFilesAsync();
        }

        /// <inheritdoc/>
        public void Clean()
        {
            this.fileProvider.ClearHistory();

            this.ServiceProvider.SnykCodeService.Clean();

            this.ServiceProvider.OssService.ClearCache();
        }

        private async Task InitializeSolutionEventsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Logger.Information("Enter InitializeSolutionEvents method");

            IVsSolution vsSolution = await this.ServiceProvider.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

            vsSolution.SetProperty((int)__VSPROPID4.VSPROPID_ActiveSolutionLoadManager, this);

            this.SolutionEvents = new SnykVsSolutionLoadEvents(this, this.ServiceProvider.OssService, this.ServiceProvider.SentryService);

            vsSolution.AdviseSolutionEvents(this.SolutionEvents, out _);

            Logger.Information("Leave InitializeSolutionEvents method");
        }

        private async System.Threading.Tasks.Task<IList<string>> GetSolutionDirectoryFilesAsync()
        {
            string solutionPath = await this.GetSolutionFolderAsync();

            string[] files = Directory.GetFileSystemEntries(solutionPath, "*", SearchOption.AllDirectories);

            var filesList = new List<string>();
            filesList.AddRange(files);

            return filesList;
        }

        private async System.Threading.Tasks.Task<IList<string>> GetSolutionItemFilesAsync(Toolkit.SolutionItem solutionItem)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var files = new List<string>();

                foreach (var item in solutionItem.Children)
                {
                    if (item.Type == Toolkit.SolutionItemType.PhysicalFile)
                    {
                        files.Add(item.FullPath);
                    }
                    else
                    {
                        var itemFiles = await this.GetSolutionItemFilesAsync(item);

                        if (itemFiles != null && itemFiles.Count > 0)
                        {
                            files.AddRange(itemFiles);
                        }
                    }
                }

                return files;
            }
            catch (Exception ignore)
            {
                // SolutionItem.Children inside Children can throw parameter incorrect exception.
                // In this case return empty list.
                return new List<string>();
            }
        }

        private async System.Threading.Tasks.Task<IEnumerable<string>> GetSolutionFilesAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var item = await Toolkit.VS.Solutions.GetActiveItemAsync();

            if (item == null)
            {
                return await this.GetSolutionProjectsFilesFromDteAsync();
            }

            var solutionItem = item.FindParent(Toolkit.SolutionItemType.Solution);

            return await this.GetSolutionItemFilesAsync(solutionItem);
        }

        private string GetDirectoryPath(string path) => Directory.Exists(path) ? path : Directory.GetParent(path).FullName;

        private async System.Threading.Tasks.Task<string> FindRootDirectoryForSolutionAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var item = await Toolkit.VS.Solutions.GetActiveItemAsync();

            if (item == null)
            {
                return null;
            }

            var solutionItem = item.FindParent(Toolkit.SolutionItemType.Solution);

            if (solutionItem == null)
            {
                return null;
            }

            var solutionDir = this.GetDirectoryPath(solutionItem.FullPath);

            var projectFolders = this.GetSolutionProjects(solutionItem);

            return this.FindRootDirectoryForSolutionProjects(solutionDir, projectFolders);
        }

        private async System.Threading.Tasks.Task<string> FindRootDirectoryForSolutionFromDteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Logger.Information("Enter GetSolutionPath method");

            var dte = this.ServiceProvider.DTE;
            var solution = dte.Solution;
            var projects = this.GetProjects();

            string solutionPath = string.Empty;

            // 1 case: Solution with projects.
            // 2 case: Solution is folder.
            if (await this.IsSolutionWithProjectsAsync(solution, projects) || await this.IsFolderAsync(solution, projects))
            {
                Logger.Information("Path is solution with projects or folder.");

                solutionPath = solution.FullName;

                if (string.IsNullOrEmpty(solutionPath))
                {
                    return string.Empty;
                }

                if (!File.GetAttributes(solutionPath).HasFlag(FileAttributes.Directory))
                {
                    Logger.Information("Remove solution file name from path.");

                    solutionPath = Directory.GetParent(solutionPath).FullName;
                }

                var projectFolders = await this.GetSolutionProjectsFromDteAsync(projects);

                solutionPath = this.FindRootDirectoryForSolutionProjects(solutionPath, projectFolders);
            }

            // 3 case: Flat project without solution.
            // 4 case: Web site (in 2015)
            if (await this.IsFlatProjectOrWebSiteAsync(solution, projects))
            {
                Logger.Information("Solution is 'dirty'. Get solution path from first project full name");

                string projectPath = solution.Projects.Item(1).FullName;

                Logger.Information("Project path {ProjectPath}. Get solution path as project directory.", projectPath);

                solutionPath = Directory.GetParent(projectPath).FullName;
            }

            Logger.Information("Result solution path is {SolutionPath}.", solutionPath);

            return solutionPath;
        }

        private async System.Threading.Tasks.Task<bool> IsFlatProjectOrWebSiteAsync(Solution solution, Projects projects)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return solution.IsDirty && projects.Count > 0;
        }

        private async System.Threading.Tasks.Task<bool> IsFolderAsync(Solution solution, Projects projects)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return !solution.IsDirty && projects.Count == 0;
        }

        private async System.Threading.Tasks.Task<bool> IsSolutionWithProjectsAsync(Solution solution, Projects projects)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return !solution.IsDirty && projects.Count > 0;
        }

        private IList<string> GetSolutionProjects(Toolkit.SolutionItem solutionItem)
        {
            var projectFolders = new List<string>();

            try
            {
                if (solutionItem.Children != null)
                {
                    foreach (var children in solutionItem.Children)
                    {
                        if (children.Type == Toolkit.SolutionItemType.Project
                            || children.Type == Toolkit.SolutionItemType.VirtualProject
                            || children.Type == Toolkit.SolutionItemType.MiscProject)
                        {
                            projectFolders.Add(this.GetDirectoryPath(children.FullPath));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error on get all project paths");
            }

            return projectFolders;
        }

        private async System.Threading.Tasks.Task<IList<string>> GetSolutionProjectsFromDteAsync(Projects projects)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var projectFolders = new List<string>();

            try
            {
                foreach (var projectItem in projects)
                {
                    var project = projectItem as Project;

                    string projectPath = new FileInfo(project.FullName).DirectoryName;

                    projectFolders.Add(this.GetDirectoryPath(projectPath));
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error on get all project paths from dte");
            }

            return projectFolders;
        }

        private async System.Threading.Tasks.Task<IList<string>> GetSolutionProjectsFilesFromDteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var solutionFiles = new List<string>();

            var projects = this.GetProjects();

            try
            {
                foreach (Project project in projects)
                {
                    foreach (ProjectItem projectItem in project.ProjectItems)
                    {
                        try
                        {
                            solutionFiles.Add(projectItem.get_FileNames(0));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "Failed to get file name from projectItem");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to get project files from Projects object");
            }

            Logger.Information("Solution files count {Count}", solutionFiles.Count());

            return solutionFiles;
        }
    }
}