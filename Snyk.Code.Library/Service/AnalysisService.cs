namespace Snyk.Code.Library.Service
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto.Analysis;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Common;

    /// <inheritdoc/>
    public class AnalysisService : IAnalysisService
    {
        private const int RequestTimeout = 100;

        private const int RequestAttempts = 100;

        private static readonly ILogger Logger = LogManager.ForContext<AnalysisService>();

        private ISnykCodeClient codeClient;

        public AnalysisService(ISnykCodeClient codeClient) => this.codeClient = codeClient;

        /// <inheritdoc/>
        public async Task<AnalysisResult> GetAnalysisAsync(string bundleId)
        {
            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            var analysisResultDto = await this.TryGetAnalysisDtoAsync(bundleId);

            return this.MapDtoAnalysisResultToDomain(analysisResultDto);
        }

        /// <summary>
        /// Try get analysis DTO 'RequestAttempts' attempts.
        /// </summary>
        /// <param name="bundleId">Source bundle id.</param>
        /// <returns><see cref="AnalysisResultDto"/> object.</returns>
        private async Task<AnalysisResultDto> TryGetAnalysisDtoAsync(string bundleId)
        {
            Logger.Debug("Enter TryGetAnalysisDtoAsync()");

            for (int counter = 0; counter < RequestAttempts; counter++)
            {
                AnalysisResultDto analysisResultDto = await this.codeClient.GetAnalysisAsync(bundleId);

                switch (analysisResultDto.Status)
                {
                    case AnalysisStatus.Done:
                        return analysisResultDto;

                    case AnalysisStatus.Failed:
                        Logger.Warning("Analysis has failed.");

                        throw new SnykCodeException("SnykCode Analysis failed.");

                    case AnalysisStatus.Waiting:
                    default:
                        Logger.Warning("SnykCodeClient.GetAnalysisAsync() return {Status}. Sleep for {RequestTimeout}", analysisResultDto.Status, RequestTimeout);

                        System.Threading.Thread.Sleep(RequestTimeout);
                        break;
                }
            }

            Logger.Warning("Can't Get Analysis after few attepts. Return AnalysisResultDto with Failed status.");

            return new AnalysisResultDto { Status = AnalysisStatus.Failed, };
        }

        private AnalysisResult MapDtoAnalysisResultToDomain(AnalysisResultDto analysisResultDto)
        {
            Logger.Debug("Start map DTO AnalysisResultDto object to Domain AnalysisResult object.");

            var analysisrResult = new AnalysisResult
            {
                Status = analysisResultDto.Status,
                Progress = analysisResultDto.Progress,
                URL = analysisResultDto.AnalysisURL,
            };

            var analysisResultsDto = analysisResultDto.AnalysisResults;

            if (analysisResultsDto == null)
            {
                return analysisrResult;
            }

            foreach (var fileKeyPair in analysisResultsDto.Files)
            {
                var fileAnalysis = new FileAnalysis { FileName = fileKeyPair.Key, };

                foreach (var suggestionIdToFileKeyPair in fileKeyPair.Value)
                {
                    string suggestionId = suggestionIdToFileKeyPair.Key;
                    var fileDtos = suggestionIdToFileKeyPair.Value;

                    var suggestionDto = analysisResultsDto.Suggestions[suggestionId];

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
                        Rows = Tuple.Create(fileDtos.First().Rows[0], fileDtos.First().Rows[1]),
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
