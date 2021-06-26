namespace Snyk.Code.Library.Api.Dto
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
        /// Initializes a new instance of the <see cref="Bundle"/> class.
        /// </summary>
        public Bundle()
        {
            this.Files = new Dictionary<string, string>();

            this.RemovedFiles = new List<string>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether files dictionary.
        /// For uploaded bundles, the files parameter must contain an object with slash-beginning file paths as keys and file hashes as values. 
        /// An empty object is not allowed. 
        /// The file hash must be computed by parsing the file in utf8, performing a SHA-256 on the resulting string and encoding the hash in hexadecimal.
        /// E.g. *"/.eslintrc.js": "4ed8e2973ddad1fe3eb6bbacd7b967ee8d5ef934763872c160d7cf708cc0c57e"*.
        /// </summary>
        public Dictionary<string, string> Files { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether removed files dictionary.
        /// The newly created child bundle will have the same files as the parent bundle (identified by the bundleId in the request) except for what is defined in the payload. 
        /// The removedFiles are parsed before the files, therefore if the same filePath appears in both of them it will not be removed. 
        /// The entries in the files object can either replace an old file with a new version (if the paths match) or add a new file to the child bundle. 
        /// </summary>
        public List<string> RemovedFiles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether bundle id.
        /// </summary>
        [JsonPropertyName("bundleId")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether missing files.
        /// Returns the bundleId required to access all the following APIs and, in case of uploaded bundles, 
        /// a list of file paths that still have to be uploaded (missingFiles) and where the missing files should be uploaded to (uploadURL). 
        /// When creating an uploaded bundle by directly passing the file contents in the array, missingFiles will be an empty array and the uploadURL can therefore be ignored.
        /// </summary>
        public string[] MissingFiles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether upload url for files.
        /// </summary>
        public string UploadURL { get; set; }
    }
}
