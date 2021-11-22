namespace Snyk.Code.Library.Service
{
    using System;
    using System.Linq;
    using System.Threading;
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
        public async Task<AnalysisResult> GetAnalysisAsync(string bundleId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var analysisResultDto = await this.TryGetAnalysisDtoAsync(bundleId, cancellationToken);

            return this.MapDtoAnalysisResultToDomain(analysisResultDto);
        }

        /// <summary>
        /// Try get analysis DTO 'RequestAttempts' attempts.
        /// </summary>
        /// <param name="bundleId">Source bundle id.</param>
        /// <returns><see cref="AnalysisResultDto"/> object.</returns>
        private async Task<AnalysisResultDto> TryGetAnalysisDtoAsync(string bundleId, CancellationToken cancellationToken = default)
        {
            Logger.Information("Try get analysis DTO object {RequestAttempts} times.", RequestAttempts);

            cancellationToken.ThrowIfCancellationRequested();

            for (int counter = 0; counter < RequestAttempts; counter++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                AnalysisResultDto analysisResultDto = await this.codeClient.GetAnalysisAsync(bundleId, cancellationToken);

                switch (analysisResultDto.Status)
                {
                    case AnalysisStatus.COMPLETE:
                        return analysisResultDto;

                    case AnalysisStatus.Failed:
                        throw new SnykCodeException("SnykCode Analysis failed.");

                    case AnalysisStatus.Waiting:
                    default:
                        Logger.Information("SnykCode service return {Status} status. Sleep for {RequestTimeout} timeout.", analysisResultDto.Status, RequestTimeout);

                        Thread.Sleep(RequestTimeout);
                        break;
                }
            }

            Logger.Warning("Can't Get analysis after {RequestAttempts} attepts. Return AnalysisResultDto with Failed status.", RequestAttempts);

            return new AnalysisResultDto { Status = AnalysisStatus.Failed, };
        }

        private AnalysisResult MapDtoAnalysisResultToDomain(AnalysisResultDto analysisResultDto)
        {
            var analysisrResult = new AnalysisResult
            {
                Status = analysisResultDto.Status,
                Progress = analysisResultDto.Progress,
                URL = analysisResultDto.AnalysisURL,
            };

            if (analysisResultDto == null)
            {
                return analysisrResult;
            }

            if (analysisResultDto.Files == null || analysisResultDto.Suggestions == null)
            {
                return analysisrResult;
            }

            foreach (var fileKeyPair in analysisResultDto.Files)
            {
                var fileAnalysis = new FileAnalysis { FileName = fileKeyPair.Key, };

                foreach (var suggestionIdToFileKeyPair in fileKeyPair.Value)
                {
                    string suggestionId = suggestionIdToFileKeyPair.Key;
                    var fileDtos = suggestionIdToFileKeyPair.Value;

                    var suggestionDto = analysisResultDto.Suggestions[suggestionId];

                    var fileDto = fileDtos.First();

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
                        FileName = fileAnalysis.FileName,
                        RepoDatasetSize = suggestionDto.RepoDatasetSize,
                        ExampleCommitDescriptions = suggestionDto.ExampleCommitDescriptions,
                        Rows = Tuple.Create(fileDto.Rows[0], fileDto.Rows[1]),
                        Columns = Tuple.Create(fileDto.Cols[0], fileDto.Cols[1]),
                        Markers = fileDto.Markers.Select(markerDto => new Marker
                        {
                            MessageIndexes = markerDto.MessageIndexes,
                            Positions = markerDto.Positions.Select(positionDto => new Position
                            {
                                Columns = positionDto.Cols,
                                Rows = positionDto.Rows,
                                FileName = positionDto.File,
                            }).ToList(),
                        }).ToList(),
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
