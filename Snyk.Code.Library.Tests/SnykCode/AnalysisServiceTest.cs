namespace Snyk.Code.Library.Tests.SnykCode
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto.Analysis;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Code.Library.Service;
    using Xunit;
    using static Snyk.Code.Library.Service.SnykCodeService;

    public class AnalysisServiceTest
    {
        private FireScanCodeProgressUpdate scanCodeProgressUpdate = (state, progress) => { };

        [Fact]
        public void AnalysisService_FailedStatusProvided_GetAnalysisThrowException()
        {
            var codeClientMock = new Mock<ISnykCodeClient>();

            var analysisService = new AnalysisService(codeClientMock.Object);

            var analysisResultsDto = new AnalysisResultsDto();

            string dummyBundleId = "dummyId";

            var dummyAnalysisResultDto = new AnalysisResultDto
            {
                Status = AnalysisStatus.Failed,
            };

            codeClientMock
                .Setup(codeClient => codeClient.GetAnalysisAsync(dummyBundleId, It.IsAny<CancellationToken>()).Result)
                .Returns(dummyAnalysisResultDto);

            _ = Assert.ThrowsAsync<AggregateException>(() => analysisService.GetAnalysisAsync(dummyBundleId, scanCodeProgressUpdate));

            codeClientMock
                .Verify(codeClient => codeClient.GetAnalysisAsync(dummyBundleId, It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public async Task AnalysisService_WaitingStatusProvided_GetAnalysisSuccessInTwoAttemptsAsync()
        {
            var codeClientMock = new Mock<ISnykCodeClient>();

            var analysisService = new AnalysisService(codeClientMock.Object);

            var analysisResultsDto = new AnalysisResultsDto();

            string dummyBundleId = "dummyId";

            var dummyAnalysisResultDto = new AnalysisResultDto
            {
                Status = AnalysisStatus.Waiting,
            };

            var mockMethodCallsCount = 0;

            codeClientMock
                .Setup(codeClient => codeClient.GetAnalysisAsync(dummyBundleId, It.IsAny<CancellationToken>()).Result)
                .Returns(dummyAnalysisResultDto)
                .Callback<string, CancellationToken>((str, cancellationToken) =>
                {
                    mockMethodCallsCount++;

                    if (mockMethodCallsCount > 23)
                    {
                        dummyAnalysisResultDto.Status = AnalysisStatus.Complete;
                    }
                });

            var analysisResult = await analysisService.GetAnalysisAsync(dummyBundleId, this.scanCodeProgressUpdate);

            Assert.NotNull(analysisResult);
            Assert.Equal(AnalysisStatus.Complete, analysisResult.Status);

            codeClientMock
                .Verify(codeClient => codeClient.GetAnalysisAsync(dummyBundleId, It.IsAny<CancellationToken>()), Times.Exactly(24));
        }

        [Fact]
        public async Task AnalysisService_InfiniteWaitingStatusProvided_GetAnalysisReturnFailedStatusAsync()
        {
            var codeClientMock = new Mock<ISnykCodeClient>();

            var analysisService = new AnalysisService(codeClientMock.Object);

            var analysisResultsDto = new AnalysisResultsDto();

            string dummyBundleId = "dummyId";

            var dummyAnalysisResultDto = new AnalysisResultDto
            {
                Status = AnalysisStatus.Waiting,
            };

            codeClientMock
                .Setup(codeClient => codeClient.GetAnalysisAsync(dummyBundleId, It.IsAny<CancellationToken>()).Result)
                .Returns(dummyAnalysisResultDto);

            var analysisResult = await analysisService.GetAnalysisAsync(dummyBundleId, this.scanCodeProgressUpdate, requestAttempts: 5);

            Assert.NotNull(analysisResult);
            Assert.Equal(AnalysisStatus.Failed, analysisResult.Status);

            codeClientMock
                .Verify(codeClient => codeClient.GetAnalysisAsync(dummyBundleId, It.IsAny<CancellationToken>()), Times.Exactly(5));
        }

        [Fact]
        public async Task AnalysisService_TwoFilesWithIssuesProvided_GetAnalysisSuccessAsync()
        {
            var codeClientMock = new Mock<ISnykCodeClient>();

            var analysisService = new AnalysisService(codeClientMock.Object);

            var analysisResultsDto = new AnalysisResultsDto();

            var files = new Dictionary<string, SuggestionIdToFileDto>
            {
                ["app.js"] = new SuggestionIdToFileDto
                {
                    ["0"] = new List<FileDto>
                    {
                        new FileDto
                        {
                            Rows = new int[] { 10, 10 },
                            Cols = new int[] { 5, 5 },
                            Markers = new List<MarkerDto>
                            {
                                new MarkerDto
                                {
                                    MessageIndexes = new List<long> { 10, 20 },
                                    Positions = new List<PositionDto>
                                    {
                                        new PositionDto
                                        {
                                            Rows = new List<long> { 40, 50 },
                                            Cols = new List<long> { 5, 10 },
                                            File = "app.js",
                                        },
                                    },
                                },
                            },
                        },
                    },
                    ["1"] = new List<FileDto>
                    {
                        new FileDto
                        {
                            Rows = new int[] { 10, 10 },
                            Cols = new int[] { 5, 5 },
                            Markers = new List<MarkerDto>
                            {
                                new MarkerDto
                                {
                                    MessageIndexes = new List<long> { 10, 20 },
                                    Positions = new List<PositionDto>
                                    {
                                        new PositionDto
                                        {
                                            Rows = new List<long> { 40, 50 },
                                            Cols = new List<long> { 5, 10 },
                                            File = "app.js",
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
                ["db.js"] = new SuggestionIdToFileDto
                {
                    ["2"] = new List<FileDto>
                    {
                        new FileDto
                        {
                            Rows = new int[] { 10, 10 },
                            Cols = new int[] { 5, 5 },
                            Markers = new List<MarkerDto>
                            {
                                new MarkerDto
                                {
                                    MessageIndexes = new List<long> { 10, 20 },
                                    Positions = new List<PositionDto>
                                    {
                                        new PositionDto
                                        {
                                            Rows = new List<long> { 40, 50 },
                                            Cols = new List<long> { 5, 10 },
                                            File = "db.js",
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            var suggestions = new Dictionary<string, SuggestionDto>
            {
                ["0"] = new SuggestionDto
                {
                    Id = "TestSuggestioinId0",
                    Title = "TestSuggestioinId",
                    Text = "Text TestSuggestioin",
                    ExampleCommitFixes = new List<ExampleCommitFixDto>
                    {
                        new ExampleCommitFixDto
                        {
                            CommitURL = "http://github.com/",
                            Lines = new List<LineDto>
                            {
                                new LineDto
                                {
                                    Line = "test1",
                                    LineChange = "test 2",
                                    LineNumber = 10,
                                },
                            },
                        },
                    },
                },
                ["1"] = new SuggestionDto
                {
                    Id = "TestSuggestioinId1",
                    Title = "TestSuggestioinId 1",
                    Text = "Text TestSuggestioin 1",
                    ExampleCommitFixes = new List<ExampleCommitFixDto>
                    {
                        new ExampleCommitFixDto
                        {
                            CommitURL = "http://github.com/",
                            Lines = new List<LineDto>
                            {
                                new LineDto
                                {
                                    Line = "test1",
                                    LineChange = "test 2",
                                    LineNumber = 10,
                                },
                            },
                        },
                    },
                },
                ["2"] = new SuggestionDto
                {
                    Id = "TestSuggestioinId2",
                    Title = "TestSuggestioinId 2",
                    Text = "Text TestSuggestioin 2",
                    ExampleCommitFixes = new List<ExampleCommitFixDto>
                    {
                        new ExampleCommitFixDto
                        {
                            CommitURL = "http://github.com/",
                            Lines = new List<LineDto>
                            {
                                new LineDto
                                {
                                    Line = "test1",
                                    LineChange = "test 2",
                                    LineNumber = 10,
                                },
                            },
                        },
                    },
                },
            };

            string dummyBundleId = "dummyId";

            var dummyAnalysisResultDto = new AnalysisResultDto
            {
                Status = AnalysisStatus.Complete,
                Suggestions = suggestions,
                Files = files,
            };

            codeClientMock
                .Setup(codeClient => codeClient.GetAnalysisAsync(dummyBundleId, It.IsAny<CancellationToken>()).Result)
                .Returns(dummyAnalysisResultDto);

            var analysisResult = await analysisService.GetAnalysisAsync(dummyBundleId, this.scanCodeProgressUpdate);

            Assert.NotNull(analysisResult);
            Assert.Equal(2, analysisResult.FileAnalyses.Count);

            Assert.Equal(2, analysisResult.FileAnalyses[0].Suggestions.Count);
            Assert.Equal("app.js", analysisResult.FileAnalyses[0].FileName);

            Assert.Single(analysisResult.FileAnalyses[1].Suggestions);
            Assert.Equal("db.js", analysisResult.FileAnalyses[1].FileName);

            codeClientMock
                .Verify(codeClient => codeClient.GetAnalysisAsync(dummyBundleId, It.IsAny<CancellationToken>()), Times.Exactly(1));
        }
    }
}
