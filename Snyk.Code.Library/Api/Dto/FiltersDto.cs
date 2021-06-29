namespace Snyk.Code.Library.Api.Dto
{
    using System.Collections.Generic;

    /// <summary>
    /// Returns the list of allowed extensions and configuration files for uploaded bundles. 
    /// This information can be used to reduce the payload size of the Create Bundle (see below) request. 
    /// Extensions begin with a dot (e.g. ".js") and config files begin with a slash (e.g. "/.eslintrc.js").
    /// </summary>
    public class FiltersDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether file extensions.
        /// </summary>
        public List<string> Extensions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether configuration files.
        /// </summary>
        public List<string> ConfigFiles { get; set; }
    }
}
