namespace Snyk.SnykCode
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// For uploaded bundles, the files parameter contain an object with slash-beginning file paths as keys and file hashes as values. 
    /// An empty object is not allowed. The file hash must be computed by parsing the file in utf8, 
    /// performing a SHA-256 on the resulting string and encoding the hash in hexadecimal 
    /// (e.g. *"/.eslintrc.js": "4ed8e2973ddad1fe3eb6bbacd7b967ee8d5ef934763872c160d7cf708cc0c57e"*).
    /// </summary>
    public class Bundle
    {
        /// <summary>
        /// Gets or sets a value indicating whether files dictionary.
        /// </summary>
        public Dictionary<string, string> Files { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether bundle id.
        /// </summary>
        [JsonPropertyName("bundleId")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether missing files.
        /// </summary>
        public Dictionary<string, string> MissingFiles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether upload url for files.
        /// </summary>
        public string UploadURL { get; set; }
    }
}
