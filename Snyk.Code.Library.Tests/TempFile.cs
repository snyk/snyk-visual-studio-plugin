namespace Snyk.Code.Library.Tests
{
    using System;
    using System.IO;

    /// <summary>
    /// A temporary file that is automatically deleted when disposed.
    /// Intended to be used as part of a <see langword="using"/> block.
    /// </summary>
    public sealed class TempFile : IDisposable
    {
        public string FilePath { get; }

        private TempFile(string filePath)
            : this(filePath, 0)
        {
        }

        private TempFile(string filePath, long fileSize)
        {
            if (File.Exists(filePath))
            {
                throw new InvalidOperationException("File already exists");
            }

            this.FilePath = filePath;
            using (var stream = File.OpenWrite(filePath))
            {
                if (fileSize > 0)
                {
                    stream.SetLength(fileSize);
                }
            }
        }

        /// <summary>
        /// Creates a random file with a fully random path.
        /// </summary>
        /// <returns>A <see cref="TempFile"/> object for the created file.</returns>
        public static TempFile Create()
        {
            var tempFileName = Path.GetTempFileName();
            return Create(tempFileName);
        }

        /// <summary>
        /// Creates a random file with a specified file name.
        /// The file will be located in a random temporary directory.
        /// </summary>
        /// <param name="fileName">The name of the file to be created</param>
        /// <returns>A <see cref="TempFile"/> object for the created file.</returns>
        public static TempFile Create(string fileName)
        {
            var tempDir = Path.GetTempPath();
            var tempFile = fileName;
            return new TempFile(Path.Combine(tempDir, tempFile));
        }

        /// <summary>
        /// Creates a random file with a specified file name and a specific size.
        /// The file will be located in a random temporary directory.
        /// </summary>
        /// <param name="fileName">The name of the file to be created</param>
        /// <param name="fileSize">The size of the new file in bytes</param>
        /// <returns>A <see cref="TempFile"/> object for the created file.</returns>
        public static TempFile Create(string fileName, long fileSize)
        {
            var tempDir = Path.GetTempPath();
            var tempFile = fileName;
            return new TempFile(Path.Combine(tempDir, tempFile), fileSize);
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(this.FilePath))
                {
                    File.Delete(this.FilePath);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Failed to delete temporary file {this.FilePath}:\n{exception.Message}");
            }
        }
    }
}