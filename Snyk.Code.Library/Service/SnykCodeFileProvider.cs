﻿namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Caching;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;
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
        public IEnumerable<string> GetFiles() => this.solutionService.GetFiles();

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
        public async Task FilterFilesAsync(IFiltersService filtersService, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dcIgnoreService = new DcIgnoreService();

            this.files = dcIgnoreService.FilterFiles(this.solutionPath, this.files, cancellationToken).ToList();

            this.files = await filtersService.FilterFilesAsync(this.files, cancellationToken);
        }

        private void InitializeCache()
        {
            foreach (string filePath in this.files)
            {
                this.AddToCache(filePath);
            }
        }

        /// <summary>
        /// Add file hash and content to cache.
        /// </summary>
        /// <param name="filePath">Source file path.</param>
        private void AddToCache(string filePath)
        {
            try
            {
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string fileHash = Sha256.ComputeHash(fileContent);

                string relativeFilePath = this.CreateRelativeFilePath(filePath);

        /// <inheritdoc/>
        public IEnumerable<string> GetChangedFiles() => this.changedFiles.Except(this.removedFiles);

        /// <inheritdoc/>
        public IEnumerable<string> GetRemovedFiles() => this.removedFiles;

        /// <inheritdoc/>
        public void ClearHistory()
        {
            this.changedFiles.Clear();
            this.removedFiles.Clear();
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetAllChangedFiles() => this.GetChangedFiles()
                .Concat(this.GetRemovedFiles())
                .Distinct()
                .ToList();
    }
}
