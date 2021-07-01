namespace Snyk.Code.Library.Api.Dto
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Data transfer object for file content pair: file path hash - file content.
    /// </summary>
    public class CodeFileDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeFileDto"/> class.
        /// </summary>
        public CodeFileDto()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeFileDto"/> class.
        /// </summary>
        /// <param name="fileHash">File hash value.</param>
        /// <param name="fileContent">File content value.</param>
        public CodeFileDto(string fileHash, string fileContent)
        {
            this.Hash = fileHash;
            this.Content = fileContent;
        }

        /// <summary>
        /// Gets or sets a value indicating whether file path hash.
        /// </summary>
        [JsonPropertyName("fileHash")]
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether file content.
        /// </summary>
        [JsonPropertyName("fileContent")]
        public string Content { get; set; }
    }
}
