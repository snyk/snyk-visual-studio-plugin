namespace Snyk.Common
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Extension methods for string class.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Remove from end of a string.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <param name="suffix">Suffix to remove.</param>
        /// <returns>Return result string.</returns>
        public static string RemoveFromEnd(this string source, string suffix)
        {
            if (source.EndsWith(suffix))
            {
                return source.Substring(0, source.Length - suffix.Length);
            }
            else
            {
                return source;
            }
        }

        /// <summary>
        /// Remove trailing slashes from string.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <returns>Result string.</returns>
        public static string RemoveTrailingSlashes(this string source) => Regex.Replace(source, "/+$", string.Empty);

        /// <summary>
        /// Check is current string is null or empty.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <returns>True if string null or empty size.</returns>
        public static bool IsNullOrEmpty(this string source) => string.IsNullOrEmpty(source);
    }
}
