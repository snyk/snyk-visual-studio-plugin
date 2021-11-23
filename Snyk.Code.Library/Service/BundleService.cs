﻿namespace Snyk.Code.Library.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto;
    using Snyk.Code.Library.Domain;
    using Snyk.Common;

    /// <inheritdoc/>
    public class BundleService : IBundleService
    {
        private const int UploadFileRequestAttempts = 5;

        private static readonly ILogger Logger = LogManager.ForContext<BundleService>();

        private readonly ISnykCodeClient codeClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="BundleService"/> class.
        /// </summary>
        /// <param name="client"><see cref="ISnykCodeClient"/> implementation.</param>
        public BundleService(ISnykCodeClient client) => this.codeClient = client;

        /// <summary>
        /// Gets a value indicating SnykCode client.
        /// </summary>
        public ISnykCodeClient CodeClient => this.codeClient;

        /// <inheritdoc/>
        public async Task<Bundle> CheckBundleAsync(string bundleId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            var bundleResponseDto = this.codeClient.CheckBundleAsync(bundleId, cancellationToken);

            return this.MapDtoBundleToDomain(await bundleResponseDto);
        }

        /// <inheritdoc/>
        public async Task<bool> UploadMissingFilesAsync(Bundle bundle, ICodeCacheService codeCacheService, CancellationToken cancellationToken = default)
        {
            Logger.Information("Uploading missing files for bundle.");

            var resultBundle = bundle;

            for (int counter = 0; counter < UploadFileRequestAttempts; counter++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileHashToContentDict = codeCacheService.GetFileHashToContentDictionary(resultBundle.MissingFiles);

                await this.UploadFilesAsync(resultBundle.Id, fileHashToContentDict, cancellationToken: cancellationToken);

                resultBundle = await this.CheckBundleAsync(bundle.Id, cancellationToken);

                if (resultBundle.MissingFiles.IsNullOrEmpty())
                {
                    return true;
                }
            }

            Logger.Information("Not all files uploadded successfully. Not uploaded files {MissingFiles}", resultBundle.MissingFiles);

            return false;
        }

        /// <inheritdoc/>
        public Task<bool> UploadFilesAsync(
            string bundleId,
            IDictionary<string, string> fileHashToContentDict,
            int maxChunkSize = SnykCodeClient.MaxBundleSize,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            if (fileHashToContentDict == null)
            {
                throw new ArgumentException("Code files list is null.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            int payloadSize = this.CalculateFilesSize(fileHashToContentDict);

            if (payloadSize < maxChunkSize)
            {
                var codeFiles = this.BuildCodeFileDtoList(fileHashToContentDict);

                return this.codeClient.UploadFilesAsync(bundleId, codeFiles, cancellationToken);
            }
            else
            {
                return this.ProcessUploadLargeFilesAsync(bundleId, fileHashToContentDict, maxChunkSize, cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Split code files list to small lists and upload them to server.
        /// </summary>
        /// <param name="bundleId">Source bundle id.</param>
        /// <param name="fileHashToContentDict">Dictionary with file hash to file content mapping.</param>
        /// <param name="maxChunkSize">Maximum allowed upload files size.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns>True if upload success.</returns>
        public async Task<bool> ProcessUploadLargeFilesAsync(
            string bundleId,
            IDictionary<string, string> fileHashToContentDict,
            int maxChunkSize = SnykCodeClient.MaxBundleSize,
            CancellationToken cancellationToken = default)
        {
            var codeFileLists = this.SplitFilesToChunkListsBySize(fileHashToContentDict, maxChunkSize);

            bool isAllFilesUploaded = true;

            foreach (var codeFileList in codeFileLists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var codeFiles = this.BuildCodeFileDtoList(codeFileList);

                isAllFilesUploaded &= await this.codeClient.UploadFilesAsync(bundleId, codeFiles, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return isAllFilesUploaded;
        }

        /// <inheritdoc/>
        public async Task<Bundle> CreateBundleAsync(
            IDictionary<string, string> pathToHashFileDict,
            int maxChunkSize = SnykCodeClient.MaxBundleSize,
            CancellationToken cancellationToken = default)
        {
            if (pathToHashFileDict == null || pathToHashFileDict.Count == 0)
            {
                throw new ArgumentException("Files list is null or empty.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            int payloadSize = this.CalculateFilesSize(pathToHashFileDict);

            Task<BundleResponseDto> bundleDto;

            if (payloadSize < maxChunkSize)
            {
                bundleDto = this.codeClient.CreateBundleAsync(pathToHashFileDict, cancellationToken);
            }
            else
            {
                bundleDto = this.ProcessCreateLargeBundleAsync(pathToHashFileDict, maxChunkSize, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            return this.MapDtoBundleToDomain(await bundleDto);
        }

        /// <inheritdoc/>
        public async Task<Bundle> ExtendBundleAsync(
            string bundleId,
            IDictionary<string, string> pathToHashFileDict,
            IEnumerable<string> filesToRemovePaths,
            int maxChunkSize = SnykCodeClient.MaxBundleSize,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            if (pathToHashFileDict.IsNullOrEmpty() && filesToRemovePaths.IsNullOrEmpty())
            {
                throw new ArgumentException("On Extend Bundle files or removed files are empty.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            int payloadSize = this.CalculateFilesSize(pathToHashFileDict) + this.CalculateFilesSize(filesToRemovePaths);

            Task<BundleResponseDto> bundleDto = null;

            if (payloadSize < maxChunkSize)
            {
                bundleDto = this.codeClient.ExtendBundleAsync(bundleId, pathToHashFileDict, filesToRemovePaths, cancellationToken);
            }
            else
            {
                bundleDto = this.ProcessExtendLargeBundleAsync(bundleId, pathToHashFileDict, filesToRemovePaths, maxChunkSize, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            return this.MapDtoBundleToDomain(await bundleDto);
        }

        /// <summary>
        /// Split bundle to list of bundles by maximun bundle size.
        /// </summary>
        /// <param name="pathToHashFileDict">Source files dictionary.</param>
        /// <param name="maxChunkSize">Maximum chunk size. By default it's 4 Mb.</param>
        /// <returns>List of smaller file dictionaries.</returns>
        public List<Dictionary<string, string>> SplitFilesToChunkListsBySize(IDictionary<string, string> pathToHashFileDict, int maxChunkSize = SnykCodeClient.MaxBundleSize, CancellationToken cancellationToken = default)
        {
            var fileDictionaries = new List<Dictionary<string, string>>();

            if (pathToHashFileDict == null || pathToHashFileDict.Count == 0)
            {
                return fileDictionaries;
            }

            cancellationToken.ThrowIfCancellationRequested();

            int bundleSize = 0;

            var fileDictionary = new Dictionary<string, string>();

            foreach (var filePair in pathToHashFileDict)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int fileSize = this.CalculateFilePairSize(filePair);

                if (fileSize > maxChunkSize)
                {
                    continue; // If file too big it skip it and continue to upload other files.
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

            cancellationToken.ThrowIfCancellationRequested();

            return fileDictionaries;
        }

        /// <summary>
        /// Split removed files list to smaller lists by maximum chunk size.
        /// </summary>
        /// <param name="removedFiles">Source list of removed files.</param>
        /// <param name="maxChunkSize">Maximum chunk size for upload.</param>
        /// <returns>List of small removed file lists.</returns>
        public IEnumerable<List<string>> SplitRemovedFilesToChunkListsBySize(IEnumerable<string> removedFiles, int maxChunkSize = SnykCodeClient.MaxBundleSize)
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
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns>Result Bundle object from server.</returns>
        public async Task<BundleResponseDto> ProcessExtendLargeBundleAsync(
            string bundleId,
            IDictionary<string, string> pathToHashFileDict,
            IEnumerable<string> removedFiles,
            int maxChunkSize = SnykCodeClient.MaxBundleSize,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bundleDto = new BundleResponseDto
            {
                Id = bundleId,
            };

            if (!removedFiles.IsNullOrEmpty())
            {
                bundleDto = await this.ExtendBundleWithRemovedFilesAsync(bundleDto.Id, removedFiles, maxChunkSize, cancellationToken);
            }

            if (!pathToHashFileDict.IsNullOrEmpty())
            {
                bundleDto = await this.ExtendBundleWithFilesAsync(bundleDto.Id, pathToHashFileDict, maxChunkSize, cancellationToken);
            }

            return bundleDto;
        }

        /// <summary>
        /// Extend (update) bundle with removed files.
        /// </summary>
        /// <param name="bundleId">Source bundle id to extend.</param>
        /// <param name="pathToHashFileDict">Files to update extend bundle.</param>
        /// <param name="maxChunkSize">Maximum bundle size. By default it's 4 Mb.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns>Result Bundle object from server.</returns>
        public async Task<BundleResponseDto> ExtendBundleWithFilesAsync(
            string bundleId, 
            IDictionary<string, string> pathToHashFileDict,
            int maxChunkSize = SnykCodeClient.MaxBundleSize,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileDictionaries = this.SplitFilesToChunkListsBySize(pathToHashFileDict, maxChunkSize);

            var firstFiles = fileDictionaries[0];

            var resultBundleDto = await this.codeClient.ExtendBundleAsync(bundleId, firstFiles, Enumerable.Empty<string>().ToList(), cancellationToken);

            fileDictionaries.Remove(firstFiles);

            // Call Extend Bundle REST API for bundles.
            foreach (var filesDictionary in fileDictionaries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                resultBundleDto = await this.codeClient.ExtendBundleAsync(resultBundleDto.Id, filesDictionary, Enumerable.Empty<string>().ToList(), cancellationToken);
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
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns>Result Bundle object from server.</returns>
        public async Task<BundleResponseDto> ExtendBundleWithRemovedFilesAsync(
            string bundleId,
            IEnumerable<string> filesToRemovePaths,
            int maxChunkSize = SnykCodeClient.MaxBundleSize,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var removeFileChunks = this.SplitRemovedFilesToChunkListsBySize(filesToRemovePaths, maxChunkSize).ToList();

            var firstRemovedFiles = removeFileChunks[0];
            var emptyDictionary = new Dictionary<string, string>();

            var resultBundleDto = await this.codeClient.ExtendBundleAsync(bundleId, emptyDictionary, firstRemovedFiles, cancellationToken);

            removeFileChunks.Remove(firstRemovedFiles);

            // Call Extend Bundle REST API for bundles.
            foreach (var removeFilesList in removeFileChunks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                resultBundleDto = await this.codeClient.ExtendBundleAsync(resultBundleDto.Id, emptyDictionary, removeFilesList, cancellationToken);
            }

            // Last created bundle is result bundle.
            return resultBundleDto;
        }

        /// <summary> ProcessCreateLargeBundle
        /// Split big bundle to list of small bundles and create new bundle on server using this "chunk" bundles.
        /// </summary>
        /// <param name="pathToHashFileDict">Source files dictionary.</param>
        /// <param name="maxChunkSize">Maximum bundle size. By default it's 4 Mb.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns>Result Bundle object from server.</returns>
        public async Task<BundleResponseDto> ProcessCreateLargeBundleAsync(
            IDictionary<string, string> pathToHashFileDict,
            int maxChunkSize = SnykCodeClient.MaxBundleSize,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileDictionaries = this.SplitFilesToChunkListsBySize(pathToHashFileDict, maxChunkSize);

            var firstFiles = fileDictionaries[0];

            // Call Create Bundle REST API for first bundle in list to create it on server.
            BundleResponseDto resultBundleDto = await this.codeClient.CreateBundleAsync(firstFiles, cancellationToken);

            fileDictionaries.Remove(firstFiles);

            // Call Extend Bundle REST API for bundles.
            foreach (var extendFiles in fileDictionaries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                resultBundleDto = await this.codeClient.ExtendBundleAsync(resultBundleDto.Id, extendFiles, Enumerable.Empty<string>().ToList(), cancellationToken);
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
        private int CalculateFilesSize(IDictionary<string, string> files) => this.CalculatePayloadSize(Json.Serialize(files));

        /// <summary>
        /// Claculate file pairs size.
        /// </summary>
        /// <param name="files">Source dictionary with file info.</param>
        /// <returns>Size of dictionary.</returns>
        private int CalculateFilesSize(IEnumerable<string> files) => this.CalculatePayloadSize(Json.Serialize(files));

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

        private List<CodeFileDto> BuildCodeFileDtoList(IDictionary<string, string> fileHashToContentDict)
        {
            var codeFileDtos = new List<CodeFileDto>();

            foreach (var filePair in fileHashToContentDict)
            {
                codeFileDtos.Add(new CodeFileDto(filePair.Key, filePair.Value));
            }

            return codeFileDtos;
        }
    }
}
