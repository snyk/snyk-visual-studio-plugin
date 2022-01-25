namespace Snyk.VisualStudio.Extension.Shared.Service
{
    using System.Threading;
    using Serilog;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.CLI;
    using Snyk.VisualStudio.Extension.Shared.Settings;

    /// <inheritdoc/>
    public class OssService : IOssService
    {
        private static readonly ILogger Logger = LogManager.ForContext<OssService>();

        private SnykCli cli;

        /// <inheritdoc/>
        public bool IsCurrentScanProcessCanceled() => this.cli.ConsoleRunner.IsStopped;

        /// <inheritdoc/>
        public CliResult Scan(string path, ISnykOptions options, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            this.cli = new SnykCli
            {
                Options = options,
            };

            Logger.Information("Snyk Extension options");
            Logger.Information("Custom Endpoint is {CustomEndpoint}", options.CustomEndpoint);
            Logger.Information("Organization {Organization}", options.Organization);
            Logger.Information("Ignore Unknown CA is {IgnoreUnknownCA}", options.IgnoreUnknownCA);
            Logger.Information("Additional Options is {AdditionalOptions}", options.AdditionalOptions);
            Logger.Information("Is Scan All Projects is {IsScanAllProjects}", options.IsScanAllProjects);
            Logger.Information("Solution path is {SolutionPath}", path);
            Logger.Information("Start scan");

            token.ThrowIfCancellationRequested();

            var cliResult = this.cli.Scan(path);

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
