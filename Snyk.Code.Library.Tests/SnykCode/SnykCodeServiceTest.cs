namespace Snyk.Code.Library.Tests.Api
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Moq;
    using Snyk.Code.Library.Domain;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Code.Library.Service;
    using Xunit;

    /// <summary>
    /// Test cases for <see cref="SnykCodeService"/>.
    /// </summary>
    public class SnykCodeServiceTest
    {
        [Fact]
        public async Task SnykCodeService_TwoFilesWithIssuesProvided_ScanSuccessAsync()
        {
            var analysisResults = new AnalysisResult
            {
                Status = AnalysisStatus.Done,
                Progress = 1,
                FileAnalyses = new List<FileAnalysis>
                {
                    new FileAnalysis
                    {
                        FileName = "app1.js",
                    },
                    new FileAnalysis
                    {
                        FileName = "app2.js",
                    },
                },
            };

            string filePath1 = TestResource.GetFileFullPath("app1.js");
            string filePath2 = TestResource.GetFileFullPath("app2.js");

            var filePaths = new List<string> { filePath1, filePath2 };

            var filtersServiceMock = new Mock<IFiltersService>();

            filtersServiceMock
                .Setup(filtersService => filtersService.FilterFilesAsync(filePaths).Result)
                .Returns(filePaths);

            string bundleId = "dummyId";

            var bundleServiceMock = new Mock<IBundleService>();

            var bundle = new Bundle
            {
                Id = bundleId,
                MissingFiles = new string[] { filePath1, filePath2 },
            };

            bundleServiceMock
                .Setup(bundleService => bundleService.CreateBundleAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<int>()).Result)
                .Returns(bundle);

            bundleServiceMock
                .Setup(bundleService => bundleService.UploadFilesAsync(bundleId, It.IsAny<IDictionary<string, string>>(), It.IsAny<int>()).Result)
                .Returns(true);

            bundleServiceMock
                .Setup(bundleService => bundleService.CheckBundleAsync(bundleId).Result)
                .Callback<string>((id) => bundle.MissingFiles = new string[] { })
                .Returns(bundle);

            var analysisServiceMock = new Mock<IAnalysisService>();

            analysisServiceMock
                .Setup(analysisService => analysisService.GetAnalysisAsync(bundleId).Result)
                .Returns(analysisResults);

            var snykCodeService = new SnykCodeService(bundleServiceMock.Object, analysisServiceMock.Object, filtersServiceMock.Object);

            var analysisResult = await snykCodeService.ScanAsync(new List<string> { filePath1, filePath2 });

            Assert.NotNull(analysisResult);
            Assert.Equal(2, analysisResult.FileAnalyses.Count);

            Assert.Equal("app1.js", analysisResult.FileAnalyses[0].FileName);
            Assert.Equal("app2.js", analysisResult.FileAnalyses[1].FileName);

            filtersServiceMock
                .Verify(filterService => filterService.FilterFilesAsync(filePaths), Times.Exactly(1));

            bundleServiceMock
                .Verify(bundleService => bundleService.CreateBundleAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<int>()), Times.Exactly(1));

            bundleServiceMock
                .Verify(bundleService => bundleService.UploadMissingFilesAsync(It.IsAny<Bundle>()), Times.Exactly(1));

            analysisServiceMock
                .Verify(analysisService => analysisService.GetAnalysisAsync(bundleId), Times.Exactly(1));
        }
    }
}
