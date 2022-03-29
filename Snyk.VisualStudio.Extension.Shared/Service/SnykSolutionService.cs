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
        private SnykSolutionService()
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

                Logger.Information("Project path {ProjectPath}. Get solution path as project directory.", projectPath);

                solutionPath = Directory.GetParent(projectPath).FullName;
            }

            Logger.Information("Result solution path is {SolutionPath}.", solutionPath);

            return solutionPath;
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
                return this.GetSolutionDirectoryFiles();
            }

            // If normal solution, then get all files in solution projects.
            return await this.GetSolutionFilesAsync();
        }

        /// <inheritdoc/>
        public void Clean()
        {
            this.fileProvider.ClearHistory();

            this.ServiceProvider.SnykCodeService.Clean();
        }

        private async Task InitializeSolutionEventsAsync()
        {
            Logger.Information("Enter InitializeSolutionEvents method");

            IVsSolution vsSolution = await this.ServiceProvider.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

            vsSolution.SetProperty((int)__VSPROPID4.VSPROPID_ActiveSolutionLoadManager, this);

            this.SolutionEvents = new SnykVsSolutionLoadEvents(this, this.ServiceProvider.OssService, this.ServiceProvider.SentryService);

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
                return new List<string>();
            }

            var solutionItem = item.FindParent(Toolkit.SolutionItemType.Solution);

            return await this.GetSolutionItemFilesAsync(solutionItem);
        }
    }
}