namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Common logic for get filters for SnykCode. Filters are file extensions (as an example '.cs', '.java', '.ts', '.js') and configuration files (as an example '.gitignore').
    /// </summary>
    public interface IFiltersService
    {
        /// <summary>
        /// Filter files by SnykCode filters.
        /// </summary>
        /// <param name="filePaths">Project file paths.</param>
        /// <param name="cancellationToken">Token to cancel current task.</param>
        /// <returns>Filtered file paths.</returns>
        Task<IList<string>> FilterFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
    }
}
