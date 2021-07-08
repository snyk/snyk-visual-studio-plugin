namespace Snyk.Code.Library
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto;

    /// <summary>
    /// Contains gigh level busines logic for SnykCode APIs.
    /// </summary>
    public class SnykCodeService
    {
        private ISnykCodeClient codeClient;

        private FiltersDto filters;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeService"/> class.
        /// </summary>
        /// <param name="snykCodeClient">SnykCode client implementation.</param>
        public SnykCodeService(ISnykCodeClient snykCodeClient) => this.codeClient = snykCodeClient;

        /// <summary>
        /// Get filters information from server if it's not initialized yet and filter file pahts with supported extensions and configuration files.
        /// </summary>
        /// <param name="filePaths">Input file paths to filter.</param>
        /// <returns>Filtered file pahts (only supported by SnykCode files by extension).</returns>
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
