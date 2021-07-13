namespace Snyk.Code.Library.Service.Impl
{
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Caching;
    using System.Text;
    using Snyk.Code.Library.Common;

    /// <inheritdoc/>
    public class FileCacheService : IFileCacheService
    {
        private ObjectCache filePathToHashCache = MemoryCache.Default;

        private CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();

        private Dictionary<string, string> filePathToContentDict = new Dictionary<string, string>();

        private IList<string> filePaths;

        /// <inheritdoc/>
        public string GetFileContent(string filePathKey)
        {
            string fileContent = this.filePathToContentDict[filePathKey];

            if (fileContent == null)
            {
                this.Add(filePathKey);

                fileContent = this.filePathToContentDict[filePathKey];
            }

            return fileContent;
        }

        /// <inheritdoc/>
        public string GetFileHash(string filePathKey)
        {
            string fileHash = this.filePathToHashCache[filePathKey] as string;

            if (fileHash == null)
            {
                this.Add(filePathKey);

                fileHash = this.filePathToHashCache[filePathKey] as string;
            }

            return fileHash;
        }

        /// <inheritdoc/>
        public void Setup(IList<string> filePaths)
        {
            this.filePaths = filePaths;

            this.cacheItemPolicy.ChangeMonitors.Add(new HostFileChangeMonitor(filePaths));

            foreach (string filePath in filePaths)
            {
                this.Add(filePath);
            }
        }

        /// <summary>
        /// Add file hash and content to cache.
        /// </summary>
        /// <param name="filePath">Source file path.</param>
        private void Add(string filePath)
        {
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string fileHash = Sha256.ComputeHash(fileContent);

            this.filePathToHashCache.Set(filePath, fileHash, this.cacheItemPolicy);

            this.filePathToContentDict.Add(filePath, fileContent);
        }
    }
}
