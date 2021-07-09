namespace Snyk.Code.Library
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Domain;

    /// <summary>
    /// Contains high level busines logic for SnykCode APIs.
    /// </summary>
    public interface ISnykCodeService
    {
        /// <summary>
        /// Scan path for code vulnerabilities.
        /// </summary>
        /// <param name="filePaths">Source file paths.</param>
        /// <returns><see cref="AnalysisResult"/> object.</returns>
        Task<AnalysisResult> ScanAsync(IList<string> filePaths);
    }
}
