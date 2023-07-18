﻿namespace Snyk.Code.Library.Service
{
    using System;
    using System.Collections.Generic;
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
        private static readonly TimeSpan MaxScanDuration = TimeSpan.FromHours(3);
        private static readonly TimeSpan WaitBetweenRequest = TimeSpan.FromSeconds(1);

        private static readonly ILogger Logger = LogManager.ForContext<AnalysisService>();

        private ISnykCodeClient codeClient;

        public AnalysisService(ISnykCodeClient codeClient) => this.codeClient = codeClient;

        /// <inheritdoc/>
        public async Task<AnalysisResult> GetAnalysisAsync(string bundleHash,
            FireScanCodeProgressUpdate scanCodeProgressUpdate,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(bundleHash))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var analysisResultDto = await this.TryGetAnalysisDtoAsync(bundleHash, scanCodeProgressUpdate, cancellationToken);

            return this.MapDtoAnalysisResultToDomain(analysisResultDto);
        }

        /// <summary>
        /// Try get analysis DTO 'RequestAttempts' attempts.
        /// </summary>
        /// <param name="bundleHash">Source bundle id.</param>
        /// <param name="scanCodeProgressUpdate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="AnalysisResultDto"/> object.</returns>
        private async Task<AnalysisResultDto> TryGetAnalysisDtoAsync(string bundleHash,
            FireScanCodeProgressUpdate scanCodeProgressUpdate,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var startTime = DateTime.Now;
            while(DateTime.Now - startTime < MaxScanDuration)
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

                int progress = (int)analysisResultDto.Progress * 100;

                scanCodeProgressUpdate(SnykCodeScanState.Analysing, progress);

                switch (analysisResultDto.Status)
                {
                    case AnalysisStatus.Complete:
                        Logger.Information("SnykCode service return {Status} status.", analysisResultDto.Status);

                        return analysisResultDto;

                    case AnalysisStatus.Failed:
                        throw new SnykCodeException("SnykCode Analysis failed with status code " + analysisResultDto.Status);

                    case AnalysisStatus.Waiting:
                    default:
                        Logger.Information("SnykCode service return {Status} status. Sleep for 1 second timeout.", analysisResultDto.Status);
                        await Task.Delay(WaitBetweenRequest, cancellationToken);
                        break;
                }
            }

            Logger.Warning("Snyk Code scan timed out on the client side.");

            return new AnalysisResultDto { Status = AnalysisStatus.Failed, };
        }

        private AnalysisResult MapDtoAnalysisResultToDomain(AnalysisResultDto analysisResultDto)
        {
            var analysisResult = new AnalysisResult
            {
                Status = analysisResultDto.Status,
                Progress = analysisResultDto.Progress,
                URL = analysisResultDto.AnalysisURL,
            };

            if (analysisResultDto.Files == null || analysisResultDto.Suggestions == null)
            {
                return analysisResult;
            }

            foreach (var fileKeyPair in analysisResultDto.Files)
            {
                var fileAnalysis = new FileAnalysis { FileName = fileKeyPair.Key, };

                foreach (var suggestionIdToFileKeyPair in fileKeyPair.Value)
                {
                    string suggestionId = suggestionIdToFileKeyPair.Key;
                    var fileDtos = suggestionIdToFileKeyPair.Value;

                    var suggestionDto = analysisResultDto.Suggestions[suggestionId];
                    var exampleFixes = new List<SuggestionFix>();

                    foreach (var exampleCommitFixes in suggestionDto.ExampleCommitFixes)
                    {
                        var suggestionFix = new SuggestionFix
                        {
                            CommitURL = exampleCommitFixes.CommitURL,
                            Lines = exampleCommitFixes.Lines
                                .Select(line => new FixLine { Line = line.Line, LineChange = line.LineChange, LineNumber = line.LineNumber }).ToList(),
                        };

                        exampleFixes.Add(suggestionFix);
                    }

                    foreach (var fileDto in fileDtos)
                    {
                        var fileName = fileAnalysis.FileName;
                        var suggestion = new Suggestion(fileName, suggestionDto, fileDto);
                        foreach (var exampleFix in exampleFixes)
                        {
                            suggestion.Fixes.Add(exampleFix);
                        }

                        fileAnalysis.Suggestions.Add(suggestion);
                    }
                }

                analysisResult.FileAnalyses.Add(fileAnalysis);
            }

            return analysisResult;
        }
    }
}
