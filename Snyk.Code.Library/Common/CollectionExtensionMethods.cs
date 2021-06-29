namespace Snyk.Code.Library.Common
{
    using System.Collections;

    /// <summary>
    /// Common extension methods for <see cref="ICollection"/> implementations.
    /// </summary>
    public static class CollectionExtensionMethods
    {
        /// <summary>
        /// Check is collection is empty (items count is 0).
        /// </summary>
        /// <param name="collection">Source collection for check.</param>
        /// <returns>True if collection is empty.</returns>
        public static bool IsNullOrEmpty(this ICollection collection) => collection == null || collection.Count == 0;
    }
}
