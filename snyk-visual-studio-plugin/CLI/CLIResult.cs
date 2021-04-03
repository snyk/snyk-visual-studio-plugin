using System;
using System.Collections.Generic;

namespace Snyk.VisualStudio.Extension.CLI
{
    public class CliResult
    {
        public List<CliVulnerabilities> CliVulnerabilitiesList { get; set; }

        private int highSeverityCount = 0;
        private int mediumSeverityCount = 0;
        private int lowSeverityCount = 0;

        private List<CliGroupedVulnerabilities> groupedVulnerabilities;

        public int HighSeverityCount => highSeverityCount;
        public int MediumSeverityCount => mediumSeverityCount;        
        public int LowSeverityCount => lowSeverityCount;

        public int Count => HighSeverityCount + MediumSeverityCount + LowSeverityCount;

        public List<CliGroupedVulnerabilities> GroupVulnerabilities
        {
            get
            {
                if (groupedVulnerabilities == null)
                {
                    groupedVulnerabilities = new List<CliGroupedVulnerabilities>();

                    foreach (CliVulnerabilities cliVulnerabilities in CliVulnerabilitiesList)
                    {
                        CliGroupedVulnerabilities groupedVulns = cliVulnerabilities.ToGroupedVulnerabilities();

                        highSeverityCount += groupedVulns.HighVulnerabilitiesCount;
                        mediumSeverityCount += groupedVulns.MediumVulnerabilitiesCount;
                        lowSeverityCount += groupedVulns.LowVulnerabilitiesCount;

                        groupedVulnerabilities.Add(groupedVulns);
                    }
                }

                return groupedVulnerabilities;
            }
        }

        public CliError Error { get; set; }

        public bool IsSuccessful()
        {
            return Error == null;
        }
    }

    public class CliGroupedVulnerabilities
    {
        public Dictionary<string, List<Vulnerability>> VulnerabilitiesMap { get; set; }
        public int UniqueCount { get; set; }
        public int PathsCount { get; set; }
        public string ProjectName { get; set; }
        public string DisplayTargetFile { get; set; }
        public string Path { get; set; }

        public string PackageManager { get; set; }

        public int HighVulnerabilitiesCount { get; set; }
        public int MediumVulnerabilitiesCount { get; set; }
        public int LowVulnerabilitiesCount { get; set; }
    }    

    public class CliVulnerabilities
    {
        public Vulnerability[] vulnerabilities { get; set; }
        public bool ok { get; set; }
        public int dependencyCount { get; set; }
        public string org { get; set; }
        public string policy { get; set; }
        public bool isPrivate { get; set; }
        public Licensespolicy licensesPolicy { get; set; }
        public string packageManager { get; set; }
        public object ignoreSettings { get; set; }
        public string summary { get; set; }
        public bool filesystemPolicy { get; set; }
        public Filtered filtered { get; set; }
        public int uniqueCount { get; set; }
        public string targetFile { get; set; }
        public string projectName { get; set; }
        public string displayTargetFile { get; set; }
        public string path { get; set; }

        public CliGroupedVulnerabilities ToGroupedVulnerabilities()
        {
            var vulnerabilitiesDictionary = new Dictionary<string, List<Vulnerability>>();
            int uniqueCount = 0;
            int pathsCount = 0;

            int highVulnsCount = 0;
            int mediumVulnsCount = 0;
            int lowVulnsCount = 0;

            Array.Sort(vulnerabilities);

            foreach (Vulnerability vulnerability in vulnerabilities)
            {
                var key = vulnerability.id;

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

                    if (vulnerability.severity == Severity.High)
                    {
                        highVulnsCount++;
                    }

                    if (vulnerability.severity == Severity.Medium)
                    {
                        mediumVulnsCount++;
                    }

                    if (vulnerability.severity == Severity.Low)
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
                ProjectName = projectName,
                DisplayTargetFile = displayTargetFile,
                Path = path,
                HighVulnerabilitiesCount = highVulnsCount,
                MediumVulnerabilitiesCount = mediumVulnsCount,
                LowVulnerabilitiesCount = lowVulnsCount,
                PackageManager = this.packageManager
            };
        }        
    }

    public class Licensespolicy
    {
        public Severities severities { get; set; }
        public Orglicenserules orgLicenseRules { get; set; }
    }

