namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Domain.Analysis;

    /// <summary>
    /// Contains high level busines logic for SnykCode APIs.
    /// </summary>
    public interface ISnykCodeService
    {
        /// <summary>
        /// Scan path for code vulnerabilities.
        /// </summary>
        /// <param name="filePaths">Source file paths.</param>
        /// <param name="basePath">Base path of project.</param>
        /// <returns><see cref="AnalysisResult"/> object.</returns>
        Task<AnalysisResult> ScanAsync(IList<string> filePaths, string basePath = "");
    }
}
