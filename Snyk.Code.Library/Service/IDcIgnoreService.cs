namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Service for dcignore and gitignore functionality.
    /// </summary>
    public interface IDcIgnoreService
    {
        /// <summary>
        /// Filter files by .gitignore and .dcignore (if exists) in project path.
        /// </summary>
        /// <param name="folderPath">Full path to folder.</param>
        /// <param name="filePaths">List of files in project to filter.</param>
        /// <returns>Filtered list of files.</returns>
        IEnumerable<string> FilterFiles(string folderPath, IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
    }
}
