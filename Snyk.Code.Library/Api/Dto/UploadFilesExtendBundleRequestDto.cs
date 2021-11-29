namespace Snyk.Code.Library.Api.Dto
{
    using System.Collections.Generic;


    /// <summary>
    /// DTO class for create bundle request.
    /// </summary>
    public class UploadFilesExtendBundleRequestDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UploadFilesExtendBundleRequestDto"/> class.
        /// </summary>
        public UploadFilesExtendBundleRequestDto()
        {
            this.Files = new Dictionary<string, CodeFileDto>();

            this.RemovedFiles = new List<string>();
        }

        /// <summary>
        /// Gets or sets a value indicating files dictionary.
        /// For uploaded bundles, the files parameter must contain an object with slash-beginning file paths as keys and file hashes as values. 
        /// An empty object is not allowed. 
        /// The file hash must be computed by parsing the file in utf8, performing a SHA-256 on the resulting string and encoding the hash in hexadecimal.
        /// E.g. *"/.eslintrc.js": "4ed8e2973ddad1fe3eb6bbacd7b967ee8d5ef934763872c160d7cf708cc0c57e"*.
        /// </summary>
        public Dictionary<string, CodeFileDto> Files { get; set; }

        /// <summary>
        /// Gets or sets a value indicating removed files dictionary.
        /// The newly created child bundle will have the same files as the parent bundle (identified by the bundleId in the request) except for what is defined in the payload. 
        /// The removedFiles are parsed before the files, therefore if the same filePath appears in both of them it will not be removed. 
        /// The entries in the files object can either replace an old file with a new version (if the paths match) or add a new file to the child bundle. 
        /// </summary>
        public IEnumerable<string> RemovedFiles { get; set; }
    }
}
