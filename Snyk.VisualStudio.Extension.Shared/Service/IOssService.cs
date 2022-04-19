namespace Snyk.VisualStudio.Extension.Shared.Service
{
    using System.Threading;
    using System.Threading.Tasks;
    using Snyk.VisualStudio.Extension.Shared.CLI;

    /// <summary>
    /// Service for OSS vulnerability scan.
    /// </summary>
    public interface IOssService
    {
        /// <summary>
        /// Scan for OSS vulnerabilities in provided path.
        /// </summary>
        /// <param name="path">Project path to scan.</param>
        /// <param name="token">Cancellation token</param>
        /// <returns><see cref="CliResult"/> object.</returns>
        /// <exception cref="OssScanException">If error on scan.</exception>
        Task<CliResult> ScanAsync(string path, CancellationToken token);

        /// <summary>
        /// Stop current scan.
        /// </summary>
        void StopScan();

        /// <summary>
        /// Gets or sets a value indicating whether is current scan process canceled.
        /// </summary>
        /// <returns>True if canceled.</returns>
        bool IsCurrentScanProcessCanceled();

        /// <summary>
        /// Clear cached value.
        /// </summary>
        void ClearCache();
    }
}
