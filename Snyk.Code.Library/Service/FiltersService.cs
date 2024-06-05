namespace Snyk.Code.Library.Service
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto;
    using Snyk.Common;

    /// <inheritdoc/>
    public class FiltersService : IFiltersService
    {
        /// <summary>
        /// The limit for maximum file size in bytes. The limit is 1MB.
        /// </summary>
        private const int MaxFileSize = 1_000_000;

        private static readonly ILogger Logger = LogManager.ForContext<FiltersService>();

        private readonly string[] defaultIgnoreDirectories = new string[] { "node_modules", ".vs", ".github" };

        private ISnykCodeClient codeClient;

        private FiltersDto filters;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiltersService"/> class.
        /// </summary>
        /// <param name="client">SnykCode client implementation.</param>
        public FiltersService(ISnykCodeClient client) => this.codeClient = client;

        /// <inheritdoc/>
        public async Task<IList<string>> FilterFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
        {
            Logger.Information("Filter {Count} files.", filePaths.Count());

            cancellationToken.ThrowIfCancellationRequested();

            var filters = await this.GetFiltersAsync();

            cancellationToken.ThrowIfCancellationRequested();

            var extensionFilters = filters.Extensions;
            var configFileFilters = filters.ConfigFiles;

            return filePaths
                    .Where(path =>
                    {
                        if (this.IsFileInIgnoredDirectory(path)
                            || this.IsFileSizeLargerThanMaximum(path))
                        {
                            return false;
                        }

                        return extensionFilters.Contains(Path.GetExtension(path));
                    })
                    .ToList();
        }

        private bool IsFileInIgnoredDirectory(string filePath)
        {
            foreach (string defaultIgnoreDirectory in this.defaultIgnoreDirectories)
            {
                string[] directories = filePath.Split(Path.DirectorySeparatorChar);

                foreach (string directoryName in directories)
                {
                    if (defaultIgnoreDirectory == directoryName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsFileSizeLargerThanMaximum(string path)
        {
            try
            {
                return new FileInfo(path).Length > MaxFileSize;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private async Task<FiltersDto> GetFiltersAsync()
        {
            if (this.filters == null)
            {
                this.filters = await this.codeClient.GetFiltersAsync();
            }

            return this.filters;
        }
    }
}
