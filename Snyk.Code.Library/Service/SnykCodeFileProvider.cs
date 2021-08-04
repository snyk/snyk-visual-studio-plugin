namespace Snyk.Code.Library.Service
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Caching;
    using System.Text;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Common;

    /// <summary>
    /// Implementation of <see cref="IFileProvider"/> for Visual Studio SnykCode.
    /// </summary>
    public class SnykCodeFileProvider : IFileProvider
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykCodeFileProvider>();

        private ObjectCache filePathToHashCache = new MemoryCache("FilePathToFileHashCache");

        private ObjectCache filePathToContentCache = new MemoryCache("FilePathToFileContentCache");

        private string solutionPath;

        private IList<string> files;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeFileProvider"/> class.
        /// </summary>
        /// <param name="solutionPath">Solution path service.</param>
        /// <param name="files"><see cref="IList{T}"/> files.</param>
        public SnykCodeFileProvider(string solutionPath, IList<string> files)
        {
            this.solutionPath = solutionPath;

            this.files = files;
        }

        public IDictionary<string, string> CreaateFileHashToContentDictionary(IList<string> files)
        {
            if (this.filePathToContentCache.GetCount() == 0)
            {
                this.InitializeCache();
            }

            var fileHashToContentDict = new Dictionary<string, string>();

            foreach (var keyValuePair in this.filePathToContentCache)
            {
                fileHashToContentDict.Add(keyValuePair.Key, keyValuePair.Value.ToString());
            }

            return fileHashToContentDict;
        }

        public IDictionary<string, string> CreateFilePathToHashDictionary()
        {
            if (this.filePathToHashCache.GetCount() == 0)
            {
                this.InitializeCache();
            }

            var filePathToHashDict = new Dictionary<string, string>();

            foreach (var keyValuePair in this.filePathToHashCache)
            {
                filePathToHashDict.Add(keyValuePair.Key, keyValuePair.Value.ToString());
            }

            return filePathToHashDict;
        }

        /// <inheritdoc/>
        public async Task FilterFilesAsync(IFiltersService filtersService) => this.files = await filtersService.FilterFilesAsync(this.files);

        private void InitializeCache()
        {
            foreach (string filePath in this.files)
            {
                this.AddToCache(filePath);
            }
        }

        /// <summary>
        /// Add file hash and content to cache.
        /// </summary>
        /// <param name="filePath">Source file path.</param>
        private void AddToCache(string filePath)
        {
            try
            {
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string fileHash = Sha256.ComputeHash(fileContent);

                string relativeFilePath = this.CreateRelativeFilePath(filePath);

                this.AddToFilePathToHashCache(relativeFilePath, fileHash);

                this.AddToFilePathToContentCache(relativeFilePath, fileContent);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        private string CreateRelativeFilePath(string filePath)
        {
            string path = filePath.Replace(this.solutionPath, string.Empty);

            return path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private void AddToFilePathToHashCache(string filePath, string fileHash) => this.filePathToHashCache.Add(filePath, fileHash, new CacheItemPolicy());

        private void AddToFilePathToContentCache(string filePath, string fileContent) => this.filePathToContentCache.Add(filePath, fileContent, new CacheItemPolicy());
    }
}
