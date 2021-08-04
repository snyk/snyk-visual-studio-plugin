namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.IO;
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
        public async Task<IList<string>> FilterFilesAsync(IList<string> filePaths)
        {
            Logger.Debug("Filter Files count {Count}.", filePaths.Count);

            var filters = await this.GetFiltersAsync();
            var extensionFilters = filters.Extensions;
            var configFileFilters = filters.ConfigFiles;

            var filteredFiles = new List<string>();

            foreach (string filePath in filePaths)
            {
                if (this.IsFileInIgnoredDirectory(filePath))
                {
                    continue;
                }

                if (extensionFilters.Contains(Path.GetExtension(filePath)) || configFileFilters.Contains(Path.GetFileName(filePath)))
                {
                    filteredFiles.Add(filePath);
                }
            }

            Logger.Debug("Filtered Files count {Count}.", filteredFiles.Count);

            return filteredFiles;
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
                Logger.Debug("Request GetFilters.");

                this.filters = await this.codeClient.GetFiltersAsync();
            }

            return this.filters;
        }
    }
}
