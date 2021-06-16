namespace Snyk.VisualStudio.Extension.CLI
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Cli vulnerabilities array.
    /// </summary>
    public class CliVulnerabilities
    {
        /// <summary>
        /// Gets or sets a value indicating whether vulnerabilities array.
        /// </summary>
        [JsonPropertyName("vulnerabilities")]
        public Vulnerability[] Vulnerabilities { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether package manager name.
        /// </summary>
        [JsonPropertyName("packageManager")]
        public string PackageManager { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether project name.
        /// </summary>
        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether display target file.
        /// </summary>
        [JsonPropertyName("displayTargetFile")]
        public string DisplayTargetFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether path.
        /// </summary>
        [JsonPropertyName("path")]
        public string Path { get; set; }

        /// <summary>
        /// Create <see cref="CliGroupedVulnerabilities"/> instance. Group vulnerabilities by id 
        /// and count critical, high, medium, low vulnerabilities count. Count unique and paths.
        /// </summary>
        /// <returns>CliGroupedVulnerabilities.</returns>
        public CliGroupedVulnerabilities ToGroupedVulnerabilities()
        {
            var vulnerabilitiesDictionary = new Dictionary<string, List<Vulnerability>>();
            int uniqueCount = 0;
            int pathsCount = 0;

            int crititcalVulnsCount = 0;
            int highVulnsCount = 0;
            int mediumVulnsCount = 0;
            int lowVulnsCount = 0;

            Array.Sort(this.Vulnerabilities);

            foreach (Vulnerability vulnerability in this.Vulnerabilities)
            {
                var key = vulnerability.Id;

                if (vulnerabilitiesDictionary.ContainsKey(key))
                {
                    var list = vulnerabilitiesDictionary[key];

                    list.Add(vulnerability);

                    pathsCount++;
                }
                else
                {
                    var list = new List<Vulnerability>();

                    vulnerabilitiesDictionary[key] = list;

                    list.Add(vulnerability);

                    pathsCount++;
                    uniqueCount++;

                    if (vulnerability.IsCriticalSeverity())
                    {
                        crititcalVulnsCount++;
                    }

                    if (vulnerability.IsHighSeverity())
                    {
                        highVulnsCount++;
                    }

                    if (vulnerability.IsMediumSeverity())
                    {
                        mediumVulnsCount++;
                    }

                    if (vulnerability.IsLowSeverity())
                    {
                        lowVulnsCount++;
                    }
                }
            }

            return new CliGroupedVulnerabilities
            {
                VulnerabilitiesMap = vulnerabilitiesDictionary,
                UniqueCount = uniqueCount,
                PathsCount = pathsCount,
                ProjectName = this.ProjectName,
                DisplayTargetFile = this.DisplayTargetFile,
                Path = this.Path,
                CriticalVulnerabilitiesCount = crititcalVulnsCount,
                HighVulnerabilitiesCount = highVulnsCount,
                MediumVulnerabilitiesCount = mediumVulnsCount,
                LowVulnerabilitiesCount = lowVulnsCount,
                PackageManager = this.PackageManager,
            };
        }
    }
}
