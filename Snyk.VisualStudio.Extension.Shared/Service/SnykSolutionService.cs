namespace Snyk.VisualStudio.Extension.Shared.CLI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using EnvDTE;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;
    using Serilog;
    using Snyk.Code.Library.Service;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Incapsulate logic for work with Visual Studio solutions.
    /// </summary>
    public class SnykSolutionService : ISolutionService
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykSolutionService>();

        private static SnykSolutionService instance;

        private IFileProvider fileProvider;

        private SnykSolutionService() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykSolutionService"/> class.
        /// </summary>
        /// <param name="serviceProvider">Service provider implementation.</param>
        private SnykSolutionService(ISnykServiceProvider serviceProvider) => this.ServiceProvider = serviceProvider;

        /// <summary>
        /// Gets a value indicating whether <see cref="SnykSolutionService"/> singleton instance.
        /// </summary>
        public static SnykSolutionService Instance => instance;

        /// <summary>
        /// Gets a value indicating whether is solution open.
        /// </summary>
        public bool IsSolutionOpen => this.ServiceProvider.DTE.Solution.IsOpen;

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
        public static async Task InitializeAsync(ISnykServiceProvider serviceProvider)
        {
            if (instance == null)
            {
                instance = new SnykSolutionService(serviceProvider);

                await instance.InitializeSolutionEventsAsync();
            }
        }

        /// <summary>
        /// Get solution projects.
        /// </summary>
        /// <returns>Projects instance.</returns>
        public Projects GetProjects() => this.ServiceProvider.DTE.Solution.Projects;

        /// <summary>
        /// Get all solution files.
        /// </summary>
        /// <returns>List of solution files.</returns>
        public IList<string> GetSolutionProjectsFiles()
        {
            var solutionFiles = new List<string>();

            var projects = this.GetProjects();

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
                        Logger.Error(ex.Message);
                    }
                }
            }

            return solutionFiles;
        }

        /// <summary>
        /// Get full file path by relative file path.
        /// </summary>
        /// <param name="file">Relative file path.</param>
        /// <returns>Full file path.</returns>
        public string GetFileFullPath(string file)
        {
            string relativePath = file
                .Replace("/", "\\")
                .Substring(1, file.Length - 1);

            string baseDirPath = this.GetPath();

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
        public string GetPath()
        {
            Logger.Information("Enter GetSolutionPath method");

            var dte = this.ServiceProvider.DTE;
            var solution = dte.Solution;
            var projects = this.GetProjects();

            string solutionPath = string.Empty;

            // 1 case: Solution with projects.
            // 2 case: Solution is folder.
            if (this.IsSolutionWithProjects(solution, projects) || this.IsFolder(solution, projects))
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
            }

            // 3 case: Flat project without solution.
            // 4 case: Web site (in 2015)
            if (this.IsFlatProjectOrWebSite(solution, projects))
            {
                Logger.Information("Solution is 'dirty'. Get solution path from first project full name");

                string projectPath = solution.Projects.Item(1).FullName;

                Logger.Information($"Project path {projectPath}. Get solution path as project directory.");

                solutionPath = Directory.GetParent(projectPath).FullName;
            }

            Logger.Information($"Result solution path is {solutionPath}.");

            return solutionPath;
        }

        /// <inheritdoc/>
        public string GetProjectType()
        {
            Logger.Information("Get project type according to solution and projects information.");

            var dte = this.ServiceProvider.DTE;
            var solution = dte.Solution;
            var projects = this.GetProjects();

            if (this.IsSolutionWithProjects(solution, projects))
            {
                return "Solution with projects";
            }

            if (this.IsFolder(solution, projects))
            {
                return "Folder";
            }

            if (this.IsFlatProjectOrWebSite(solution, projects))
            {
                return "Flat project or web site";
            }

            return "unknown";
        }

        /// <summary>
        /// Get solution files using VS API.
        /// </summary>
        /// <returns>List of file paths.</returns>
        public IEnumerable<string> GetFiles()
        {
            var solutionProjectsFiles = this.GetSolutionProjectsFiles();

            // If Solution files are empty try get files directly from file system.
            if (solutionProjectsFiles.Count == 0)
            {
                solutionProjectsFiles = this.GetSolutionDirectoryFiles();
            }

            return solutionProjectsFiles;
        }

        /// <inheritdoc/>
        public void Clean()
        {
            this.fileProvider = null;

            this.ServiceProvider.SnykCodeService.Clean();
        }

        private async Task InitializeSolutionEventsAsync()
        {
            Logger.Information("Enter InitializeSolutionEvents method");

            IVsSolution vsSolution = await this.ServiceProvider.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

            vsSolution.SetProperty((int)__VSPROPID4.VSPROPID_ActiveSolutionLoadManager, this);

            this.SolutionEvents = new SnykVsSolutionLoadEvents(this);

            vsSolution.AdviseSolutionEvents(this.SolutionEvents, out _);

            Logger.Information("Leave InitializeSolutionEvents method");
        }

        private IList<string> GetSolutionDirectoryFiles()
        {
            string solutionPath = this.GetPath();

            string[] files = Directory.GetFileSystemEntries(solutionPath, "*", SearchOption.AllDirectories);

            var filesList = new List<string>();
            filesList.AddRange(files);

            return filesList;
        }

        private bool IsFlatProjectOrWebSite(Solution solution, Projects projects) => solution.IsDirty && projects.Count > 0;

        private bool IsSolutionWithProjects(Solution solution, Projects projects) => !solution.IsDirty && projects.Count > 0;

        private bool IsFolder(Solution solution, Projects projects) => !solution.IsDirty && projects.Count == 0;
    }
}