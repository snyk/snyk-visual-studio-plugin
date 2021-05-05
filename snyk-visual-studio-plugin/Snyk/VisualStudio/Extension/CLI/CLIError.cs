namespace Snyk.VisualStudio.Extension.CLI
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Snyk Open source error object.
    /// </summary>
    [DataContract]
    public class CliError
    {
        [DataMember(Name = "ok")]
        private bool isSuccess;

        [DataMember(Name = "error")]
        private string message;

        [DataMember]
        private string path;

        /// <summary>
        /// Initializes a new instance of the <see cref="CliError"/> class.
        /// </summary>
        public CliError() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CliError"/> class.
        /// </summary>
        /// <param name="message">Error message string.</param>
        public CliError(string message) => this.Message = message;

        /// <summary>
        /// Gets or sets a value indicating whether is success. In Json it's "ok" property.
        /// </summary>
        public bool IsSuccess
        {
            get
            {
                return this.isSuccess;
            }

            set
            {
                this.isSuccess = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether error message.
        /// </summary>
        public string Message
        {
            get
            {
                return this.message;
            }

            set
            {
                this.message = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether error path.
        /// </summary>
        public string Path
        {
            get
            {
                return this.path;
            }

            set
            {
                this.path = value;
            }
        }
    }
}
