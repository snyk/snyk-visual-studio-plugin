namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using MAB.DotIgnore;

    /// <inheritdoc/>
    public class DcIgnoreService : IDcIgnoreService
    {
        private string folderPath;

        private string gitIGnorePath;
        private string dcIGnorePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="DcIgnoreService"/> class.
        /// </summary>
        /// <param name="folderPath">Basic folder path.</param>
        public DcIgnoreService(string folderPath)
        {
            this.folderPath = folderPath;

            this.gitIGnorePath = Path.Combine(this.folderPath, ".gitignore");
            this.dcIGnorePath = Path.Combine(this.folderPath, ".dcignore");
        }

        /// <inheritdoc/>
        public void CreateDcIgnoreIfNeeded()
        {
            if (!File.Exists(this.gitIGnorePath) && !File.Exists(this.dcIGnorePath))
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourcePath = assembly.GetManifestResourceNames()
                        .Single(str => str.EndsWith("full.dcignore"));

                using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string dcIgnoreTemplate = reader.ReadToEnd();

                        File.WriteAllText(this.dcIGnorePath, dcIgnoreTemplate);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public IList<string> FilterFiles(IList<string> filePaths)
        {
            this.CreateDcIgnoreIfNeeded();

            var filteredFiles = this.FilterFilesByGitIgnore(filePaths);

            return this.FilterFilesByDcIgnore(filteredFiles);
        }

        /// <inheritdoc/>
        public IList<string> FilterFilesByDcIgnore(IList<string> filePaths)
            => this.FilterFilesByIgnoreFile(this.dcIGnorePath, filePaths);

        /// <inheritdoc/>
        public IList<string> FilterFilesByGitIgnore(IList<string> filePaths)
            => this.FilterFilesByIgnoreFile(this.gitIGnorePath, filePaths);

        private IList<string> FilterFilesByIgnoreFile(string ignoreFilePath, IList<string> filePaths)
        {
            if (!File.Exists(ignoreFilePath))
            {
                return filePaths;
            }

            var ignores = new IgnoreList(ignoreFilePath);

            return filePaths.Where(path => !ignores.IsIgnored(new FileInfo(path))).ToList();
        }
    }
}
