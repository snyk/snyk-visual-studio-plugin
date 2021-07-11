namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Common logic for get filters for SnykCode. Filters are file extensions (as an example '.cs', '.java', '.ts', '.js') and configuration files (as an example '.gitignore').
    /// </summary>
    public interface IFiltersService
    {
        Task<IList<string>> FilterFilesAsync(IList<string> filePaths);
    }
}
