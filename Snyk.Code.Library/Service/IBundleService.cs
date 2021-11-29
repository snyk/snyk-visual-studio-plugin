﻿namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Domain;

    /// <summary>
    /// BundleService contains logic on top of <see cref="SnykCodeClient"/> class for SnykCode functionality.
    /// </summary>
    public interface IBundleService
    {
        /// <summary>
        /// Checks the status of a bundle.
        /// </summary>
        /// <param name="bundleId">Bundle id to check.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns>Returns the bundleId and, in case of uploaded bundles, the current missingFiles and the uploadURL.
        /// This API can be used to check if an old uploaded bundle has expired (status code 404),
        /// or to check if there are still missing files after uploading ("Upload Files").
        /// </returns>
        Task<Bundle> CheckBundleAsync(string bundleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upload bundle missing files. If files not uploaded by one call it will try 5 times for upload.
        /// </summary>
        /// <param name="bundle">Source bundle with missing files to upload.</param>
        /// <param name="codeCacheService">File cache service.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns>True if upload all files successfully and false if not.</returns>
        Task<bool> UploadMissingFilesAsync(Bundle bundle, ICodeCacheService codeCacheService, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads missing files to a bundle.
        /// Small files should be uploaded in batches to avoid excessive overhead due to too many requests. 
        /// The file contents must be utf-8 parsed strings and the file hashes must be computed over these strings, matching the "Create Bundle" request.
        /// </summary>
        /// <param name="bundleId">Bundle id to file upload.</param>
        /// <param name="fileHashToContentDict">Dictionary with file hash to file content mapping.</param>
        /// <param name="maxChunkSize">Maximum allowed upload files size.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns>True if upload success.</returns>
        Task<bool> UploadFilesAsync(string bundleId, IDictionary<string, (string hash, string content)> fileHashToContentDict, int maxChunkSize = SnykCodeClient.MaxBundleSize, CancellationToken cancellationToken = default);

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
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns>Bundle object with bundle id, missing files and upload url.</returns>
        Task<Bundle> CreateBundleAsync(IDictionary<string, string> pathToHashFileDict, int maxChunkSize = SnykCodeClient.MaxBundleSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new bundle based on a previously uploaded one.
        /// This method wrap functionality to extend bundle if it's small by size or make few chunks and extend by chunks.
        /// </summary>
        /// <param name="bundleId">Already created bundle id.</param>
        /// <param name="pathToHashFileDict">Files to add in bundle.</param>
        /// <param name="filesToRemovePaths">Files to remove in bundle.</param>
        /// <param name="maxChunkSize">Maximum bundle chunk size. By default it is 4 Mb.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns>Result extended bundle.</returns>
        Task<Bundle> ExtendBundleAsync(
            string bundleId,
            IDictionary<string, string> pathToHashFileDict,
            IEnumerable<string> filesToRemovePaths,
            int maxChunkSize = SnykCodeClient.MaxBundleSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Split bundle to list of bundles by maximun bundle size.
        /// </summary>
        /// <param name="pathToHashFileDict">Source files dictionary.</param>
        /// <param name="maxChunkSize">Maximum chunk size. By default it's 4 Mb.</param>
        /// <param name="cancellationToken">Token to cancel current task.</param>
        /// <returns>List of smaller file dictionaries.</returns>
        List<Dictionary<string, string>> SplitFilesToChunkListsBySize(
            IDictionary<string, string> pathToHashFileDict,
            int maxChunkSize = SnykCodeClient.MaxBundleSize,
            CancellationToken cancellationToken = default);
    }
}
