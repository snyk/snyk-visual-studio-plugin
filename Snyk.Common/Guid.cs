namespace Snyk.Common
{
    /// <summary>
    /// Util for guid strings.
    /// </summary>
    public class Guid
    {
        /// <summary>
        /// Check is tring is valid guid.
        /// </summary>
        /// <param name="guid">Source string with guid to check.</param>
        /// <returns>True if source string is valid guid.</returns>
        public static bool IsValid(string guid) => !string.IsNullOrEmpty(guid) && System.Guid.TryParse(guid, out _);
    }
}
