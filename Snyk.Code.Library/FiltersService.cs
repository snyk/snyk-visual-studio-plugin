namespace Snyk.Code.Library
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto;

    /// <inheritdoc/>
    public class FiltersService : IFiltersService
    {
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
            var filters = await this.GetFiltersAsync();
            var extensionFilters = filters.Extensions;
            var configFileFilters = filters.ConfigFiles;

            var filteredFiles = new List<string>();

            foreach (string filePath in filePaths)
            {
                if (extensionFilters.Contains(Path.GetExtension(filePath)) || configFileFilters.Contains(Path.GetFileName(filePath)))
                {
                    filteredFiles.Add(filePath);
                }
            }

            return filteredFiles;
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
