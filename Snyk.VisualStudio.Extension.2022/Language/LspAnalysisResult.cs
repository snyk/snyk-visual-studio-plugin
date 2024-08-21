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
        public List<Issue> Issues { get; set; }
    }

    public class AdditionalData
    {
        public string Message { get; set; }
        public string Rule { get; set; }
        public string RuleId { get; set; }
        public int RepoDatasetSize { get; set; }
        public List<ExampleCommitFix> ExampleCommitFixes { get; set; }
        public List<string> Cwe { get; set; }
        public string Text { get; set; }
        public List<Marker> Markers { get; set; }
        public List<int> Cols { get; set; }
        public List<int> Rows { get; set; }
        public bool IsSecurityType { get; set; }
        public int PriorityScore { get; set; }
        public bool HasAIFix { get; set; }
        public List<DataFlow> DataFlow { get; set; }
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
        public List<LineData> Lines { get; set; }
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
        public IgnoreDetails IgnoreDetails { get; set; }
        public AdditionalData AdditionalData { get; set; }
    }
    
    public class LineData
    {
        public string Line { get; set; }
        public int LineNumber { get; set; }
        public string LineChange { get; set; }
    }
    
    public class Marker
    {
        public List<int> Msg { get; set; }
        public List<Po> Pos { get; set; }
    }
    
    public class Po
    {
        public List<int> Cols { get; set; }
        public List<int> Rows { get; set; }
        public string File { get; set; }
    }
    
    public class Range
    {
        public Start Start { get; set; }
        public End End { get; set; }
    }
    
    public class Root
    {
        public List<Issue> Issues { get; set; }
    }
    
    public class Start
    {
        public int Line { get; set; }
        public int Character { get; set; }
    }
}
