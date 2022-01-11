﻿namespace Snyk.Code.Library.Service
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto.Analysis;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Common;
    using static Snyk.Code.Library.Service.SnykCodeService;

    /// <inheritdoc/>
    public class AnalysisService : IAnalysisService
    {
        public const int RequestAttempts = 900;

        private const int RequestTimeout = 1000;

        private static readonly ILogger Logger = LogManager.ForContext<AnalysisService>();

        private ISnykCodeClient codeClient;

        public AnalysisService(ISnykCodeClient codeClient) => this.codeClient = codeClient;

        /// <inheritdoc/>
        public async Task<AnalysisResult> GetAnalysisAsync(
            string bundleHash,
            FireScanCodeProgressUpdate scanCodeProgressUpdate,
            int requestAttempts = RequestAttempts,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(bundleHash))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var analysisResultDto = await this.TryGetAnalysisDtoAsync(bundleHash, scanCodeProgressUpdate, requestAttempts, cancellationToken);

            return this.MapDtoAnalysisResultToDomain(analysisResultDto);
        }

        /// <summary>
        /// Try get analysis DTO 'RequestAttempts' attempts.
        /// </summary>
        /// <param name="bundleHash">Source bundle id.</param>
        /// <returns><see cref="AnalysisResultDto"/> object.</returns>
        private async Task<AnalysisResultDto> TryGetAnalysisDtoAsync(
            string bundleHash,
            FireScanCodeProgressUpdate scanCodeProgressUpdate,
            int requestAttempts,
            CancellationToken cancellationToken = default)
        {
            Logger.Debug("Try get analysis DTO object {RequestAttempts} times.", RequestAttempts);

            cancellationToken.ThrowIfCancellationRequested();

            for (int counter = 0; counter < requestAttempts; counter++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                AnalysisResultDto analysisResultDto;

                try
                {
                    analysisResultDto = await this.codeClient.GetAnalysisAsync(bundleHash, cancellationToken);
                }
                catch (HttpRequestException e)
                {
                    // This catch handle possible network connection issues.
                    // If some network connection problem appear this catch just log it and method
                    // try to get analysis results on next iteration.
                    Logger.Error(e, "Error on try to get analysis.");

                    analysisResultDto = new AnalysisResultDto
                    {
                        Status = AnalysisStatus.Waiting,
                    };
                }

                Logger.Debug($"Request analysis status {analysisResultDto.Status}");

                int progress = (int)analysisResultDto.Progress * 100;

                scanCodeProgressUpdate(SnykCodeScanState.Analysing, progress);

                switch (analysisResultDto.Status)
                {
                    case AnalysisStatus.Complete:
                        Logger.Information("SnykCode service return {Status} status.", analysisResultDto.Status);

                        return analysisResultDto;

                    case AnalysisStatus.Failed:
                        throw new SnykCodeException("SnykCode Analysis failed.");

                    case AnalysisStatus.Waiting:
                    default:
                        Logger.Information("SnykCode service return {Status} status. Sleep for 1 second timeout.", analysisResultDto.Status);

                        Thread.Sleep(RequestTimeout);
                        break;
                }
            }

            Logger.Warning("Can't Get analysis after {requestAttempts} attepts. Return AnalysisResultDto with Failed status.", requestAttempts);

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
