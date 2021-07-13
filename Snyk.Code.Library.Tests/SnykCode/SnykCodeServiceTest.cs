namespace Snyk.Code.Library.Tests.Api
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Moq;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto;
    using Snyk.Code.Library.Api.Dto.Analysis;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Code.Library.Service.Impl;
    using Xunit;

    /// <summary>
    /// Test cases for <see cref="SnykCodeService"/>.
    /// </summary>
    public class SnykCodeServiceTest
    {
        [Fact]
        public async Task SnykCodeService_TwoFilesWithIssuesProvided_ScanSuccessAsync()
        {
            var analysisResultsDto = new AnalysisResultsDto();

            analysisResultsDto.Files = new Dictionary<string, SuggestionIdToFileDto>
            {
                ["app1.js"] = new SuggestionIdToFileDto
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
                                            File = "app1.js",
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
                                            File = "app1.js",
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
                ["app2.js"] = new SuggestionIdToFileDto
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
                                            File = "app2.js",
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            analysisResultsDto.Suggestions = new Dictionary<string, SuggestionDto>
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
                Status = AnalysisStatus.Done,
                AnalysisResults = analysisResultsDto,
            };

            var codeClientMock = new Mock<ISnykCodeClient>();

            codeClientMock
                .Setup(codeClient => codeClient.GetFiltersAsync().Result)
                .Returns(new FiltersDto 
                {
                    Extensions = new List<string> { ".js", },
                    ConfigFiles = new List<string>(),
                });

            codeClientMock
                .Setup(codeClient => codeClient.GetAnalysisAsync(dummyBundleId).Result)
                .Returns(dummyAnalysisResultDto);

            string filePath1 = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "app1.js");
            string filePath2 = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "app2.js");

            var bundleDto = new BundleResponseDto
            {
                Id = dummyBundleId,
                MissingFiles = new string[] { filePath1, filePath2 },
            };

            codeClientMock
                .Setup(codeClient => codeClient.CreateBundleAsync(It.IsAny<Dictionary<string, string>>()).Result)
                .Returns(bundleDto);

            codeClientMock
                .Setup(codeClient => codeClient.UploadFilesAsync(dummyBundleId, It.IsAny<IEnumerable<CodeFileDto>>()).Result)
                .Returns(true);

            codeClientMock
                .Setup(codeClient => codeClient.CheckBundleAsync(dummyBundleId).Result)
                .Callback<string>((bundleId) => bundleDto.MissingFiles = new string[] { })
                .Returns(bundleDto);

            var snykCodeService = new SnykCodeService(codeClientMock.Object);

            var analysisResult = await snykCodeService.ScanAsync(new List<string> { filePath1, filePath2 });

            Assert.NotNull(analysisResult);
            Assert.Equal(2, analysisResult.FileAnalyses.Count);

            Assert.Equal(2, analysisResult.FileAnalyses[0].Suggestions.Count);
            Assert.Equal("app1.js", analysisResult.FileAnalyses[0].FileName);

            Assert.Single(analysisResult.FileAnalyses[1].Suggestions);
            Assert.Equal("app2.js", analysisResult.FileAnalyses[1].FileName);

            codeClientMock
                .Verify(codeClient => codeClient.GetAnalysisAsync(dummyBundleId), Times.Exactly(1));
        }

        [Fact]
        public async Task SnykCodeService_OneMissingFileAfterUploadProvided_ScanSuccessAsync()
        {
            var analysisResultsDto = new AnalysisResultsDto();

            analysisResultsDto.Files = new Dictionary<string, SuggestionIdToFileDto>
            {
                ["app1.js"] = new SuggestionIdToFileDto
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
                                            File = "app1.js",
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
                                            File = "app1.js",
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
                ["app2.js"] = new SuggestionIdToFileDto
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
                                            File = "app2.js",
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            analysisResultsDto.Suggestions = new Dictionary<string, SuggestionDto>
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
                Status = AnalysisStatus.Done,
                AnalysisResults = analysisResultsDto,
            };

            var codeClientMock = new Mock<ISnykCodeClient>();

            codeClientMock
                .Setup(codeClient => codeClient.GetFiltersAsync().Result)
                .Returns(new FiltersDto
                {
                    Extensions = new List<string> { ".js", },
                    ConfigFiles = new List<string>(),
                });

            codeClientMock
                .Setup(codeClient => codeClient.GetAnalysisAsync(dummyBundleId).Result)
                .Returns(dummyAnalysisResultDto);

            string filePath1 = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "app1.js");
            string filePath2 = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "app2.js");

            var bundleDto = new BundleResponseDto
            {
                Id = dummyBundleId,
                MissingFiles = new string[] { filePath1, filePath2 },
            };

            codeClientMock
                .Setup(codeClient => codeClient.CreateBundleAsync(It.IsAny<Dictionary<string, string>>()).Result)
                .Returns(bundleDto);

            codeClientMock
                .Setup(codeClient => codeClient.UploadFilesAsync(dummyBundleId, It.IsAny<IEnumerable<CodeFileDto>>()).Result)
                .Returns(true);

            codeClientMock
                .Setup(codeClient => codeClient.CheckBundleAsync(dummyBundleId).Result)
                .Callback<string>((bundleId) => bundleDto.MissingFiles = new string[] { filePath1 })
                .Returns(bundleDto);

            var snykCodeService = new SnykCodeService(codeClientMock.Object);

            var analysisResult = await snykCodeService.ScanAsync(new List<string> { filePath1, filePath2 });

            Assert.NotNull(analysisResult);
            Assert.Equal(2, analysisResult.FileAnalyses.Count);

            Assert.Equal(2, analysisResult.FileAnalyses[0].Suggestions.Count);
            Assert.Equal("app1.js", analysisResult.FileAnalyses[0].FileName);

            Assert.Single(analysisResult.FileAnalyses[1].Suggestions);
            Assert.Equal("app2.js", analysisResult.FileAnalyses[1].FileName);

            codeClientMock
                .Verify(codeClient => codeClient.GetFiltersAsync(), Times.Exactly(1));

            codeClientMock
                .Verify(codeClient => codeClient.CreateBundleAsync(It.IsAny<IDictionary<string, string>>()), Times.Exactly(1));

            codeClientMock
                .Verify(codeClient => codeClient.UploadFilesAsync(bundleDto.Id, It.IsAny<IEnumerable<CodeFileDto>>()), Times.Exactly(2));

            codeClientMock
                .Verify(codeClient => codeClient.CheckBundleAsync(bundleDto.Id), Times.Exactly(1));

            codeClientMock
                .Verify(codeClient => codeClient.GetAnalysisAsync(dummyBundleId), Times.Exactly(1));
        }
    }
}
