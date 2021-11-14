namespace Snyk.Code.Library.Service
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Code.Library.Domain;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Common;

    /// <inheritdoc/>
    public class SnykCodeService : ISnykCodeService
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykCodeService>();

        private IBundleService bundleService;

        private IAnalysisService analysisService;

        private IFiltersService filtersService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeService"/> class.
        /// </summary>
        /// <param name="bundleService"><see cref="IBundleService"/> instance.</param>
        /// <param name="analysisService"><see cref="IAnalysisService"/> instance.</param>
        /// <param name="filtersService"><see cref="IFiltersService"/> instance.</param>
        public SnykCodeService(IBundleService bundleService, IAnalysisService analysisService, IFiltersService filtersService)
        {
            this.filtersService = filtersService;

            this.bundleService = bundleService;

            this.analysisService = analysisService;
        }

        /// <inheritdoc/>
        public string GetSnykCodeErrorMessage(Exception sourceException)
        {
            try
            {
                var snykCodeError = Json.Deserialize<SnykCodeError>(sourceException.Message);

                return $"Message: {snykCodeError.Message}. Code: {snykCodeError.Code}";
            }
            catch (Exception e)
            {
                Logger.Error(sourceException, "Failed to obtain Snyk Code error message");
                Logger.Error(e, string.Empty);

                return sourceException.Message;
            }
        }

        /// <inheritdoc/>
        public async Task<AnalysisResult> ScanAsync(IFileProvider fileProvider, CancellationToken cancellationToken = default)
        {
            Logger.Information("Start SnykCode scanning...");

            await fileProvider.FilterFilesAsync(this.filtersService);

            var filePathToHashDict = fileProvider.CreateFilePathToHashDictionary();

            var resultBundle = await this.bundleService.CreateBundleAsync(filePathToHashDict, cancellationToken: cancellationToken);

            await this.bundleService.UploadMissingFilesAsync(resultBundle, fileProvider, cancellationToken);

            return await this.analysisService.GetAnalysisAsync(resultBundle.Id, cancellationToken: cancellationToken);
        }
    }
}
