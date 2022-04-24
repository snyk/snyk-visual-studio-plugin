namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Common;

    /// <summary>
    /// Cache service for SnykCode.
    /// When user start run a scan and SnykCode client prepare files to upload.
    /// Cache file paths and file hashes.
    /// Add change event listener for file.
    /// If user run a scan and files don’t changed → return cached result.
    /// If user run a scan and files are changed → extend bundle with changed files and return new result.
    /// </summary>
    public interface ICodeCacheService
    {
        /// <summary>
        /// Create <see cref="IDictionary{TKey, TValue}"/> with file path to file content values.
        /// </summary>
        /// <returns>IDictionary.</returns>
        IDictionary<string, string> GetFileHashToContentDictionary();

        /// <summary>
        /// Create <see cref="IDictionary{TKey, TValue}"/> with file path to file content values by provided files list.
        /// </summary>
        /// <param name="files">Provided files list.</param>
        /// <returns>IDictionary.</returns>
        IDictionary<string, string> GetFileHashToContentDictionary(IEnumerable<string> files);

        /// <summary>
        /// Create <see cref="IDictionary{TKey, TValue}"/> with file path to file hash values by provided files list.
        /// </summary>
        /// <param name="files">Provided files list.</param>
        /// <returns>IDictionary.</returns>
        Task<IDictionary<string, string>> GetFilePathToHashDictionaryAsync(IEnumerable<string> files);

        /// <summary>
        /// Create file path to file hash and content dictionary.
        /// </summary>
        /// <param name="files">Source files.</param>
        /// <returns>Dictionariy with file path and file hash and content.</returns>
        IDictionary<string, (string, string)> CreateFilePathToHashAndContentDictionary(IList<string> files);

        /// <summary>
        /// Create <see cref="IDictionary{TKey, TValue}"/> with file path to file hash values.
        /// </summary>
        /// <returns>IDictionary.</returns>
        IDictionary<string, string> GetFilePathToHashDictionary();

        /// <summary>
        /// Initialize cache.
        /// </summary>
        /// <param name="files">Files to initialize cache.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task InitializeAsync(IEnumerable<string> files);

        /// <summary>
        /// Check is cache valid for this project.
        /// </summary>
        /// <param name="project">Project key.</param>
        /// <returns>True if cache for this project exists and valid.</returns>
        bool IsCacheValid();

        /// <summary>
        /// Check is cache exists.
        /// </summary>
        /// <param name="project">Project key.</param>
        /// <returns>True if cache for this project exists.</returns>
        bool IsCacheExists();

        /// <summary>
        /// Get <see cref="AnalysisResult"/> instance for project if it exists.
        /// </summary>
        /// <param name="project">Project key, for example project path.</param>
        /// <returns><see cref="AnalysisResult"/> instance.</returns>
        AnalysisResult GetCachedAnalysisResult();

        /// <summary>
        /// Add <see cref="AnalysisResult"/> instance for project.
        /// </summary>
        /// <param name="analysisResult">AnalysisResult to cache.</param>
        void SetAnalysisResult(AnalysisResult analysisResult);

        /// <summary>
        /// Cached (previous scan) bundle id.
        /// </summary>
        /// <returns>Return bundle id string.</returns>
        string GetCachedBundleId();

        /// <summary>
        /// Set cached bundle id.
        /// </summary>
        /// <param name="id">Bundle id string.</param>
        void SetCachedBundleId(string id);

        /// <summary>
        /// Update cache state according <see cref="IFileProvider"/> data.
        /// </summary>
        /// <param name="fileProvider">File provider instance.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task UpdateAsync(IFileProvider fileProvider);

        /// <summary>
        /// Get file paths in relative format. Input will be like C:\Test\ProjectName\somefile.txt and output will be like /somefile.txt.
        /// </summary>
        /// <param name="files">List of absolute file paths.</param>
        /// <returns>List of relative file paths.</returns>
        Task<IEnumerable<string>> GetRelativeFilePathsAsync(IEnumerable<string> files);
    }
}
