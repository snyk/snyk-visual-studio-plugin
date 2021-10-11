namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;

    /// <summary>
    /// Service for dcignore and gitignore functionality.
    /// </summary>
    public interface IDcIgnoreService
    {
        /// <summary>
        /// Filter files by .gitignore rules.
        /// </summary>
        /// <param name="filePaths">Project file paths.</param>
        /// <returns>Filtered file paths.</returns>
        IEnumerable<string> FilterFilesByGitIgnore(IEnumerable<string> filePaths);

        /// <summary>
        /// Filter files by .dcignore rules.
        /// </summary>
        /// <param name="filePaths">Project file paths.</param>
        /// <returns>Filtered file paths.</returns>
        IEnumerable<string> FilterFilesByDcIgnore(IEnumerable<string> filePaths);

        /// <summary>
        /// Filter files by .gitignore and .dcignore (if exists) in project path.
        /// </summary>
        /// <param name="filePaths">List of files in project to filter.</param>
        /// <returns>Filtered list of files.</returns>
        IEnumerable<string> FilterFiles(IEnumerable<string> filePaths);

        /// <summary>
        /// Create .dcignore file if no .gitignore and .dcignore.
        /// </summary>
        /// <param name="projectPath">Source project path.</param>
        void CreateDcIgnoreIfNeeded();
    }
}