    public class Severities
    {
    }

    public class Orglicenserules
    {
        public AGPL10 AGPL10 { get; set; }
        public AGPL30 AGPL30 { get; set; }
        public Artistic10 Artistic10 { get; set; }
        public Artistic20 Artistic20 { get; set; }
        public CDDL10 CDDL10 { get; set; }
        public CPOL102 CPOL102 { get; set; }
        public EPL10 EPL10 { get; set; }
        public GPL20 GPL20 { get; set; }
        public GPL30 GPL30 { get; set; }
        public LGPL20 LGPL20 { get; set; }
        public LGPL21 LGPL21 { get; set; }
        public LGPL30 LGPL30 { get; set; }
        public MPL11 MPL11 { get; set; }
        public MPL20 MPL20 { get; set; }
        public MSRL MSRL { get; set; }
        public Simpl20 SimPL20 { get; set; }
    }

    public class AGPL10
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }

    public class AGPL30
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }

    public class Artistic10
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }

    public class Artistic20
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }

    public class CDDL10
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }

    public class CPOL102
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }

    public class EPL10
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }

    public class GPL20
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }

    public class GPL30
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }

    public class LGPL20
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }

    public class LGPL21
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }

    public class LGPL30
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }    

    public class MPL11
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }

    public class MPL20
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }

    public class MSRL
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }

    public class Simpl20
    {
        public string licenseType { get; set; }
        public string severity { get; set; }
        public string instructions { get; set; }
    }

    public class Filtered
    {
        public object[] ignore { get; set; }
        public object[] patch { get; set; }
    }

    public class Severity
    {
        public const string High = "high";
        public const string Medium = "medium";
        public const string Low = "low";
    }

    public class Vulnerability : IComparable<Vulnerability>
    {
        public string CVSSv3 { get; set; }
        public object[] alternativeIds { get; set; }
        //public DateTime creationTime { get; set; }
        public string[] credit { get; set; }
        public float cvssScore { get; set; }
        public string description { get; set; }
        //public DateTime disclosureTime { get; set; }
        public string exploit { get; set; }
        public string[] fixedIn { get; set; }
        public object[] functions { get; set; }
        public object[] functions_new { get; set; }
        public string id { get; set; }
        public Identifiers identifiers { get; set; }
        public string language { get; set; }
        //public DateTime modificationTime { get; set; }
        public string moduleName { get; set; }
        public string packageManager { get; set; }
        public string packageName { get; set; }
        public object[] patches { get; set; }
        public bool proprietary { get; set; }
        //public DateTime publicationTime { get; set; }
        public Reference[] references { get; set; }
        public Semver semver { get; set; }
        public string severity { get; set; }
        public string severityWithCritical { get; set; }
        public string title { get; set; }
        public string[] from { get; set; }
        public object[] upgradePath { get; set; }
        public bool isUpgradable { get; set; }
        public bool isPatchable { get; set; }
        public string name { get; set; }
        public string version { get; set; }

        public string GetPackageNameTitle() => $"{packageName}@{version}: {title}";

        public int CompareTo(Vulnerability otherVulnerability)
        {
            if (this.severity == otherVulnerability.severity)
            {
                return 0;
            }

            if (this.severity == Severity.Low && otherVulnerability.severity != Severity.Low)
            {
                return 1;
            }
            
            if (this.severity == Severity.Medium && otherVulnerability.severity == Severity.High)
            {
                return 1;
            }

            return -1;
        }

        public string IntroducedThrough
        {
            get
            {
                return from == null ? "" : string.Join(" > ", from);
            }
        }

        public string Remediation
        {
            get
            {
                return fixedIn == null ? "" : string.Join(", ", fixedIn);
            }
        }

        public string url
        {
            get
            {
                return "https://snyk.io/vuln/" + id;
            }
        }

        public string Overview
        {
            get
            {
                string temp = description.Substring("## Overview".Length);

                int endIndex = temp.IndexOf("## ");

                return endIndex > 0 ? temp.Substring(0, endIndex) : "";              
            }
        }
    }

    public class Identifiers
    {
        public string[] CVE { get; set; }
        public string[] CWE { get; set; }
    }

    public class Semver
    {
        public string[] vulnerable { get; set; }
    }

    public class Reference
    {
        public string title { get; set; }
        public string url { get; set; }
    }
}
