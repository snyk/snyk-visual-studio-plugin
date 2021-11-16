namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provide file path and content for solutions and projects.
    /// </summary>
    public interface IFileProvider
    {
        /// <summary>
        /// Get solution files.
        /// </summary>
        /// <returns>List of file paths.</returns>
        IEnumerable<string> GetFiles();

        /// <summary>
        /// Get solution path.
        /// </summary>
        /// <returns>Path to solution.</returns>
        string GetSolutionPath();

        /// <summary>
        /// Save changed file path.
        /// </summary>
        /// <param name="file">File path.</param>
        void AddChangedFile(string file);

        /// <summary>
        /// Save path to remove in file provider.
        /// </summary>
        /// <param name="file">File path</param>
        void RemoveFile(string file);

        /// <summary>
        /// Get all removed from solution file paths.
        /// </summary>
        /// <returns>List of removed file paths.</returns>
        IEnumerable<string> GetRemovedFiles();

        /// <summary>
        /// Clear added, changed and removed lists.
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// Get added and changed file paths (exclude removed file paths).
        /// </summary>
        /// <returns>List of added and removed file paths.</returns>
        IEnumerable<string> GetChangedFiles();

        /// <summary>
        /// Get all file paths (added, removed, changed).
        /// </summary>
        /// <returns>List of file paths.</returns>
        IEnumerable<string> GetAllChangedFiles();
        /// <param name="filtersService">Filter service implementation</param
        /// <param name="cancellationToken">Token to cancel current task.</param>
        /// <returns>Task.</returns>
    }
}
