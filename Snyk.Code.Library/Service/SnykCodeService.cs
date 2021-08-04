namespace Snyk.Code.Library.Service
{
    using System.Threading.Tasks;
    using Serilog;
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
        public async Task<AnalysisResult> ScanAsync(IFileProvider fileProvider)
        {
            Logger.Debug("Start Snyk scan");

            await fileProvider.FilterFilesAsync(this.filtersService);

            var filePathToHashDict = fileProvider.CreateFilePathToHashDictionary();

            var resultBundle = await this.bundleService.CreateBundleAsync(filePathToHashDict);

            await this.bundleService.UploadMissingFilesAsync(resultBundle, fileProvider);

            return await this.analysisService.GetAnalysisAsync(resultBundle.Id);
        }
    }
}
