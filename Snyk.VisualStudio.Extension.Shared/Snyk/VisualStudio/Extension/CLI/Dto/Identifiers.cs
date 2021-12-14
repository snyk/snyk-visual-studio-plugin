namespace Snyk.VisualStudio.Extension.CLI
{
    /// <summary>
    /// Vulnerability identifiers.
    /// </summary>
    public class Identifiers
    {
        /// <summary>
        /// Gets or sets Cve identifiers.
        /// </summary>
        public string[] CVE { get; set; }

        /// <summary>
        /// Gets or sets Cwe identifiers.
        /// </summary>
        public string[] CWE { get; set; }

        /// <summary>
        /// Gets or sets Ghsa identifiers.
        /// </summary>
        public string[] GHSA { get; set; }

        /// <summary>
        /// Gets or sets Rhsa identifiers.
        /// </summary>
        public string[] RHSA { get; set; }
    }
}
