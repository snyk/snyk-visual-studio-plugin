namespace Snyk.Code.Library.Common
{
    using System.Collections;

    /// <summary>
    /// Common extension methods implementations.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Check is collection is empty (items count is 0).
        /// </summary>
        /// <param name="collection">Source collection for check.</param>
        /// <returns>True if collection is empty.</returns>
        public static bool IsNullOrEmpty(this ICollection collection) => collection == null || collection.Count == 0;

        /// <summary>
        /// Check is dictionary is empty (items count is 0).
        /// </summary>
        /// <param name="dictionary">Source dictionary for check.</param>
        /// <returns>True if dictionary is empty.</returns>
        public static bool IsNullOrEmpty(this IDictionary dictionary) => dictionary == null || dictionary.Count == 0;

        /// <summary>
        /// Compute SHA-256 hash from source string.
        /// </summary>
        /// <param name="sourceStr">Source string to hash.</param>
        /// <returns>Sha256 hash string.</returns>
        public static string GetSha256Hash(this string sourceStr) => Sha256.ComputeHash(sourceStr);
    }
}
