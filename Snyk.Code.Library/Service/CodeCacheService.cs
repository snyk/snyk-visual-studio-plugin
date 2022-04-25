namespace Snyk.Code.Library.Service
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Caching;
    using System.Text;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Common;

    /// <inheritdoc/>
    public class CodeCacheService : ICodeCacheService
    {
        private const double ExpirationTime = 24.0;

        private static readonly ILogger Logger = LogManager.ForContext<CodeCacheService>();

        private readonly ObjectCache filePathToHashCache = new MemoryCache("FilePathToFileHashCache");

        private readonly ObjectCache filePathToContentCache = new MemoryCache("FilePathToFileContentCache");

        private readonly ObjectCache analysisResultCache = new MemoryCache("analysisResultCache");

        private bool isCacheValid;

        private IFileProvider fileProvider;

        private string bundleId;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeCacheService"/> class.
        /// </summary>
        /// <param name="fileProvider">File provider interface for cache.</param>
        public CodeCacheService(IFileProvider fileProvider) => this.fileProvider = fileProvider;

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
        public IDictionary<string, (string, string)> CreateFilePathToHashAndContentDictionary(IList<string> files)
        {
            var pathToHashAndContentDict = new Dictionary<string, (string, string)>();

            foreach (var keyValuePair in this.filePathToContentCache)
            {
                string path = keyValuePair.Key;
                string hash = this.filePathToHashCache[path].ToString();
                string content = keyValuePair.Value.ToString();

                if (string.IsNullOrEmpty(content))
                {
                    continue;
                }

                pathToHashAndContentDict.Add(path, (hash, content));
            }

            return pathToHashAndContentDict;
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
        public async Task<IDictionary<string, string>> GetFilePathToHashDictionaryAsync(IEnumerable<string> files)
        {
            var tasks = new List<Task>();
            var filePathToHashDict = new ConcurrentDictionary<string, string>();

            foreach (string file in files)
            {
                tasks.Add(this.AddHashToDictionaryAsync(file, filePathToHashDict));
            }

            await Task.WhenAll(tasks);

            return filePathToHashDict;
        }

        /// <inheritdoc/>
        public bool IsCacheExists() => this.GetCachedAnalysisResult() != null;

        /// <inheritdoc/>
        public bool IsCacheValid() => this.isCacheValid;

        /// <inheritdoc/>
        public AnalysisResult GetCachedAnalysisResult() =>
            this.analysisResultCache["analysisResult"] != null ? (AnalysisResult)this.analysisResultCache["analysisResult"] : null;

        /// <inheritdoc/>
        public void SetAnalysisResult(AnalysisResult analysisResult)
        {
            this.analysisResultCache.Add("analysisResult", analysisResult, this.New24HoursExpirationTimeCacheItemPolicy());

            this.isCacheValid = true;
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(IEnumerable<string> files) => await Task.WhenAll(files.Select(this.AddFileAsync));

        /// <inheritdoc/>
        public string GetCachedBundleId() => this.bundleId;

        /// <inheritdoc/>
        public void SetCachedBundleId(string id) => this.bundleId = id;

        /// <inheritdoc/>
        public async Task UpdateAsync(IFileProvider fileProvider)
        {
            await Task.WhenAll(fileProvider.GetChangedFiles().Select(this.AddFileAsync));
            await Task.WhenAll(fileProvider.GetRemovedFiles().Select(this.RemoveFileAsync));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetRelativeFilePathsAsync(IEnumerable<string> files)
        {
            IList<string> relateFilePaths = new List<string>();

            foreach (string fileFullPath in files)
            {
                var solutionPath = await this.fileProvider.GetSolutionPathAsync();

                relateFilePaths.Add(FileUtil.GetRelativeFilePath(solutionPath, fileFullPath));
            }

            return relateFilePaths;
        }

        /// <summary>
        /// Add (or update if it already exists) file hash and content to cache.
        /// </summary>
        /// <param name="filePath">Source file path.</param>
        private async Task<string> AddFileAsync(string filePath)
        {
            try
            {
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                if (string.IsNullOrEmpty(fileContent))
                {
                    return string.Empty;
                }

                string fileHash = Sha256.ComputeHash(fileContent);

                var solutionPath = await this.fileProvider.GetSolutionPathAsync();

                // Updating caches is thread-safe:
                // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.caching.objectcache?view=dotnet-plat-ext-6.0#thread-safety
                string relativeFilePath = FileUtil.GetRelativeFilePath(solutionPath, filePath);
                this.AddToFilePathToHashCache(relativeFilePath, fileHash);
                this.AddToFilePathToContentCache(relativeFilePath, fileContent);
                this.Invalidate();

                return fileHash;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to update cache.");
            }

            return string.Empty;
        }

        private async Task AddHashToDictionaryAsync(string file, ConcurrentDictionary<string, string> filePathToHashDict)
        {
            if (!File.Exists(file))
            {
                return;
            }

            string filePath = await this.GetRelativeFilePathIfFullPathAsync(file);

            if (this.filePathToHashCache[filePath] == null)
            {
                await this.AddFileAsync(file);
            }

            filePathToHashDict[filePath] = this.filePathToHashCache[filePath].ToString();
        }

        private async Task RemoveFileAsync(string file)
        {
            var solutionPath = await this.fileProvider.GetSolutionPathAsync();

            string relativeFilePath = FileUtil.GetRelativeFilePath(solutionPath, file);

            this.filePathToHashCache.Remove(relativeFilePath);
            this.filePathToContentCache.Remove(relativeFilePath);

            this.Invalidate();
        }

        private void Invalidate() => this.isCacheValid = false;

        private async Task<string> GetRelativeFilePathIfFullPathAsync(string filePath)
        {
            var solutionPath = await this.fileProvider.GetSolutionPathAsync();

            return string.IsNullOrEmpty(filePath) || !filePath.StartsWith(solutionPath)
                ? filePath : FileUtil.GetRelativeFilePath(solutionPath, filePath);
        }

        private void AddToFilePathToHashCache(string filePath, string fileHash) =>
            this.filePathToHashCache.Set(filePath, fileHash, this.New24HoursExpirationTimeCacheItemPolicy());

        private void AddToFilePathToContentCache(string filePath, string fileContent) =>
            this.filePathToContentCache.Set(filePath, fileContent, this.New24HoursExpirationTimeCacheItemPolicy());

        private CacheItemPolicy New24HoursExpirationTimeCacheItemPolicy() =>
            new CacheItemPolicy { AbsoluteExpiration = DateTime.Now.AddHours(ExpirationTime) };
    }
}
