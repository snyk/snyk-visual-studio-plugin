namespace Snyk.Code.Library.Service
{
    using System.Threading.Tasks;
    using Snyk.Code.Library.Domain.Analysis;

    /// <summary>
    /// Contains high level busines logic for SnykCode APIs.
    /// </summary>
    public interface ISnykCodeService
    {
        /// <summary>
        /// Scan source code provided for code vulnerabilities.
        /// </summary>
        /// <param name="fileProvider">Provider for files to scan.</param>
        /// <returns><see cref="AnalysisResult"/> object.</returns>
        Task<AnalysisResult> ScanAsync(IFileProvider fileProvider);
    }
}
