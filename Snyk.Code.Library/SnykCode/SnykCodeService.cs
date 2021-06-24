namespace Snyk.Code.Library.SnykCode
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Snyk.Code.Library.Common;

    /// <summary>
    /// SnykCodeService contains logic on top of <see cref="SnykCodeClient"/> class for SnykCode functionality.
    /// </summary>
    public class SnykCodeService
    {       
        private readonly SnykCodeClient codeClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeService"/> class.
        /// </summary>
        /// <param name="baseUrl">Base URL for deproxy.</param>
        /// <param name="token">User token.</param>
        public SnykCodeService(string baseUrl, string token) => this.codeClient = new SnykCodeClient(baseUrl, token);

        /// <summary>
        /// Create new <see cref="Bundle"/> and get result <see cref="Bundle"/> object.
        // If payload < 4 Mb it just send this bundle and return results.
        // If payload > 4 Mb it will:
        //      Split initial bundle on list of bundles (chunks).
        //      Call Create bundle REST API for first bundle in list.
        //      For all other bundles it will Extend bundle.
        //      Return last bundle as result.
        /// </summary>
        /// <param name="newBundle">Bundle object with files data.</param>
        /// <returns>Bundle object with bundle id, missing files and upload url.</returns>
        public async System.Threading.Tasks.Task<Bundle> CreateBundle(Bundle newBundle, int maxBundleChunkSize = SnykCodeClient.MaxBundleSize)
        {
            if (newBundle == null || newBundle.Files.Count == 0)
            {
                throw new ArgumentException("Bundle is null or empty.");
            }

            int payloadSize = CalculateBundleSize(newBundle);

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
        /// <returns></returns>
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

            int payloadSize = CalculateBundleSize(extendBundle);

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

            // Add last created bundle in for loop to list of bundles.
            //bundles.Add(bundle);

            foreach (KeyValuePair<string, string> filePair in newBundle.Files)
            {
                int fileSize = CalculateFilePairSize(filePair);

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
        /// <param name="newBundle">Source bundle.</param>
        /// <param name="maxBundleChunkSize">Maximum bundle size. By default it's 4 Mb.</param>
        /// <returns>Result Bundle object from server.</returns>
        public async System.Threading.Tasks.Task<Bundle> ProcessExtendLargeBundle(Bundle previousBundle, Bundle extendBundle, int maxBundleChunkSize = SnykCodeClient.MaxBundleSize)
        {
            List<Bundle> bundles = SplitBundleToChunksBySize(extendBundle, maxBundleChunkSize);

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
            List<Bundle> bundles = SplitBundleToChunksBySize(newBundle, maxBundleChunkSize);

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
        /// Calculate bundle size in bytes.        
        /// </summary>
        /// <param name="sourceStr">Source string.</param>
        /// <returns>Size in bytys.</returns>
        private int CalculatePayloadSize(string sourceStr) 
        {
            UTF8Encoding utf8 = new UTF8Encoding();

            return utf8.GetByteCount(sourceStr);
        }
    }
}
