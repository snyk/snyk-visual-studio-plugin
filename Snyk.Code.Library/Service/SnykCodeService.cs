namespace Snyk.Code.Library.Service
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
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
        public async Task<AnalysisResult> ScanAsync(IList<string> filePaths, string basePath = "")
        {
            Logger.Debug("Start Snyk scan");

            var filteredFiles = await this.filtersService.FilterFilesAsync(filePaths);

            var filePathToHashDict = this.CreateFilePathToHashDictionary(filteredFiles, basePath);

            var resultBundle = await this.bundleService.CreateBundleAsync(filePathToHashDict);

            await this.bundleService.UploadMissingFilesAsync(resultBundle, basePath);

            return await this.analysisService.GetAnalysisAsync(resultBundle.Id);
        }

        /// <inheritdoc/>
        public async Task<AnalysisResult> ScanAsync(IFileProvider fileProvider)
        {
            Logger.Debug("Start Snyk scan");

            await fileProvider.InitializeAsync();

            var filePathToHashDict = fileProvider.CreateFilePathToHashDictionary();

            var resultBundle = await this.bundleService.CreateBundleAsync(filePathToHashDict);

            await this.bundleService.UploadMissingFilesAsync(resultBundle, fileProvider);

            return await this.analysisService.GetAnalysisAsync(resultBundle.Id);
        }

        private IDictionary<string, string> CreateFilePathToHashDictionary(IList<string> filePaths, string basePath = "")
        {
            var filePathToHashDict = new Dictionary<string, string>();

            foreach (string filePath in filePaths)
            {
                try {
                    string fullFilePath = filePath;

                    if (basePath != string.Empty)
                    {
                        fullFilePath = basePath + filePath;
                    }

                    string sgadsf = Directory.GetCurrentDirectory();

                    string fileContent = File.ReadAllText(fullFilePath, Encoding.UTF8);

                    string fileHash = Sha256.ComputeHash(fileContent);

                    string codeFilePath = filePath;

                    if (codeFilePath.IndexOf("\\\\") != -1)
                    {
                        codeFilePath = filePath.Replace("\\\\", "/");
                    }

                    if (codeFilePath.IndexOf("\\") != -1)
                    {
                        codeFilePath = filePath.Replace("\\", "/");
                    }

                    filePathToHashDict.Add(codeFilePath, fileHash);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
            }

            return filePathToHashDict;
        }
    }
}
