namespace Snyk.Code.Library.Api
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Snyk.Code.Library.Api.Dto;
    using Snyk.Code.Library.Common;

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
        /// Uploads missing files to a bundle.
        /// Small files should be uploaded in batches to avoid excessive overhead due to too many requests. 
        /// The file contents must be utf-8 parsed strings and the file hashes must be computed over these strings, matching the "Create Bundle" request.
        /// </summary>
        /// <param name="bundleId">Bundle id to file upload.</param>
        /// <param name="codeFiles">Code files list with file path and file content.</param>
        /// <param name="maxBundleChunkSize">Maximum allowed upload files size.</param>
        /// <returns>True if upload success.</returns>
        public async System.Threading.Tasks.Task<bool> UploadFiles(string bundleId, List<CodeFile> codeFiles, int maxBundleChunkSize = SnykCodeClient.MaxBundleSize)
        {
            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            if (codeFiles == null || codeFiles.Count == 0)
            {
                throw new ArgumentException("Code files list is null or empty.");
            }

            int payloadSize = this.CalculateFilesSize(codeFiles);

            if (payloadSize < maxBundleChunkSize)
            {
                return await this.codeClient.UploadFiles(bundleId, codeFiles);
            }
            else
            {
                return await this.ProcessUploadLargeFiles(bundleId, codeFiles, maxBundleChunkSize);
            }
        }

        /// <summary>
        /// Split code files list to small lists and upload them to server.
        /// </summary>
        /// <param name="bundleId">Source bundle id.</param>
        /// <param name="codeFiles">Code files list.</param>
        /// <param name="maxBundleChunkSize">Maximum allowed upload files size.</param>
        /// <returns>True if upload success.</returns>
        public async System.Threading.Tasks.Task<bool> ProcessUploadLargeFiles(string bundleId, List<CodeFile> codeFiles, int maxBundleChunkSize = SnykCodeClient.MaxBundleSize)
        {
            List<List<CodeFile>> codeFileLists = this.SplitCodeFilesToLists(codeFiles, maxBundleChunkSize);

            foreach (List<CodeFile> codeFileList in codeFileLists)
            {
                _ = await this.codeClient.UploadFiles(bundleId, codeFileList);
            }

            return true;
        }

        /// <summary>
        /// Split code files list to small code files lists (chunks) for upload.
        /// </summary>
        /// <param name="codeFiles">Source code files list.</param>
        /// <param name="maxBundleChunkSize">Maximum allowed upload files size.</param>
        /// <returns>List of 'chunks' lists of code files. </returns>
        public List<List<CodeFile>> SplitCodeFilesToLists(List<CodeFile> codeFiles, int maxBundleChunkSize = SnykCodeClient.MaxBundleSize)
        {
            List<List<CodeFile>> codeFileLists = new List<List<CodeFile>>();

            int listSize = 0;
            List<CodeFile> tempList = new List<CodeFile>();

            foreach (CodeFile file in codeFiles)
            {
                int fileSize = this.CalculateFileSize(file);

                if (listSize + fileSize > maxBundleChunkSize)
                {
                    // Save previous bundle and create new.
                    codeFileLists.Add(tempList);

                    tempList = new List<CodeFile>();

                    listSize = 0;
                }

                tempList.Add(file);

                listSize += fileSize;
            }

            // Add last created bundle in for loop to list of bundles.
            codeFileLists.Add(tempList);

            return codeFileLists;
        }

        /// <summary>
        /// Create new <see cref="Bundle"/> and get result <see cref="Bundle"/> object.
        /// If payload < 4 Mb it just send this bundle and return results.
        /// If payload > 4 Mb it will:
        ///      Split initial bundle on list of bundles (chunks).
        ///      Call Create bundle REST API for first bundle in list.
        ///      For all other bundles it will Extend bundle.
        ///      Return last bundle as result.
        /// </summary>
        /// <param name="newBundle">Bundle object with files data.</param>
        /// <param name="maxBundleChunkSize">Maximum allowed bundle size.</param>
        /// <returns>Bundle object with bundle id, missing files and upload url.</returns>
        public async System.Threading.Tasks.Task<Bundle> CreateBundle(Bundle newBundle, int maxBundleChunkSize = SnykCodeClient.MaxBundleSize)
        {
            if (newBundle == null || newBundle.Files.Count == 0)
            {
                throw new ArgumentException("Bundle is null or empty.");
            }

            int payloadSize = this.CalculateBundleSize(newBundle);

            // If payload < 4 Mb just send this bundle and return results.
            if (payloadSize < maxBundleChunkSize)
            {
                return await this.codeClient.CreateBundle(newBundle);
            }
            else
            {
                return await this.ProcessCreateLargeBundle(newBundle);
            }
        }

        /// <summary>
        /// Creates a new bundle based on a previously uploaded one.
        /// This method wrap functionality to extend bundle if it's small by size or make few chunks and extend by chunks.
        /// </summary>
        /// <param name="previousBundle">Already created bundle with valid bundle id.</param>
        /// <param name="extendBundle">Bundle to extend with new or removed files.</param>
        /// <param name="maxBundleChunkSize">Maximum bundle chunk size. By default it is 4 Mb.</param>
        /// <returns>Result extended bundle.</returns>
        public async System.Threading.Tasks.Task<Bundle> ExtendBundle(Bundle previousBundle, Bundle extendBundle, int maxBundleChunkSize = SnykCodeClient.MaxBundleSize)
        {
            if (previousBundle == null || string.IsNullOrEmpty(previousBundle.Id))
            {
                throw new ArgumentException("Previous Bundle is null or empty.");
            }

            if (extendBundle == null)
            {
                throw new ArgumentException("Extend Bundle is null or empty.");
            }

            int payloadSize = this.CalculateBundleSize(extendBundle);

            // If payload < 4 max bundle chunk size just send this bundle and return results.
            if (payloadSize < maxBundleChunkSize)
            {
                return await this.codeClient.ExtendBundle(previousBundle, extendBundle);
            }
            else
            {
                return await this.ProcessExtendLargeBundle(previousBundle, extendBundle, maxBundleChunkSize);
            }
        }

        /// <summary>
        /// Split bundle to list of bundles by maximun bundle size.
        /// </summary>
        /// <param name="newBundle">Source bundle.</param>
        /// <param name="maxBundleChunkSize">Maximum bundle size. By default it's 4 Mb.</param>
        /// <returns>List<Bundle>.</returns>
        public List<Bundle> SplitBundleToChunksBySize(Bundle newBundle, int maxBundleChunkSize = SnykCodeClient.MaxBundleSize)
        {
            List<Bundle> bundles = new List<Bundle>();

            int bundleSize = 0;
            Bundle bundle = new Bundle();

            foreach (string removeFile in newBundle.RemovedFiles)
            {
                int fileSize = this.CalculatePayloadSize(removeFile);

                if (bundleSize + fileSize > maxBundleChunkSize)
                {
                    // Save previous bundle and create new.
                    bundles.Add(bundle);

                    bundle = new Bundle();

                    bundleSize = 0;
                }

                bundle.RemovedFiles.Add(removeFile);

                bundleSize += fileSize;
            }

            foreach (KeyValuePair<string, string> filePair in newBundle.Files)
            {
                int fileSize = this.CalculateFilePairSize(filePair);

                if (bundleSize + fileSize > maxBundleChunkSize)
                {
                    // Save previous bundle and create new.
                    bundles.Add(bundle);

                    bundle = new Bundle();

                    bundleSize = 0;
                }

                bundle.Files.Add(filePair.Key, filePair.Value);

                bundleSize += fileSize;
            }

            // Add last created bundle in for loop to list of bundles.
            bundles.Add(bundle);

            return bundles;
        }

        /// <summary>
        /// Split big bundle to list of small bundles and extend bundle using this "chunk" bundles.
        /// </summary>
        /// <param name="previousBundle">Already created bundle to extend.</param>
        /// <param name="extendBundle">Bundle with new data for extend.</param>
        /// <param name="maxBundleChunkSize">Maximum bundle size. By default it's 4 Mb.</param>
        /// <returns>Result Bundle object from server.</returns>
        public async System.Threading.Tasks.Task<Bundle> ProcessExtendLargeBundle(Bundle previousBundle, Bundle extendBundle, int maxBundleChunkSize = SnykCodeClient.MaxBundleSize)
        {
            List<Bundle> bundles = this.SplitBundleToChunksBySize(extendBundle, maxBundleChunkSize);

            Bundle firstBundle = bundles[0];

            // Call Create Bundle REST API for first bundle in list to create it on server.
            Bundle resultBundle = await this.codeClient.ExtendBundle(previousBundle, firstBundle);

            bundles.Remove(firstBundle);

            // Call Extend Bundle REST API for bundles.
            foreach (Bundle bundleItem in bundles)
            {
                resultBundle = await this.codeClient.ExtendBundle(resultBundle, bundleItem);
            }

            // Last created bundle is result bundle.
            return resultBundle;
        }

        /// <summary> ProcessCreateLargeBundle
        /// Split big bundle to list of small bundles and create new bundle on server using this "chunk" bundles.
        /// </summary>
        /// <param name="newBundle">Source bundle.</param>
        /// <param name="maxBundleChunkSize">Maximum bundle size. By default it's 4 Mb.</param>
        /// <returns>Result Bundle object from server.</returns>
        public async System.Threading.Tasks.Task<Bundle> ProcessCreateLargeBundle(Bundle newBundle, int maxBundleChunkSize = SnykCodeClient.MaxBundleSize)
        {
            List<Bundle> bundles = this.SplitBundleToChunksBySize(newBundle, maxBundleChunkSize);

            Bundle firstBundle = bundles[0];

            // Call Create Bundle REST API for first bundle in list to create it on server.
            Bundle resultBundle = await this.codeClient.CreateBundle(firstBundle);

            bundles.Remove(firstBundle);

            // Call Extend Bundle REST API for bundles.
            foreach (Bundle bundleItem in bundles)
            {
                resultBundle = await this.codeClient.ExtendBundle(resultBundle, bundleItem);
            }

            // Last created bundle is result bundle.
            return resultBundle;
        }

        /// <summary>
        /// Calculate key value pair size in bytes. It multiply it to 2 because for UTF one char is 2 bytes.
        /// </summary>
        /// <param name="filePair">Source file pair (file path + file hash).</param>
        /// <returns>Size in bytys.</returns>
        private int CalculateFilePairSize(KeyValuePair<string, string> filePair) => this.CalculatePayloadSize(Json.Serialize<KeyValuePair<string, string>>(filePair));

        /// <summary>
        /// Calculate bundle size in bytes. It multiply it to 2 because for UTF one char is 2 bytes.
        /// </summary>
        /// <param name="bundle">Source bundle.</param>
        /// <returns>Size in bytys.</returns>
        private int CalculateBundleSize(Bundle bundle) => this.CalculatePayloadSize(Json.Serialize<Bundle>(bundle));

        /// <summary>
        /// Calculate size of code files list.
        /// </summary>
        /// <param name="codeFiles">Source list of code files.</param>
        /// <returns>Size in bytes.</returns>
        private int CalculateFilesSize(List<CodeFile> codeFiles) => this.CalculatePayloadSize(Json.Serialize<List<CodeFile>>(codeFiles));

        /// <summary>
        /// Calculate size of code file.
        /// </summary>
        /// <param name="codeFile">Source code file.</param>
        /// <returns>Size in bytes.</returns>
        private int CalculateFileSize(CodeFile codeFile) => this.CalculatePayloadSize(Json.Serialize<CodeFile>(codeFile));

        /// <summary>
        /// Calculate bundle size in bytes.
        /// </summary>
        /// <param name="sourceStr">Source string.</param>
        /// <returns>Size in bytys.</returns>
        private int CalculatePayloadSize(string sourceStr) => UTF8Encoding.UTF8.GetByteCount(sourceStr);
    }
}
