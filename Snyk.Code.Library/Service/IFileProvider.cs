namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Provide file path and content for solutions and projects.
    /// </summary>
    public interface IFileProvider
    {
        /// <summary>
        /// Create <see cref="IDictionary{TKey, TValue}"/> with file path to file content values.
        /// </summary>
        /// <param name="files">Files list.</param>
        /// <returns>IDictionary.</returns>
        IDictionary<string, string> CreaateFileHashToContentDictionary(IList<string> files);

        /// <summary>
        /// Create <see cref="IDictionary{TKey, TValue}"/> with file path to file hash values.
        /// </summary>
        /// <returns>IDictionary.</returns>
        IDictionary<string, string> CreateFilePathToHashDictionary();

        /// <summary>
        /// Filter files with <see cref="IFiltersService"/>.
        /// </summary>
        /// <param name="filtersService">Filter service implementation</param>
        /// <returns>Task.</returns>
        Task FilterFilesAsync(IFiltersService filtersService);
    }
}
