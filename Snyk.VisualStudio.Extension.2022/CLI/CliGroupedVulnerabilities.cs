using Snyk.VisualStudio.Extension.Language;
using System.Collections.Generic;

namespace Snyk.VisualStudio.Extension.CLI
{

    /// <summary>
    /// Grouped CLI vulnerabilities.
    /// </summary>
    public class CliGroupedVulnerabilities
    {
        /// <summary>
        /// Gets or sets a value indicating whether vulnerabilities dictionary.
        /// </summary>
        public Dictionary<string, List<Issue>> VulnerabilitiesMap { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether unique vulnerabilities count.
        /// </summary>
        public int UniqueCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether paths count.
        /// </summary>
        public int PathsCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether project name.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether unique display target file name.
        /// </summary>
        public string DisplayTargetFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether package manager name.
        /// </summary>
        public string PackageManager { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether critical vulnerabilities count.
        /// </summary>
        public int CriticalVulnerabilitiesCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether high vulnerabilities count.
        /// </summary>
        public int HighVulnerabilitiesCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether medium vulnerabilities count.
        /// </summary>
        public int MediumVulnerabilitiesCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether low vulnerabilities count.
        /// </summary>
        public int LowVulnerabilitiesCount { get; set; }
    }
}
