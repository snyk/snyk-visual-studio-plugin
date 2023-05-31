namespace Snyk.Common
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Common extension methods implementations.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Check is collection is empty (items count is 0) or null.
        /// </summary>
        /// <param name="collection">Source collection for check.</param>
        /// <returns>True if collection is empty.</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection) => collection?.Any() == true;
    }
}
