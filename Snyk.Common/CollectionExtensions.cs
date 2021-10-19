namespace Snyk.Common
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Common extension methods implementations.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Check is collection is empty (items count is 0).
        /// </summary>
        /// <param name="collection">Source collection for check.</param>
        /// <returns>True if collection is empty.</returns>
        public static bool IsNullOrEmpty(this ICollection collection) => collection == null || collection.Count == 0;

        /// <summary>
        /// Check is collection is empty (items count is 0).
        /// </summary>
        /// <param name="collection">Source collection for check.</param>
        /// <returns>True if collection is empty.</returns>
        public static bool IsNullOrEmpty(this ICollection<string> collection) => collection == null || collection.Count == 0;

        /// <summary>
        /// Check is list is empty (items count is 0).
        /// </summary>
        /// <param name="list">Source list for check.</param>
        /// <returns>True if list is empty.</returns>
        public static bool IsEmpty(this IList<string> list) => list.Count == 0;

        /// <summary>
        /// Check is dictionary is empty (items count is 0).
        /// </summary>
        /// <param name="dictionary">Source dictionary for check.</param>
        /// <returns>True if dictionary is empty.</returns>
        public static bool IsNullOrEmpty(this IDictionary dictionary) => dictionary == null || dictionary.Count == 0;

        /// <summary>
        /// Check is dictionary is empty (items count is 0).
        /// </summary>
        /// <param name="dictionary">Source dictionary for check.</param>
        /// <returns>True if dictionary is empty.</returns>
        public static bool IsNullOrEmpty(this IDictionary<string, string> dictionary) => dictionary == null || dictionary.Count == 0;

        /// <summary>
        /// Check is collection is empty (items count is 0).
        /// </summary>
        /// <param name="collection">Source collection for check.</param>
        /// <returns>True if collection is empty.</returns>
        public static bool IsNullOrEmpty(this IEnumerable<string> collection) => collection == null || collection.Count() == 0;
    }
}
