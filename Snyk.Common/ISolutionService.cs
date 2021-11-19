namespace Snyk.Common
{
    using System.Collections.Generic;

    /// <summary>
    /// Service for solution related functionality.
    /// </summary>
    public interface ISolutionService
    {
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
    }
}
