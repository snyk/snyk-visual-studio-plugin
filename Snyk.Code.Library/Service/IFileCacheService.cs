using System.Collections.Generic;

namespace Snyk.Code.Library.Service
{
    /// <summary>
    /// Cache service for SnykCode functionality.
    /// </summary>
    public interface IFileCacheService
    {
        /// <summary>
        /// Get file content from cache by file path.
        /// </summary>
        /// <param name="filePathKey">Key is file path.</param>
        /// <returns>Returns file content.</returns>
        string GetFileContent(string filePathKey);

        /// <summary>
        /// Get file hash by key.
        /// </summary>
        /// <param name="filePathKey">Key is file path.</param>
        /// <returns>Returns file hash in SHA-256.</returns>
        string GetFileHash(string filePathKey);

        /// <summary>
        /// Setup base file path to hash cache and content.
        /// </summary>
        /// <param name="filePaths">List of file paths.</param>
        void Setup(IList<string> filePaths);
    }
}
