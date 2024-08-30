using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;
using Snyk.Code.Library.Domain.Analysis;
using Snyk.Common;
using Snyk.VisualStudio.Extension.Model;
using Snyk.VisualStudio.Extension.Service;
using StreamJsonRpc;
using Position = Snyk.Code.Library.Domain.Analysis.Position;

namespace Snyk.VisualStudio.Extension.Language
{
    public class SnykLanguageClientCustomTarget
    {
        private readonly ISnykServiceProvider serviceProvider;
        private ConcurrentDictionary<string, IEnumerable<Issue>> snykIssueDictionary = new ConcurrentDictionary<string, IEnumerable<Issue>>();
        private static readonly ILogger _logger = LogManager.ForContext<SnykLanguageClientCustomTarget>();
        public SnykLanguageClientCustomTarget(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        [JsonRpcMethod("$/snyk.publishDiagnostics316")]
        public void OnPublishDiagnostics316(JToken arg)
        {
            var uri = arg["uri"];
            var diagnosticsArray = (JArray)arg["diagnostics"];
            if (uri == null)
            {
                return;
            }

            var parsedUri = new Uri(uri.ToString());
            if (diagnosticsArray == null || diagnosticsArray.Count == 0)
            {
                snykIssueDictionary.TryRemove(parsedUri.AbsolutePath, out _);
                return;
            }

            if (diagnosticsArray[0]["source"] == null)
            {
                return;
            }

            var source = diagnosticsArray[0]["source"].ToString();
            var dataList = diagnosticsArray.Where(x => x["data"] != null)
                .Select(x =>
                {
                    var issue = x["data"].TryParse<Issue>();
                    issue.Product = LspSourceToProduct(source);
                    return issue;
                } );
            
            snykIssueDictionary.TryAdd(parsedUri.AbsolutePath, dataList);
        }

        [JsonRpcMethod("$/snyk.scan")]
        public async Task OnSnykScan(JToken arg)
        {
            var lspAnalysisResult = arg.TryParse<LspAnalysisResult>();
            if (lspAnalysisResult == null) return;
            if (lspAnalysisResult.Status == "inProgress")
            {
                var featuresSettings = await serviceProvider.TasksService.GetFeaturesSettingsAsync();
                serviceProvider.TasksService.FireSnykCodeScanningStartedEvent(featuresSettings);
                return;
            }
            if (lspAnalysisResult.Status == "error")
            {
                serviceProvider.TasksService.OnSnykCodeError(lspAnalysisResult.ErrorMessage);
                return;
            }
            if (lspAnalysisResult.Status != "success" || lspAnalysisResult.Product != "code")
            {
                return;
            }

            var res = MapDtoAnalysisResultToDomain(snykIssueDictionary, lspAnalysisResult);
            serviceProvider.TasksService.FireScanningUpdateEvent(snykIssueDictionary);
        }

        [JsonRpcMethod("$/snyk.getFeatureFlagStatus")]
        public void OnSnykGetFeatureFlagStatus(JToken arg)
        {

        }

        private string LspSourceToProduct(string source)
        {
            return source switch
            {
                "Snyk Code" => "code",
                "Snyk Open Source" => "oss",
                "Snyk IaC" => "iac",
                _ => ""
            };
        }
        private AnalysisResult MapDtoAnalysisResultToDomain(IDictionary<string, IEnumerable<Issue>> snykIssueDictionary, LspAnalysisResult lspAnalysisResult)
        {
            var analysisResult = new AnalysisResult
            {
                Status = lspAnalysisResult.Status,
            };
            

            foreach (var kv in snykIssueDictionary)
            {
                var fileAnalysis = new FileAnalysis { FileName = kv.Key, };

                foreach (var issue in kv.Value.Where(x=>x.Product == lspAnalysisResult.Product))
                {
                    var exampleFixes = new List<SuggestionFix>();
                    if (issue.AdditionalData.ExampleCommitFixes != null)
                    {
                        foreach (var exampleCommitFixes in issue.AdditionalData.ExampleCommitFixes)
                        {
                            var suggestionFix = new SuggestionFix
                            {
                                CommitURL = exampleCommitFixes.CommitURL,
                                Lines = exampleCommitFixes.Lines
                                    .Select(line => new FixLine
                                    {
                                        Line = line.Line, LineChange = line.LineChange, LineNumber = line.LineNumber
                                    }).ToList(),
                            };

                            exampleFixes.Add(suggestionFix);
                        }
                    }

                    var fileName = fileAnalysis.FileName;
                    var suggestion = new Suggestion();
                    suggestion.Id = issue.AdditionalData?.RuleId;
                    suggestion.Rule = issue.AdditionalData?.Rule;
                    suggestion.Message = issue.AdditionalData?.Message;
                    suggestion.Severity = Severity.ToInt(issue.Severity);
                    suggestion.Categories = issue.AdditionalData?.IsSecurityType ?? false ? ["Security"] : [""];
                    //suggestion.Tags = issue.Tag;
                    suggestion.Title = issue.Title;
                    suggestion.Cwe = issue.AdditionalData?.Cwe;
                    suggestion.Text = issue.AdditionalData?.Text;
                    suggestion.FileName = fileName;
                    suggestion.RepoDatasetSize = issue.AdditionalData?.RepoDatasetSize ?? 0;
                    //suggestion.ExampleCommitDescriptions = issue.AdditionalData?.ExampleCommitFixes;
                    suggestion.Rows = Tuple.Create(issue.Range.Start.Line, issue.Range.End.Line);
                    suggestion.Columns = Tuple.Create(issue.Range.Start.Character, issue.Range.End.Character);
                    if (issue.AdditionalData?.Markers != null)
                    {
                        suggestion.Markers = issue.AdditionalData?.Markers?.Select(markerDto => new Snyk.Code.Library.Domain.Analysis.Marker
                        {
                            Positions = markerDto.Pos.Select(positionDto => new Position
                            {
                                Columns = positionDto.Cols.Select(x=>(long)x).ToList(),
                                Rows = positionDto.Rows.Select(x => (long)x).ToList(),
                                FileName = positionDto.File,
                            }).ToList(),
                        }).ToList();
                    }
                    foreach (var exampleFix in exampleFixes)
                    {
                        suggestion.Fixes.Add(exampleFix);
                    }

                    fileAnalysis.Suggestions.Add(suggestion);
                }

                analysisResult.FileAnalyses.Add(fileAnalysis);
            }

            return analysisResult;
        }
    }
}
