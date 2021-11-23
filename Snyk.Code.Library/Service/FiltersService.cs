namespace Snyk.Code.Library.Service
{
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
                    .Where(path => !this.IsFileInIgnoredDirectory(path) && (extensionFilters.Contains(Path.GetExtension(path)) || configFileFilters.Contains(Path.GetFileName(path))))
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
