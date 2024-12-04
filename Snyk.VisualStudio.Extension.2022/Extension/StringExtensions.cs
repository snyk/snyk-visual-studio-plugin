using System;
using System.Text.RegularExpressions;

namespace Snyk.VisualStudio.Extension.Extension
{
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
            => source.EndsWith(suffix) ? source.Substring(0, source.Length - suffix.Length) : source;

        /// <summary>
        /// Remove trailing slashes from string.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <returns>Result string.</returns>
        public static string RemoveTrailingSlashes(this string source) => Regex.Replace(source, "/+$", string.Empty);

        /// <summary>
        /// Replaces the first occurrence of a string in this instance with another string.
        /// </summary>
        /// <param name="source">
        /// The string to search in.
        /// </param>
        /// <param name="oldValue">
        /// The string to replace.
        /// </param>
        /// <param name="newValue">
        /// The string to replace the first occurrence of oldValue with.
        /// </param>
        /// <returns>
        /// A string that is equivalent to the current string except that the first occurrence of <paramref name="oldValue"/> is
        /// replaced with <paramref name="newValue"/>.
        /// </returns>
        public static string ReplaceFirst(this string source, string oldValue, string newValue)
        {
            var index = source.IndexOf(oldValue, StringComparison.Ordinal);
            if (index == -1)
            {
                return source;
            }

            return source.Substring(0, index) + newValue + source.Substring(index + oldValue.Length);
        }

        public static bool IsNullOrEmpty(this string target) => string.IsNullOrEmpty(target);
    }
}