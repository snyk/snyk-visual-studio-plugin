namespace Snyk.Common
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Common util methods for files.
    /// </summary>
    public class FileUtil
    {
        /// <summary>
        /// Convert absolute file path list to list with relative file paths.
        /// </summary>
        /// <param name="rootPath">Root directory path.</param>
        /// <param name="files">List with absolute file paths.</param>
        /// <returns>Result list with relative file paths.</returns>
        public static IEnumerable<string> GetRelativeFilePaths(string rootPath, IEnumerable<string> files)
        {
            IList<string> relateFilePaths = new List<string>();

            foreach (string fileFullPath in files)
            {
                relateFilePaths.Add(GetRelativeFilePath(rootPath, fileFullPath));
            }

            return relateFilePaths;
        }

        /// <summary>
        /// Convert absolute file path to relative file path.
        /// </summary>
        /// <param name="rootPath">Root directory path.</param>
        /// <param name="filePath">Source absolute file path string.</param>
        /// <returns>Result string relative path.</returns>
        public static string GetRelativeFilePath(string rootPath, string filePath) =>
            filePath
                .Replace(rootPath, string.Empty)
                .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
