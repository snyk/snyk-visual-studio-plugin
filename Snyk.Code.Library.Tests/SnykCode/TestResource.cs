namespace Snyk.Code.Library.Tests.Api
{
    using System.IO;

    /// <summary>
    /// Helper class to get resource information for tests.
    /// </summary>
    public class TestResource
    {
        /// <summary>
        /// Get full path for file in test resources.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <returns>Full path string.</returns>
        public static string GetFileFullPath(string fileName)
            => Path.Combine(Directory.GetCurrentDirectory(), "Resources", fileName);

        /// <summary>
        /// Get path to Resources directory.
        /// </summary>
        /// <returns>Resources directory path string.</returns>
        public static string GetResourcesPath() => Path.Combine(Directory.GetCurrentDirectory(), "Resources");

        /// <summary>
        /// Get file content as string.
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>File content string.</returns>
        public static string GetFileContent(string fileName) => File.ReadAllText(GetFileFullPath(fileName));
    }
}
