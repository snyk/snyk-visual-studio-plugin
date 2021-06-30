namespace Snyk.Code.Library
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto;
    using Snyk.Code.Library.Common;
    using Snyk.Code.Library.Domain;

    /// <summary>
    /// BundleService contains logic on top of <see cref="SnykCodeClient"/> class for SnykCode functionality.
    /// </summary>
    public class BundleService
    {
        private readonly SnykCodeClient codeClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="BundleService"/> class.
        /// </summary>
        /// <param name="baseUrl">Base URL for deproxy.</param>
        /// <param name="token">User token.</param>
        public BundleService(string baseUrl, string token) => this.codeClient = new SnykCodeClient(baseUrl, token);

        /// <summary>
        /// Create new <see cref="BundleResponseDto"/> and get result <see cref="BundleResponseDto"/> object.
        /// If payload &lt; 4 Mb it just send this bundle and return results.
        /// If payload &gt; 4 Mb it will:
        ///      Split initial bundle on list of bundles (chunks).
        ///      Call Create bundle REST API for first bundle in list.
        ///      For all other bundles it will Extend bundle.
        ///      Return last bundle as result.
        /// </summary>
        /// <param name="pathToHashFileDict">Files dictionary (file path - file hash) for new bundle.</param>
        /// <param name="maxChunkSize">Maximum allowed bundle size.</param>
        /// <returns>Bundle object with bundle id, missing files and upload url.</returns>
        public async Task<Bundle> CreateBundleAsync(Dictionary<string, string> pathToHashFileDict, int maxChunkSize = SnykCodeClient.MaxBundleSize)
        {
            if (pathToHashFileDict == null || pathToHashFileDict.IsNullOrEmpty())
            {
                throw new ArgumentException("Files list is null or empty.");
            }

            int payloadSize = this.CalculateFilesSize(pathToHashFileDict);

            Task<BundleResponseDto> bundleDto = null;

            if (payloadSize < maxChunkSize)
            {
                bundleDto = this.codeClient.CreateBundleAsync(pathToHashFileDict);
            }
            else
            {
                bundleDto = this.ProcessCreateLargeBundleAsync(pathToHashFileDict, maxChunkSize);
            }

            return this.MapDtoBundleToDomain(await bundleDto);
        }

        /// <summary>
        /// Creates a new bundle based on a previously uploaded one.
        /// This method wrap functionality to extend bundle if it's small by size or make few chunks and extend by chunks.
        /// </summary>
        /// <param name="bundleId">Already created bundle id.</param>
        /// <param name="pathToHashFileDict">Files to add in bundle.</param>
        /// <param name="filesToRemovePaths">Files to remove in bundle.</param>
        /// <param name="maxChunkSize">Maximum bundle chunk size. By default it is 4 Mb.</param>
        /// <returns>Result extended bundle.</returns>
        public async Task<Bundle> ExtendBundleAsync(
            string bundleId,
            Dictionary<string, string> pathToHashFileDict,
            List<string> filesToRemovePaths,
            int maxChunkSize = SnykCodeClient.MaxBundleSize)
        {
            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            if (pathToHashFileDict.IsNullOrEmpty() && filesToRemovePaths.IsNullOrEmpty())
            {
                throw new ArgumentException("On Extend Bundle files or removed files are empty.");
            }

            int payloadSize = this.CalculateFilesSize(pathToHashFileDict) + this.CalculateFilesSize(filesToRemovePaths);

            Task<BundleResponseDto> bundleDto = null;

            // If payload < 4 max bundle chunk size just send this bundle and return results.
            if (payloadSize < maxChunkSize)
            {
                bundleDto = this.codeClient.ExtendBundleAsync(bundleId, pathToHashFileDict, filesToRemovePaths);
            }
            else
            {
                bundleDto = this.ProcessExtendLargeBundleAsync(bundleId, pathToHashFileDict, filesToRemovePaths, maxChunkSize);
            }

            return this.MapDtoBundleToDomain(await bundleDto);
        }

        /// <summary>
        /// Split bundle to list of bundles by maximun bundle size.
        /// </summary>
        /// <param name="pathToHashFileDict">Source files dictionary.</param>
        /// <param name="maxChunkSize">Maximum chunk size. By default it's 4 Mb.</param>
        /// <returns>List of smaller file dictionaries.</returns>
        public List<Dictionary<string, string>> SplitFilesToChunkListsBySize(Dictionary<string, string> pathToHashFileDict, int maxChunkSize = SnykCodeClient.MaxBundleSize)
        {
            var fileDictionaries = new List<Dictionary<string, string>>();

            if (pathToHashFileDict.IsNullOrEmpty())
            {
                return fileDictionaries;
            }

            int bundleSize = 0;

            var fileDictionary = new Dictionary<string, string>();

            foreach (var filePair in pathToHashFileDict)
            {
                int fileSize = this.CalculateFilePairSize(filePair);

                if (fileSize > maxChunkSize)
                {
                    throw new SnykCodeException("One file size bigger then max one chunk size.");
                }

                if (bundleSize + fileSize > maxChunkSize)
                {
                    // Save previous dictionary and create new.
                    fileDictionaries.Add(fileDictionary);

                    fileDictionary = new Dictionary<string, string>();

                    bundleSize = 0;
                }

                fileDictionary.Add(filePair.Key, filePair.Value);

                bundleSize += fileSize;
            }

            fileDictionaries.Add(fileDictionary);

            return fileDictionaries;
        }

        /// <summary>
        /// Split removed files list to smaller lists by maximum chunk size.
        /// </summary>
        /// <param name="removedFiles">Source list of removed files.</param>
        /// <param name="maxChunkSize">Maximum chunk size for upload.</param>
        /// <returns>List of small removed file lists.</returns>
        public List<List<string>> SplitRemovedFilesToChunkListsBySize(List<string> removedFiles, int maxChunkSize = SnykCodeClient.MaxBundleSize)
        {
            var chunkLists = new List<List<string>>();

            if (removedFiles.IsNullOrEmpty())
            {
                return chunkLists;
            }

            int chunkSize = 0;
            var chunkList = new List<string>();

            foreach (string removeFile in removedFiles)
            {
                int fileSize = this.CalculatePayloadSize(removeFile);

                if (fileSize > maxChunkSize)
                {
                    throw new SnykCodeException("One remove file size bigger then max one chunk size.");
                }

                if (chunkSize + fileSize > maxChunkSize)
                {
                    // Save previous chunkList and create new.
                    chunkLists.Add(chunkList);

                    chunkList = new List<string>();

                    chunkSize = 0;
                }

                chunkList.Add(removeFile);

                chunkSize += fileSize;
            }

            chunkLists.Add(chunkList);

            return chunkLists;
        }

        /// <summary>
        /// Split big bundle to list of small bundles and extend bundle using this "chunk" bundles.
        /// </summary>
        /// <param name="bundleId">Already created bundle id to extend.</param>
        /// <param name="pathToHashFileDict">Files data for extend.</param>
        /// <param name="removedFiles">Removed files data for delete in extend bundle.</param>
        /// <param name="maxChunkSize">Maximum bundle size. By default it's 4 Mb.</param>
        /// <returns>Result Bundle object from server.</returns>
        public async Task<BundleResponseDto> ProcessExtendLargeBundleAsync(
            string bundleId,
            Dictionary<string, string> pathToHashFileDict,
            List<string> removedFiles,
            int maxChunkSize = SnykCodeClient.MaxBundleSize)
        {
            var bundleDto = new BundleResponseDto
            {
                Id = bundleId,
            };

            if (!removedFiles.IsNullOrEmpty())
            {
                bundleDto = await this.ExtendBundleWithRemovedFilesAsync(bundleDto.Id, removedFiles, maxChunkSize);
            }

            if (!pathToHashFileDict.IsNullOrEmpty())
            {
                bundleDto = await this.ExtendBundleWithFilesAsync(bundleDto.Id, pathToHashFileDict, maxChunkSize);
            }

            return bundleDto;
        }

        /// <summary>
        /// Extend (update) bundle with removed files.
        /// </summary>
        /// <param name="bundleId">Source bundle id to extend.</param>
        /// <param name="pathToHashFileDict">Files to update extend bundle.</param>
        /// <param name="maxChunkSize">Maximum bundle size. By default it's 4 Mb.</param>
        /// <returns>Result Bundle object from server.</returns>
        public async Task<BundleResponseDto> ExtendBundleWithFilesAsync(string bundleId, Dictionary<string, string> pathToHashFileDict, int maxChunkSize = SnykCodeClient.MaxBundleSize)
        {
            var fileDictionaries = this.SplitFilesToChunkListsBySize(pathToHashFileDict, maxChunkSize);

            var firstFiles = fileDictionaries[0];

            var resultBundleDto = await this.codeClient.ExtendBundleAsync(bundleId, firstFiles, Enumerable.Empty<string>().ToList());

            fileDictionaries.Remove(firstFiles);

            // Call Extend Bundle REST API for bundles.
            foreach (var filesDictionary in fileDictionaries)
            {
                resultBundleDto = await this.codeClient.ExtendBundleAsync(resultBundleDto.Id, filesDictionary, Enumerable.Empty<string>().ToList());
            }

            // Last created bundle is result bundle.
            return resultBundleDto;
        }

        /// <summary>
        /// Extend (update) bundle with removed files.
        /// </summary>
        /// <param name="bundleId">Source bundle id to extend.</param>
        /// <param name="filesToRemovePaths">Files to remove.</param>
        /// <param name="maxChunkSize">Maximum bundle size. By default it's 4 Mb.</param>
        /// <returns>Result Bundle object from server.</returns>
        public async Task<BundleResponseDto> ExtendBundleWithRemovedFilesAsync(string bundleId, List<string> filesToRemovePaths, int maxChunkSize = SnykCodeClient.MaxBundleSize)
        {
            var removeFileChunks = this.SplitRemovedFilesToChunkListsBySize(filesToRemovePaths, maxChunkSize);

            var firstRemovedFiles = removeFileChunks[0];
            var emptyDictionary = new Dictionary<string, string>();

            var resultBundleDto = await this.codeClient.ExtendBundleAsync(bundleId, emptyDictionary, firstRemovedFiles);

            removeFileChunks.Remove(firstRemovedFiles);

            // Call Extend Bundle REST API for bundles.
            foreach (var removeFilesList in removeFileChunks)
            {
                resultBundleDto = await this.codeClient.ExtendBundleAsync(resultBundleDto.Id, emptyDictionary, removeFilesList);
            }

            // Last created bundle is result bundle.
            return resultBundleDto;
        }

        /// <summary> ProcessCreateLargeBundle
        /// Split big bundle to list of small bundles and create new bundle on server using this "chunk" bundles.
        /// </summary>
        /// <param name="pathToHashFileDict">Source files dictionary.</param>
        /// <param name="maxChunkSize">Maximum bundle size. By default it's 4 Mb.</param>
        /// <returns>Result Bundle object from server.</returns>
        public async Task<BundleResponseDto> ProcessCreateLargeBundleAsync(Dictionary<string, string> pathToHashFileDict, int maxChunkSize = SnykCodeClient.MaxBundleSize)
        {
            var fileDictionaries = this.SplitFilesToChunkListsBySize(pathToHashFileDict, maxChunkSize);

            var firstFiles = fileDictionaries[0];

            // Call Create Bundle REST API for first bundle in list to create it on server.
            BundleResponseDto resultBundleDto = await this.codeClient.CreateBundleAsync(firstFiles);

            fileDictionaries.Remove(firstFiles);

            // Call Extend Bundle REST API for bundles.
            foreach (var extendFiles in fileDictionaries)
            {
                resultBundleDto = await this.codeClient.ExtendBundleAsync(resultBundleDto.Id, extendFiles, Enumerable.Empty<string>().ToList());
            }

            // Last created bundle is result bundle.
            return resultBundleDto;
        }

        /// <summary>
        /// Calculate key value pair size in bytes. It multiply it to 2 because for UTF one char is 2 bytes.
        /// </summary>
        /// <param name="filePair">Source file pair (file path + file hash).</param>
        /// <returns>Size in bytys.</returns>
        private int CalculateFilePairSize(KeyValuePair<string, string> filePair) => this.CalculatePayloadSize(Json.Serialize(filePair));

        /// <summary>
        /// Claculate file pairs size.
        /// </summary>
        /// <param name="files">Source dictionary with file info.</param>
        /// <returns>Size of dictionary.</returns>
        private int CalculateFilesSize(Dictionary<string, string> files) => this.CalculatePayloadSize(Json.Serialize(files));

        /// <summary>
        /// Claculate file pairs size.
        /// </summary>
        /// <param name="files">Source dictionary with file info.</param>
        /// <returns>Size of dictionary.</returns>
        private int CalculateFilesSize(List<string> files) => this.CalculatePayloadSize(Json.Serialize(files));

        /// <summary>
        /// Calculate bundle size in bytes.
        /// </summary>
        /// <param name="sourceStr">Source string.</param>
        /// <returns>Size in bytys.</returns>
        private int CalculatePayloadSize(string sourceStr) => UTF8Encoding.UTF8.GetByteCount(sourceStr);

        /// <summary>
        /// Map <see cref="BundleDto"/> object to domain object <see cref="Bundle"/>.
        /// </summary>
        /// <param name="bundleDto">Source bundle DTO object.</param>
        /// <returns>Result domain Bundle object.</returns>
        private Bundle MapDtoBundleToDomain(BundleResponseDto bundleDto)
        {
            return new Bundle
            {
                Id = bundleDto.Id,
                UploadURL = bundleDto.UploadURL,
                MissingFiles = bundleDto.MissingFiles,
            };
        }
    }
}
