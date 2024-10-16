using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Snyk.VisualStudio.Extension.Language
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class LsAnalysisResult
    {
        public string Status { get; set; }
        public string Product { get; set; }
        public string FolderPath { get; set; }
        public string ErrorMessage { get; set; }
        public IEnumerable<Issue> Issues { get; set; }
    }

    public class AdditionalData
    {
        // Code
        public string Message { get; set; }
        public string Rule { get; set; }
        public int RepoDatasetSize { get; set; }
        public IList<ExampleCommitFix> ExampleCommitFixes { get; set; }
        public IList<string> Cwe { get; set; }
        public string Text { get; set; }
        public IList<Marker> Markers { get; set; }
        public IList<int> Cols { get; set; }
        public IList<int> Rows { get; set; }
        public bool IsSecurityType { get; set; }
        public int PriorityScore { get; set; }
        public bool HasAIFix { get; set; }
        public IList<DataFlow> DataFlow { get; set; }

        // OSS + Code
        public string RuleId { get; set; }
        public string Details { get; set; }

        // OSS
        public string License { get; set; }
        public Identifiers Identifiers { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string PackageManager { get; set; }
        public string PackageName { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Exploit { get; set; }
        public string CVSSv3 { get; set; }
        public string CvssScore { get; set; }
        public IList<string> FixedIn { get; set; }
        public IList<string> From { get; set; }
        public IList<string> UpgradePath { get; set; }
        public bool IsPatchable { get; set; }
        public bool IsUpgradable { get; set; }
        public string ProjectName { get; set; }
        public string DisplayTargetFile { get; set; }

        // IaC
        public string PublicId { get; set; }
        public string Documentation { get; set; }
        public int LineNumber { get; set; }
        public string Issue { get; set; }
        public string Impact { get; set; }
        public IList<string> Path { get; set; }
        public string Resolve { get; set; }
        public IList<string> References { get; set; }
        public string CustomUIContent { get; set; }
    }

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
    }

    public class DataFlow
    {
        public int Position { get; set; }
        public string FilePath { get; set; }
        public FlowRange FlowRange { get; set; }
        public string Content { get; set; }
    }

    public class End
    {
        public int Line { get; set; }
        public int Character { get; set; }
    }

    public class ExampleCommitFix
    {
        public string CommitURL { get; set; }
        public IList<LineData> Lines { get; set; }
    }

    public class FlowRange
    {
        public Start Start { get; set; }
        public End End { get; set; }
    }

    public class IgnoreDetails
    {
        public string Category { get; set; }
        public string Reason { get; set; }
        public string Expiration { get; set; }
        public DateTime IgnoredOn { get; set; }
        public string IgnoredBy { get; set; }
    }

    public class Issue
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Severity { get; set; }
        public string FilePath { get; set; }
        public Range Range { get; set; }
        public bool IsIgnored { get; set; }
        public bool IsNew { get; set; }
        public IgnoreDetails IgnoreDetails { get; set; }
        public AdditionalData AdditionalData { get; set; }
        public string Product { get; set; }

        public string GetDisplayTitle() => string.IsNullOrEmpty(this.Title) ? this.AdditionalData?.Message : this.Title;

        public string GetDisplayTitleWithLineNumber()
        {
            var line = "line " + this.Range?.End?.Line + ": " + this.GetDisplayTitle();
            if (this.AdditionalData?.HasAIFix ?? false)
            {
                line = "⚡" + line;
            }

            return line;
        }

        public string GetPackageNameTitle() => $"{this.AdditionalData?.PackageName}@{this.AdditionalData?.Version}: {this.Title}";
    }

    public class LineData
    {
        public string Line { get; set; }
        public int LineNumber { get; set; }
        public string LineChange { get; set; }
    }
    
    public class Marker
    {
        public IList<int> Msg { get; set; }
        public IList<Po> Pos { get; set; }
    }
    
    public class Po
    {
        public IList<int> Cols { get; set; }
        public IList<int> Rows { get; set; }
        public string File { get; set; }
    }
    
    public class Range
    {
        public Start Start { get; set; }
        public End End { get; set; }
    }
    
    public class Root
    {
        public IList<Issue> Issues { get; set; }
    }
    
    public class Start
    {
        public int Line { get; set; }
        public int Character { get; set; }
    }

    public class LsTrust
    {
        public IList<string> TrustedFolders { get; set; }
    }
}
