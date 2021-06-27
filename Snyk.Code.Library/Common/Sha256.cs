namespace Snyk.Code.Library.Common
{
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
    }
}
