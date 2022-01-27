namespace Snyk.Common
{
    using System.Collections.Generic;

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
        string GetPath();

        /// <summary>
        /// Get all solution files.
        /// </summary>
        /// <returns>List of solution files.</returns>
        IEnumerable<string> GetFiles();

        /// <summary>
        /// Clean solution related variables.
        /// </summary>
        void Clean();

        /// <summary>
        /// Return project type.
        /// </summary>
        /// <returns>Flat or web site, solution or folder.</returns>
        string GetProjectType();
    }
}
