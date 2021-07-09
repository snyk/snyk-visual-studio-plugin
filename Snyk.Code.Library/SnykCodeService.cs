namespace Snyk.Code.Library
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Common;
    using Snyk.Code.Library.Domain;

    /// <inheritdoc/>
    public class SnykCodeService : ISnykCodeService
    {
        private ISnykCodeClient codeClient;

        private IFiltersService filtersService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeService"/> class.
        /// </summary>
        /// <param name="codeClient">SnykCode client implementation.</param>
        public SnykCodeService(ISnykCodeClient codeClient)
        {
            this.codeClient = codeClient;

            this.filtersService = new FiltersService(this.codeClient);
        }

        /// <inheritdoc/>
        public async Task<AnalysisResult> ScanAsync(IList<string> filePaths)
        {
            var filteredFiles = await this.filtersService.FilterFilesAsync(filePaths);

            var filePathToHashDict = this.CreateFilePathToHashDictionary(filteredFiles);

            var bundleService = new BundleService(this.codeClient);

            var resultBundle = await bundleService.CreateBundleAsync(filePathToHashDict);

            _ = await bundleService.UploadFilesAsync(resultBundle.Id, this.CreateFileHashToContentDictionary(resultBundle.MissingFiles));

            resultBundle = await bundleService.CheckBundleAsync(resultBundle.Id);

            if (resultBundle.MissingFiles.Count > 0)
            {
                _ = await bundleService.UploadFilesAsync(resultBundle.Id, this.CreateFileHashToContentDictionary(resultBundle.MissingFiles));
            }

            var analysisService = new AnalysisService(this.codeClient);

            return await analysisService.GetAnalysisAsync(resultBundle.Id);
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

        /// <summary>
        /// TODO: Add cache.
        /// </summary>
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
