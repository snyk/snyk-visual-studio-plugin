namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Serilog;
    using Snyk.Common;

    /// <summary>
    /// Implementation of <see cref="IFileProvider"/> for Visual Studio SnykCode.
    /// </summary>
    public class SnykCodeFileProvider : IFileProvider
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykCodeFileProvider>();

        private IList<string> removedFiles;

        private IList<string> changedFiles;

        private IList<string> addedFiles;

        private string solutionPath;

        private ISolutionService solutionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeFileProvider"/> class.
        /// </summary>
        /// <param name="solutionService">Solution service intance.</param>
        public SnykCodeFileProvider(ISolutionService solutionService)
        {
            this.solutionService = solutionService;

            this.addedFiles = new List<string>();
            this.removedFiles = new List<string>();
            this.changedFiles = new List<string>();
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetFiles()
        {
            var solutionFiles = this.solutionService.GetFiles();

            Logger.Information("If Solution files are empty, try get files directly from file system.");

            if (solutionFiles.Count() == 0)
            {
                solutionFiles = this.GetDirectoryFiles();
            }

            Logger.Information($"Solution files count {solutionFiles.Count()}");

            return solutionFiles;
        }

        /// <inheritdoc/>
        public string GetSolutionPath()
        {
            if (string.IsNullOrEmpty(this.solutionPath))
            {
                this.solutionPath = this.solutionService.GetPath();
            }

            return this.solutionPath;
        }

        /// <inheritdoc/>
        public void AddChangedFile(string file) => this.changedFiles.Add(file);

        /// <inheritdoc/>
        public void AddNewFile(string file) => this.addedFiles.Add(file);

        /// <inheritdoc/>
        public void RemoveFile(string file) => this.removedFiles.Add(file);

        /// <inheritdoc/>
        public IEnumerable<string> GetAddedFiles() => this.addedFiles.Except(this.removedFiles);

        /// <inheritdoc/>
        public IEnumerable<string> GetChangedFiles() => this.changedFiles.Except(this.removedFiles);

        /// <inheritdoc/>
        public IEnumerable<string> GetRemovedFiles() => this.removedFiles;

        /// <inheritdoc/>
        public void ClearHistory()
        {
            this.addedFiles = new List<string>();
            this.changedFiles = new List<string>();
            this.removedFiles = new List<string>();
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetAddedAndChangedFiles() => this.GetAddedFiles()
                .Concat(this.GetChangedFiles())
                .Distinct()
                .ToList();

        /// <inheritdoc/>
        public IEnumerable<string> GetAllChangedFiles() => this.GetAddedFiles()
                .Concat(this.GetChangedFiles())
                .Concat(this.GetRemovedFiles())
                .Distinct()
                .ToList();

        private IList<string> GetDirectoryFiles()
        {
            string[] files = Directory.GetFileSystemEntries(this.GetSolutionPath(), "*", SearchOption.AllDirectories);

            var filesList = new List<string>();
            filesList.AddRange(files);

            return filesList;
        }
    }
}
