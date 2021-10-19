namespace Snyk.Code.Library.Service
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Caching;
    using System.Text;
    using Serilog;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Common;

    /// <inheritdoc/>
    public class CodeCacheService : ICodeCacheService
    {
        private static readonly ILogger Logger = LogManager.ForContext<CodeCacheService>();

        private ObjectCache filePathToHashCache = new MemoryCache("FilePathToFileHashCache");

        private ObjectCache filePathToContentCache = new MemoryCache("FilePathToFileContentCache");

        private AnalysisResult analysisResult;

        private bool cacheValid;

        private string rootDirectoryPath;

        private string bundleId;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeCacheService"/> class.
        /// </summary>
        /// <param name="rootPath">Path to solution folder.</param>
        public CodeCacheService(string rootPath)
        {
            this.rootDirectoryPath = rootPath;
        }

        /// <inheritdoc/>
        public IDictionary<string, string> GetFileHashToContentDictionary()
        {
            var fileHashToContentDict = new Dictionary<string, string>();

            foreach (var keyValuePair in this.filePathToContentCache)
            {
                fileHashToContentDict.Add(keyValuePair.Key, keyValuePair.Value.ToString());
            }

            return fileHashToContentDict;
        }

        /// <inheritdoc/>
        public IDictionary<string, string> GetFileHashToContentDictionary(IEnumerable<string> files)
        {
            var filePathToHashDict = new Dictionary<string, string>();

            foreach (string file in files)
            {
                filePathToHashDict.Add(file, this.filePathToContentCache[file].ToString());
            }

            return filePathToHashDict;
        }

        /// <inheritdoc/>
        public IDictionary<string, string> GetFilePathToHashDictionary()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            foreach (var keyValuePair in this.filePathToHashCache)
            {
                filePathToHashDict.Add(keyValuePair.Key, keyValuePair.Value.ToString());
            }

            return filePathToHashDict;
        }

        /// <inheritdoc/>
        public IDictionary<string, string> GetFilePathToHashDictionary(IEnumerable<string> files)
        {
            var filePathToHashDict = new Dictionary<string, string>();

            foreach (string file in files)
            {
                string filePath = this.GetRelativeFilePathIfFullPath(file);

                if (File.Exists(filePath))
                {
                    filePathToHashDict.Add(filePath, this.filePathToHashCache[filePath].ToString());
                }
            }

            return filePathToHashDict;
        }

        /// <inheritdoc/>
        public bool CacheNotExists() => this.analysisResult == null;

        /// <inheritdoc/>
        public bool CacheValid() => this.cacheValid;

        /// <inheritdoc/>
        public AnalysisResult GetCachedAnalysisResult() => this.analysisResult;

        /// <inheritdoc/>
        public void SetAnalysisResult(AnalysisResult analysisResult)
        {
            this.analysisResult = analysisResult;

            this.cacheValid = true;
        }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<string> files)
        {
            foreach (string filePath in files)
            {
                this.AddFile(filePath);
            }
        }

        /// <inheritdoc/>
        public string GetCachedBundleId() => this.bundleId;

        /// <inheritdoc/>
        public void SetCachedBundleId(string id) => this.bundleId = id;

        /// <inheritdoc/>
        public void Update(IFileProvider fileProvider)
        {
            foreach (string file in fileProvider.GetAddedAndChangedFiles())
            {
                this.UpdateFile(file);
            }

            foreach (string file in fileProvider.GetRemovedFiles())
            {
                this.RemoveFile(file);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetRelativeFilePaths(IEnumerable<string> files)
        {
            IList<string> relateFilePaths = new List<string>();

            foreach (string fileFullPath in files)
            {
                relateFilePaths.Add(this.GetRelativeFilePath(fileFullPath));
            }

            return relateFilePaths;
        }

        /// <summary>
        /// Add (or update if it already exists) file hash and content to cache.
        /// </summary>
        /// <param name="filePath">Source file path.</param>
        private void AddFile(string filePath)
        {
            try
            {
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string fileHash = Sha256.ComputeHash(fileContent);

                string relativeFilePath = this.GetRelativeFilePath(filePath);

                this.AddToFilePathToHashCache(relativeFilePath, fileHash);

                this.AddToFilePathToContentCache(relativeFilePath, fileContent);

                this.Invalidate();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        private void UpdateFile(string file)
        {
            this.AddFile(file);

            this.Invalidate();
        }

        private void RemoveFile(string file)
        {
            string relativeFilePath = this.GetRelativeFilePath(file);

            this.filePathToHashCache.Remove(relativeFilePath);
            this.filePathToContentCache.Remove(relativeFilePath);

            this.Invalidate();
        }

        private void Invalidate() => this.cacheValid = false;

        private string GetRelativeFilePath(string filePath)
        {
            string path = filePath.Replace(this.rootDirectoryPath, string.Empty);

            return path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private string GetRelativeFilePathIfFullPath(string filePath)
            => string.IsNullOrEmpty(filePath) || !filePath.StartsWith(this.rootDirectoryPath) ? filePath : this.GetRelativeFilePath(filePath);

        private void AddToFilePathToHashCache(string filePath, string fileHash) => this.filePathToHashCache[filePath] = fileHash;

        private void AddToFilePathToContentCache(string filePath, string fileContent) => this.filePathToContentCache[filePath] = fileContent;
    }
}
