namespace Snyk.Common
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Methods related to Sha256 encoding.
    /// </summary>
    public class Sha256
    {
        /// <summary>
        /// Compute SHA-256 hash from source string.
        /// </summary>
        /// <param name="sourceStr">Source string to hash.</param>
        /// <returns>Sha256 hash string.</returns>
        public static string ComputeHash(string sourceStr)
        {
            // Create a SHA256.
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array.
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(sourceStr));

                // Convert byte array to a string.
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// Get SHA 256 checksum for file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <returns>Checksum string.</returns>
        public static string Checksum(string filePath)
        {
            using (var sha256 = SHA256Managed.Create())
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    return BitConverter.ToString(sha256.ComputeHash(fileStream)).Replace("-", string.Empty);
                }
            }
        }
    }
}
