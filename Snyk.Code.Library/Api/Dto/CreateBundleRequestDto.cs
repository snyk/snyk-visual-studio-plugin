namespace Snyk.Code.Library.Api.Dto
{
    using System.Collections.Generic;


    /// <summary>
    /// DTO class for create bundle request.
    /// </summary>
    public class CreateBundleRequestDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateBundleRequestDto"/> class.
        /// </summary>
        public CreateBundleRequestDto() => this.Files = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets a value indicating files dictionary.
        /// For uploaded bundles, the files parameter must contain an object with slash-beginning file paths as keys and file hashes as values. 
        /// An empty object is not allowed. 
        /// The file hash must be computed by parsing the file in utf8, performing a SHA-256 on the resulting string and encoding the hash in hexadecimal.
        /// E.g. *"/.eslintrc.js": "4ed8e2973ddad1fe3eb6bbacd7b967ee8d5ef934763872c160d7cf708cc0c57e"*.
        /// </summary>
        public Dictionary<string, string> Files { get; set; }
    }
}
