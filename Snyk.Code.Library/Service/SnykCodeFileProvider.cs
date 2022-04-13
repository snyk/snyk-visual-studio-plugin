namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Snyk.Common;

    /// <summary>
    /// Implementation of <see cref="IFileProvider"/> for Visual Studio SnykCode.
    /// </summary>
    public class SnykCodeFileProvider : IFileProvider
    {
        private IList<string> removedFiles;

        private IList<string> changedFiles;

        private string solutionPath;

        private ISolutionService solutionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeFileProvider"/> class.
        /// </summary>
        /// <param name="solutionService">Solution service intance.</param>
        public SnykCodeFileProvider(ISolutionService solutionService)
        {
            this.solutionService = solutionService;

            this.removedFiles = new List<string>();
            this.changedFiles = new List<string>();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetFilesAsync() => await this.solutionService.GetFilesAsync();

        /// <inheritdoc/>
        public async Task<string> GetSolutionPathAsync()
        {
            if (string.IsNullOrEmpty(this.solutionPath))
            {
                this.solutionPath = await this.solutionService.GetPathAsync();
            }

            return this.solutionPath;
        }

        /// <inheritdoc/>
        public void AddChangedFile(string file) => this.changedFiles.Add(file);

        /// <inheritdoc/>
        public void RemoveFile(string file) => this.removedFiles.Add(file);

        /// <inheritdoc/>
        public IEnumerable<string> GetChangedFiles() => this.changedFiles
            .Except(this.removedFiles)
            .Where(file => file != null);

        /// <inheritdoc/>
        public IEnumerable<string> GetRemovedFiles() => this.removedFiles;

        /// <inheritdoc/>
        public void ClearHistory()
        {
            this.changedFiles.Clear();
            this.removedFiles.Clear();

            this.solutionPath = null;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetAllChangedFiles() => this.GetChangedFiles()
                .Concat(this.GetRemovedFiles())
                .Distinct()
                .Where(file => file != null);
    }
}
