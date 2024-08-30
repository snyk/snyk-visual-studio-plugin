using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Snyk.VisualStudio.Extension.Language
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class LspAnalysisResult
    {
        public string Status { get; set; }
        public string Product { get; set; }
        public string FolderPath { get; set; }
        public string ErrorMessage { get; set; }
        public IEnumerable<Issue> Issues { get; set; }
    }

    public class AdditionalData
    {
        public string Message { get; set; }
        public string Rule { get; set; }
        public string RuleId { get; set; }
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
        public string Details { get; set; }
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
        public string GetDisplayTitleWithLineNumber() => "line " + this.Range?.End?.Line + ": " + this.GetDisplayTitle();
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
}
