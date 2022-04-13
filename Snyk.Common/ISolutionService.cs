﻿namespace Snyk.Common
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for solution related functionality.
    /// </summary>
    public interface ISolutionService
    {
        /// <summary>
        /// Gets file provider instance.
        /// </summary>
        IFileProvider FileProvider { get; }

        /// <summary>
        /// Get solution path.
        /// </summary>
        /// <returns>Path string.</returns>
        Task<string> GetPathAsync();

        /// <summary>
        /// Get all solution files.
        /// </summary>
        /// <returns>List of solution files.</returns>
        Task<IEnumerable<string>> GetFilesAsync();

        /// <summary>
        /// Clean solution related variables.
        /// </summary>
        void Clean();

        /// <summary>
        /// Check is folder opened as solution.
        /// </summary>
        /// <returns>True is user open folder as solution.</returns>
        bool IsSolutionOpenedAsFolder();

        /// <summary>
        /// Get full file path by relative file path.
        /// </summary>
        /// <param name="file">Relative file path.</param>
        /// <returns>Full file path.</returns>
        Task<string> GetFileFullPathAsync(string file);

        /// <summary>
        /// Gets a value indicating whether is solution open.
        /// </summary>
        /// <returns>True if solution open.</returns>
        bool IsSolutionOpen();

        /// <summary>
        /// Find root directory for all paths.
        /// </summary>
        /// <param name="rootDir">Initial root directory.</param>
        /// <param name="paths">All paths.</param>
        /// <returns>Root directory for all paths.</returns>
        string FindRootDirectoryForSolutionProjects(string rootDir, IList<string> paths);
    }
}
