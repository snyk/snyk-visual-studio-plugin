namespace Snyk.Code.Library.Api
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Api.Dto;
    using Snyk.Code.Library.Api.Dto.Analysis;

    /// <summary>
    /// SnykCode client interface.
    /// </summary>
    public interface ISnykCodeClient
    {
        /// <summary>
        /// Starts a new bundle analysis or checks its current status and available results.
        /// Returns the current analysis status, the relative progress (between 0 and 1) within the current status, the analysisURL that you can access on your browser to see the interactive analysis on DeepCode, and the analysisResults if available. 
        /// The status is defined as follows:
        /// WAITING: Your request is waiting in a queue to be processed.
        /// FETCHING: The analysis has just begun and it is currently cloning/fetching the git repository or checking missing files.
        /// ANALYZING: DeepCode is analyzing every file in the bundle to check for bugs and create suggestions.
        /// DC_DONE: DeepCode has finished analyzing the files but external linter tools are still computing.
        /// DONE: All analyses have been computed and are available.
        /// FAILED: Something went wrong with the analysis. For uploaded bundles this occurs when attempting to analyze bundles with missing files.If caused by a transient error, further calls to this API will reset the analysis status and start from the "FETCHING" phase again.
        /// The analysisResults object is only available in the "DONE" status.
        /// It contains all the suggestions and the relative positions.
        /// </summary>
        /// <param name="bundleId">Source bundle id to analysy.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns>Analysis results with suggestions and the relative positions.</returns>
        Task<AnalysisResultDto> GetAnalysisAsync(string bundleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads missing files to a bundle.
        /// Small files should be uploaded in batches to avoid excessive overhead due to too many requests. 
        /// The file contents must be utf-8 parsed strings and the file hashes must be computed over these strings, matching the "Create Bundle" request.
        /// </summary>
        /// <param name="bundleId">Bundle id to file upload.</param>
        /// <param name="codeFiles">List of <see cref="CodeFileDto"/> with file hash and file content.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns>True if upload success.</returns>
        Task<bool> UploadFilesAsync(string bundleId, IEnumerable<CodeFileDto> codeFiles, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new bundle based on a previously uploaded one.
        /// The newly created child bundle will have the same files as the parent bundle (identified by the bundleId in the request) except for what is defined in the payload. 
        /// The removedFiles are parsed before the files, therefore if the same filePath appears in both of them it will not be removed. 
        /// The entries in the files object can either replace an old file with a new version (if the paths match) or add a new file to the child bundle. 
        /// This API is only available for extending uploaded bundles (not git bundles).
        /// As per the "Create Bundle" API, it is possible to pass either an object or an array in the file parameter, with the same semantics as previously described.
        /// Extending a bundle by removing all the parent bundle's files is not allowed.
        /// </summary>
        /// <param name="bundleId">Already created bundle id.</param>
        /// <param name="pathToHashFileDict">Files to add in bundle.</param>
        /// <param name="removedFiles">Files to remove in bundle.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns>Extended bundle object.</returns>
        Task<BundleResponseDto> ExtendBundleAsync(
            string bundleId, 
            Dictionary<string, string> pathToHashFileDict,
            List<string> removedFiles, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks the status of a bundle.
        /// </summary>
        /// <param name="bundleId">Bundle id to check.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns
        /// >Returns the bundleId and, in case of uploaded bundles, the current missingFiles and the uploadURL.
        /// This API can be used to check if an old uploaded bundle has expired (status code 404),
        /// or to check if there are still missing files after uploading ("Upload Files").
        /// </returns>
        Task<BundleResponseDto> CheckBundleAsync(string bundleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create new <see cref="BundleResponseDto"/> and get result <see cref="BundleResponseDto"/> object.
        /// </summary>
        /// <param name="pathToHashFileDict">Bundle files.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns>Bundle object with bundle id, missing files and upload url.</returns>
        Task<BundleResponseDto> CreateBundleAsync(IDictionary<string, string> pathToHashFileDict, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the list of allowed extensions and configuration files for uploaded bundles.
        /// </summary>
        /// <returns><see cref="FiltersDto"/>.</returns>
        Task<FiltersDto> GetFiltersAsync();
    }
}
