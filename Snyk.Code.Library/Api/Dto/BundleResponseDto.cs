﻿namespace Snyk.Code.Library.Api.Dto
{
    using Newtonsoft.Json;

    /// <summary>
    /// For uploaded bundles, the files parameter contain an object with slash-beginning file paths as keys and file hashes as values. 
    /// An empty object is not allowed. The file hash must be computed by parsing the file in utf8, 
    /// performing a SHA-256 on the resulting string and encoding the hash in hexadecimal 
    /// (e.g. *"/.eslintrc.js": "4ed8e2973ddad1fe3eb6bbacd7b967ee8d5ef934763872c160d7cf708cc0c57e"*).
    /// </summary>
    public class BundleResponseDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BundleResponseDto"/> class.
        /// </summary>
        public BundleResponseDto()
        {
            this.MissingFiles = new string[0];
        }

        /// <summary>
        /// Gets or sets a value indicating bundle hash.
        /// </summary>
        [JsonProperty("bundleHash")]
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether missing files.
        /// Returns the bundleId required to access all the following APIs and, in case of uploaded bundles, 
        /// a list of file paths that still have to be uploaded (missingFiles) and where the missing files should be uploaded to (uploadURL). 
        /// When creating an uploaded bundle by directly passing the file contents in the array, missingFiles will be an empty array and the uploadURL can therefore be ignored.
        /// </summary>
        public string[] MissingFiles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating upload url for files.
        /// </summary>
        public string UploadURL { get; set; }
    }
}
