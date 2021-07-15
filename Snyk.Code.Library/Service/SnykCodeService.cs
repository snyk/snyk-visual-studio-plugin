namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Common;
    using Snyk.Code.Library.Domain.Analysis;

    /// <inheritdoc/>
    public class SnykCodeService : ISnykCodeService
    {
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
        public async Task<AnalysisResult> ScanAsync(IList<string> filePaths)
        {
            var filteredFiles = await this.filtersService.FilterFilesAsync(filePaths);

            var filePathToHashDict = this.CreateFilePathToHashDictionary(filteredFiles);

            var resultBundle = await this.bundleService.CreateBundleAsync(filePathToHashDict);

            _ = await this.bundleService.UploadFilesAsync(resultBundle.Id, this.CreateFileHashToContentDictionary(resultBundle.MissingFiles));

            resultBundle = await this.bundleService.CheckBundleAsync(resultBundle.Id);

            if (resultBundle.MissingFiles.Count > 0)
            {
                _ = await this.bundleService.UploadFilesAsync(resultBundle.Id, this.CreateFileHashToContentDictionary(resultBundle.MissingFiles));
            }

            return await this.analysisService.GetAnalysisAsync(resultBundle.Id);
        }

        private IDictionary<string, string> CreateFilePathToHashDictionary(IList<string> filePaths)
        {
            var filePathToHashDict = new Dictionary<string, string>();

            foreach (string filePath in filePaths)
            {
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string fileHash = Sha256.ComputeHash(fileContent);

                filePathToHashDict.Add(filePath, fileHash);
            }

            return filePathToHashDict;
        }

        private IDictionary<string, string> CreateFileHashToContentDictionary(IList<string> filePaths)
        {
            var fileHashToContentDict = new Dictionary<string, string>();

            foreach (string filePath in filePaths)
            {
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string fileHash = Sha256.ComputeHash(fileContent);

                fileHashToContentDict.Add(fileHash, fileContent);
            }

            return fileHashToContentDict;
        }
    }
}
