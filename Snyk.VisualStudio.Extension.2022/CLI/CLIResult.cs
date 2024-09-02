namespace Snyk.VisualStudio.Extension.CLI
{
    using System.Collections.Generic;

    /// <summary>
    /// Snyk Open source CLI result object.
    /// </summary>
    public class CliResult
    {
        private int criticalSeverityCount = 0;
        private int highSeverityCount = 0;
        private int mediumSeverityCount = 0;
        private int lowSeverityCount = 0;

        private List<CliGroupedVulnerabilities> groupedVulnerabilities;

        /// <summary>
        /// Gets or sets a value indicating whether CliVulnerabilities List.
        /// </summary>
        public List<CliVulnerabilities> CliVulnerabilitiesList { get; set; }

        /// <summary>
        /// Gets critical severity issues count.
        /// </summary>
        public int CriticalSeverityCount => this.criticalSeverityCount;

        /// <summary>
        /// Gets high severity issues count.
        /// </summary>
        public int HighSeverityCount => this.highSeverityCount;

        /// <summary>
        /// Gets medium severity issues count.
        /// </summary>
        public int MediumSeverityCount => this.mediumSeverityCount;

        /// <summary>
        /// Gets low severity issues count.
        /// </summary>
        public int LowSeverityCount => this.lowSeverityCount;

        /// <summary>
        /// Gets total issues count (critical, high, medium and low).
        /// </summary>
        public int Count => this.CriticalSeverityCount + this.HighSeverityCount + this.MediumSeverityCount + this.LowSeverityCount;

        /// <summary>
        /// Gets list of <see cref="CliGroupedVulnerabilities"/>.
        /// </summary>
        public List<CliGroupedVulnerabilities> GroupVulnerabilities
        {
            get
            {
                if (this.groupedVulnerabilities == null)
                {
                    this.groupedVulnerabilities = new List<CliGroupedVulnerabilities>();

                    foreach (CliVulnerabilities cliVulnerabilities in this.CliVulnerabilitiesList)
                    {
                        CliGroupedVulnerabilities groupedVulns = cliVulnerabilities.ToGroupedVulnerabilities();

                        this.criticalSeverityCount += groupedVulns.CriticalVulnerabilitiesCount;
                        this.highSeverityCount += groupedVulns.HighVulnerabilitiesCount;
                        this.mediumSeverityCount += groupedVulns.MediumVulnerabilitiesCount;
                        this.lowSeverityCount += groupedVulns.LowVulnerabilitiesCount;

                        this.groupedVulnerabilities.Add(groupedVulns);
                    }
                }

                return this.groupedVulnerabilities;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether instance of <see cref="OssError"/>.
        /// </summary>
        public OssError Error { get; set; }

        /// <summary>
        /// Check is response successfull or there is an error.
        /// </summary>
        /// <returns>Bool.</returns>
        public bool IsSuccessful() => this.Error == null;
    }
}
