namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using MAB.DotIgnore;

    /// <inheritdoc/>
    public class DcIgnoreService : IDcIgnoreService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DcIgnoreService"/> class.
        /// </summary>
        public DcIgnoreService()
        {
        }

        /// <inheritdoc/>
        public void CreateDcIgnoreIfNeeded(string folderPath)
        {
            string gitIGnorePath = Path.Combine(folderPath, ".gitignore");
            string dcIGnorePath = Path.Combine(folderPath, ".dcignore");

            if (File.Exists(gitIGnorePath) || File.Exists(dcIGnorePath))
            {
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith("full.dcignore"));

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string dcIgnoreTemplate = reader.ReadToEnd();

                    File.WriteAllText(dcIGnorePath, dcIgnoreTemplate, Encoding.UTF8);
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<string> FilterFiles(string folderPath, IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
        {
            this.CreateDcIgnoreIfNeeded(folderPath);

            cancellationToken.ThrowIfCancellationRequested();

            var filteredFiles = this.FilterFilesByGitIgnore(folderPath, filePaths, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return this.FilterFilesByDcIgnore(folderPath, filteredFiles, cancellationToken);
        }

        /// <inheritdoc/>
        public IEnumerable<string> FilterFilesByDcIgnore(string folderPath, IEnumerable<string> filePaths, CancellationToken cancellationToken = default) => this.FilterFilesByIgnoreFile(folderPath, ".dcignore", filePaths);

        /// <inheritdoc/>
        public IEnumerable<string> FilterFilesByGitIgnore(string folderPath, IEnumerable<string> filePaths, CancellationToken cancellationToken = default) => this.FilterFilesByIgnoreFile(folderPath, ".gitignore", filePaths);

        private IEnumerable<string> FilterFilesByIgnoreFile(string folderPath, string ignoreFileName, IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
        {
            var gitIgnoreFiles = Directory.EnumerateFiles(folderPath, ignoreFileName, SearchOption.AllDirectories).ToList();
            string rootGitIgnoreFile = Path.Combine(folderPath, ignoreFileName);

            var projectFiles = filePaths;
            var filteredFiles = new List<string>();

            cancellationToken.ThrowIfCancellationRequested();

            if (gitIgnoreFiles.Contains(rootGitIgnoreFile))
            {
                projectFiles = this.FilterFilesByFileIgnoreList(rootGitIgnoreFile, filePaths);

                gitIgnoreFiles.Remove(rootGitIgnoreFile);
            }

            foreach (string gitIgnoreFullPath in gitIgnoreFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string gitIgnoreDir = Directory.GetParent(gitIgnoreFullPath).FullName;
                string gitIgnoreRelativeDir = gitIgnoreDir
                    .Replace(folderPath, string.Empty)
                    .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                var dirFiles = projectFiles
                    .Where(file => file.StartsWith(gitIgnoreRelativeDir))
                    .ToList();

                if (dirFiles.Count == 0)
                {
                    continue;
                }

                var files = this.FilterFilesByFileIgnoreList(gitIgnoreFullPath, dirFiles);

                filteredFiles.AddRange(files);

                projectFiles = projectFiles.Except(dirFiles);
            }

            filteredFiles.AddRange(projectFiles);

            return filteredFiles;
        }

        private IEnumerable<string> FilterFilesByFileIgnoreList(string ignoreFilePath, IEnumerable<string> filePaths)
        {
            if (!File.Exists(ignoreFilePath))
            {
                return filePaths;
            }

            var ignores = new IgnoreList(ignoreFilePath);

            return filePaths.Where(path => !ignores.IsIgnored(new FileInfo(path)));
        }
    }
}
