namespace Snyk.Code.Library.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

        private ICodeCacheService codeCacheService;

        private IDcIgnoreService dcIgnoreService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeService"/> class.
        /// </summary>
        /// <param name="bundleService"><see cref="IBundleService"/> instance.</param>
        /// <param name="analysisService"><see cref="IAnalysisService"/> instance.</param>
        /// <param name="filtersService"><see cref="IFiltersService"/> instance.</param>
        /// <param name="codeCacheService"><see cref="ICodeCacheService"/> instance.</param>
        /// <param name="dcIgnoreService"><see cref="IDcIgnoreService"/> instance.</param>
        public SnykCodeService(
            IBundleService bundleService,
            IAnalysisService analysisService,
            IFiltersService filtersService,
            ICodeCacheService codeCacheService,
            IDcIgnoreService dcIgnoreService)
        {
            this.filtersService = filtersService;

            this.bundleService = bundleService;

            this.analysisService = analysisService;

            this.codeCacheService = codeCacheService;

            this.dcIgnoreService = dcIgnoreService;
        }

        /// <summary>
        /// Delegate interface to process progress update events.
        /// </summary>
        /// <param name="state">Current progress state (step).</param>
        /// <param name="progress">Current progress percentage.</param>
        public delegate void FireScanCodeProgressUpdate(SnykCodeScanState state, int progress);

        /// <summary>
        /// Cli scanning started event handler.
        /// </summary>
        private event EventHandler<SnykCodeEventArgs> ScanProgress;

        public EventHandler<SnykCodeEventArgs> ScanEventHandler { get => this.ScanProgress; set => this.ScanProgress = value; }

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
            Logger.Debug("Start SnykCode scanning...");
            this.FireScanProgressEvent(SnykCodeScanState.Preparing, 0);
            var codeCache = this.LazyGetCacheService(fileProvider);

            if (!codeCache.IsCacheExists())
            {
                return await this.NewScanAsync(fileProvider, codeCache, cancellationToken);
            }

            var filteredChangedFiles = await this.GetFilteredFilesAsync(await fileProvider.GetSolutionPathAsync(), fileProvider.GetAllChangedFiles());

            if (this.AnyFilesChangedInSolution(filteredChangedFiles))
            {
                this.FireScanProgressEvent(SnykCodeScanState.Analysing, 100);

                return codeCache.GetCachedAnalysisResult();
            }

            return await this.UpdatePreviousScanAsync(fileProvider, filteredChangedFiles, codeCache, cancellationToken);
        }

        /// <inheritdoc/>
        public void Clean() => this.codeCacheService = null;

        private async Task<AnalysisResult> NewScanAsync(IFileProvider fileProvider, ICodeCacheService codeCache, CancellationToken cancellationToken = default)
        {
            this.FireScanProgressEvent(SnykCodeScanState.Preparing, 0);

            var solutionFiles = await fileProvider.GetFilesAsync();

            Logger.Information("Solution files to filter count {Count}", solutionFiles.Count());

            var files = await this.GetFilteredFilesAsync(await fileProvider.GetSolutionPathAsync(), solutionFiles);

            Logger.Information("Filtered files count {Count}", files?.Count());

            if (files == null || files.Count() == 0)
            {
                return null;
            }

            await codeCache.InitializeAsync(files);

            var filePathToHashDict = codeCache.GetFilePathToHashDictionary();

            cancellationToken.ThrowIfCancellationRequested();

            var resultBundle = await this.bundleService.CreateBundleAsync(filePathToHashDict, cancellationToken: cancellationToken);

            var scanCodeProgressUpdater = new FireScanCodeProgressUpdate(this.FireScanProgressEvent);

            await this.bundleService.UploadMissingFilesAsync(
                resultBundle,
                codeCache,
                scanCodeProgressUpdater,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var analysisResult = await this.analysisService.GetAnalysisAsync(resultBundle.Id, scanCodeProgressUpdater, cancellationToken: cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            this.UpdateCache(resultBundle.Id, analysisResult);

            fileProvider.ClearHistory();

            cancellationToken.ThrowIfCancellationRequested();

            this.FireScanProgressEvent(SnykCodeScanState.Analysing, 100);

            return analysisResult;
        }

        private async Task<AnalysisResult> UpdatePreviousScanAsync(
            IFileProvider fileProvider,
            IEnumerable<string> changedFiles,
            ICodeCacheService cacheService,
            CancellationToken cancellationToken = default)
        {
            await cacheService.UpdateAsync(fileProvider);

            var extendFilePathToHashDict = await cacheService.GetFilePathToHashDictionaryAsync(changedFiles);

            string bundleId = cacheService.GetCachedBundleId();

            var removedFiles = await cacheService.GetRelativeFilePathsAsync(fileProvider.GetRemovedFiles());

            var extendedBundle = await this.bundleService.ExtendBundleAsync(bundleId, extendFilePathToHashDict, removedFiles);

            var scanCodeProgressUpdater = new FireScanCodeProgressUpdate(this.FireScanProgressEvent);

            await this.bundleService.UploadMissingFilesAsync(
                extendedBundle,
                cacheService,
                scanCodeProgressUpdater,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var analysisResult = await this.analysisService.GetAnalysisAsync(extendedBundle.Id, scanCodeProgressUpdater, cancellationToken: cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            this.UpdateCache(extendedBundle.Id, analysisResult);

            cancellationToken.ThrowIfCancellationRequested();

            fileProvider.ClearHistory();

            return analysisResult;
        }

        private async Task<IEnumerable<string>> GetFilteredFilesAsync(string rootDirectoryPath, IEnumerable<string> files, CancellationToken cancellationToken = default)
        {
            files = this.dcIgnoreService.FilterFiles(rootDirectoryPath, files);

            return await this.filtersService.FilterFilesAsync(files, cancellationToken);
        }

        private void UpdateCache(string bundleId, AnalysisResult analysisResult)
        {
            this.codeCacheService.SetAnalysisResult(analysisResult);

            this.codeCacheService.SetCachedBundleId(bundleId);
        }

        private bool AnyFilesChangedInSolution(IEnumerable<string> files) => files.IsNullOrEmpty();

        private ICodeCacheService LazyGetCacheService(IFileProvider fileProvider)
        {
            var codeCache = this.codeCacheService;
            if (codeCache == null)
            {
                codeCache = new CodeCacheService(fileProvider);
                this.codeCacheService = codeCache;
            }

            return codeCache;
        }

        private void FireScanProgressEvent(SnykCodeScanState state, int progress)
            => this.ScanProgress?.Invoke(this, new SnykCodeEventArgs { ScanState = state, Progress = progress, });
    }
}
