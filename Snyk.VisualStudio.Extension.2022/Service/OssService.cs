namespace Snyk.VisualStudio.Extension.Shared.Service
{
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.CLI;

    /// <inheritdoc/>
    public class OssService : IOssService
    {
        private static readonly ILogger Logger = LogManager.ForContext<OssService>();

        private ICli cli;

        private CliResult cachedCliResult;

        private ISnykServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="OssService"/> class.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider.</param>
        public OssService(ISnykServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

        /// <inheritdoc/>
        public void ClearCache() => this.cachedCliResult = null;

        /// <inheritdoc/>
        public bool IsCurrentScanProcessCanceled() => this.cli.ConsoleRunner.IsStopped;

        /// <inheritdoc/>
        public async Task<CliResult> ScanAsync(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (this.cachedCliResult != null)
            {
                return this.cachedCliResult;
            }

            var options = this.serviceProvider.Options;

            this.cli = this.serviceProvider.NewCli();

            Logger.Information("Custom Endpoint is {CustomEndpoint}", options.CustomEndpoint);
            Logger.Information("Organization {Organization}", options.Organization);
            Logger.Information("Ignore Unknown CA is {IgnoreUnknownCA}", options.IgnoreUnknownCA);
            Logger.Information("Additional Options is {AdditionalOptions}", await options.GetAdditionalOptionsAsync());
            Logger.Information("Is Scan All Projects is {IsScanAllProjects}", await options.IsScanAllProjectsAsync());
            Logger.Information("Solution path is {SolutionPath}", path);

            Logger.Information("Start scan");

            token.ThrowIfCancellationRequested();

            var cliResult = await this.cli.ScanAsync(path);

            Logger.Information("Scan finished. Is successful: {IsSuccessful}", cliResult.IsSuccessful());

            token.ThrowIfCancellationRequested();

            if (!cliResult.IsSuccessful())
            {
                throw new OssScanException
                {
                    Error = cliResult.Error,
                };
            }

            token.ThrowIfCancellationRequested();

            this.cachedCliResult = cliResult;

            return cliResult;
        }

        /// <inheritdoc/>
        public void StopScan()
        {
            if (this.cli != null && this.cli?.ConsoleRunner != null)
            {
                this.cli?.ConsoleRunner?.Stop();
            }
        }
    }
}
