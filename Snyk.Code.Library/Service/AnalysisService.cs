namespace Snyk.Code.Library.Service
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto.Analysis;
    using Snyk.Code.Library.Domain.Analysis;

    /// <inheritdoc/>
    public class AnalysisService : IAnalysisService
    {
        private const int RequestTimeout = 100;

        private const int RequestAttempts = 100;

        private ISnykCodeClient codeClient;

        public AnalysisService(ISnykCodeClient codeClient) => this.codeClient = codeClient;

        /// <inheritdoc/>
        public async Task<AnalysisResult> GetAnalysisAsync(string bundleId)
        {
            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            AnalysisResultDto analysisResultDto;

            int counter = 1;

            do
            {
                analysisResultDto = await this.codeClient.GetAnalysisAsync(bundleId);

                if (analysisResultDto.Status == AnalysisStatus.Waiting)
                {
                    System.Threading.Thread.Sleep(RequestTimeout);
                }

                if (analysisResultDto.Status == AnalysisStatus.Failed)
                {
                    throw new SnykCodeException("SnykCode Analysis failed.");
                }

                if (counter >= RequestAttempts)
                {
                    analysisResultDto = new AnalysisResultDto
                    {
                        Status = AnalysisStatus.Failed,
                    };

                    break;
                }

                counter++;
            }
            while (analysisResultDto.Status != AnalysisStatus.Done);

            return this.MapDtoAnalysisResultToDomain(analysisResultDto);
        }

        private AnalysisResult MapDtoAnalysisResultToDomain(AnalysisResultDto analysisResultDto)
        {
            var analysisrResult = new AnalysisResult
            {
                Status = analysisResultDto.Status,
                Progress = analysisResultDto.Progress,
                URL = analysisResultDto.AnalysisURL,
            };

            var analysisResults = analysisResultDto.AnalysisResults;

            if (analysisResults == null)
            {
                return analysisrResult;
            }

            foreach (var fileKeyPair in analysisResults.Files)
            {
                var fileAnalysis = new FileAnalysis { FileName = fileKeyPair.Key, };

                foreach (var suggestionIdToFileKeyPair in fileKeyPair.Value)
                {
                    string suggestionId = suggestionIdToFileKeyPair.Key;
                    var fileDtos = suggestionIdToFileKeyPair.Value;

                    var suggestionDto = analysisResults.Suggestions[suggestionId];

                    var suggestion = new Suggestion
                    {
                        Id = suggestionDto.Id,
                        Rule = suggestionDto.Rule,
                        Message = suggestionDto.Message,
                        Severity = suggestionDto.Severity,
                        Categories = suggestionDto.Categories,
                        Tags = suggestionDto.Tags,
                        Title = suggestionDto.Title,
                        Cwe = suggestionDto.Cwe,
                        Text = suggestionDto.Text,
                        ExampleCommitDescriptions = suggestionDto.ExampleCommitDescriptions,
                    };

                    foreach (var exampleCommitFixes in suggestionDto.ExampleCommitFixes)
                    {
                        var suggestionFix = new SuggestionFix
                        {
                            CommitURL = exampleCommitFixes.CommitURL,
                            Lines = exampleCommitFixes.Lines
                                .Select(line => new FixLine { Line = line.Line, LineChange = line.LineChange, LineNumber = line.LineNumber }).ToList(),
                        };

                        suggestion.Fixes.Add(suggestionFix);
                    }

                    fileAnalysis.Suggestions.Add(suggestion);
                }

                analysisrResult.FileAnalyses.Add(fileAnalysis);
            }

            return analysisrResult;
        }
    }
}
