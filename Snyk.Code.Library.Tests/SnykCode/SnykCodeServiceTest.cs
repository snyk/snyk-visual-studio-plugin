namespace Snyk.Code.Library.Tests.Api
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Snyk.Code.Library.Domain;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Code.Library.Service;
    using Snyk.Common;
    using Xunit;

    /// <summary>
    /// Test cases for <see cref="SnykCodeService"/>.
    /// </summary>
    public class SnykCodeServiceTest
    {
        [Fact]
        public async Task SnykCodeService_CodeCacheAndFileChangesExists_UpdatePreviousScanAsync()
        {
            var bundleServiceMock = new Mock<IBundleService>();
            var filtersServiceMock = new Mock<IFiltersService>();
            var analysisServiceMock = new Mock<IAnalysisService>();
            var codeCacheServiceMock = new Mock<ICodeCacheService>();
            var dcIgnoreServiceMock = new Mock<IDcIgnoreService>();
            var fileProviderMock = new Mock<IFileProvider>();

            var snykCodeService = new SnykCodeService(
                bundleServiceMock.Object,
                analysisServiceMock.Object,
                filtersServiceMock.Object,
                codeCacheServiceMock.Object,
                dcIgnoreServiceMock.Object);

            var addedFiles = new List<string>
            {
                "C:\\Project\\Test\\Main.cs",
                "C:\\Project\\Test\\DbService.cs",
            };

            fileProviderMock
                .Setup(fileProvider => fileProvider.GetAddedFiles())
                .Returns(addedFiles);

            var changedFiles = new List<string> { "C:\\Project\\Test\\Window.cs", };

            fileProviderMock
                .Setup(fileProvider => fileProvider.GetChangedFiles())
                .Returns(changedFiles);

            fileProviderMock
                .Setup(fileProvider => fileProvider.GetAllChangedFiles())
                .Returns(changedFiles);

            var removedFiles = new List<string> { "C:\\Project\\Test\\DummyWindow.cs", };

            fileProviderMock
                .Setup(fileProvider => fileProvider.GetRemovedFiles())
                .Returns(removedFiles);

            fileProviderMock
                .Setup(fileProvider => fileProvider.ClearHistory());

            codeCacheServiceMock
                .Setup(analysisService => analysisService.CacheValid())
                .Returns(false);

            codeCacheServiceMock
                .Setup(analysisService => analysisService.CacheNotExists())
                .Returns(false);

            string bundleId = "testBundleId";

            codeCacheServiceMock
                .Setup(codeCacheService => codeCacheService.GetCachedBundleId())
                .Returns(bundleId);

            var extendFilePathToHashDict = new Dictionary<string, string>()
            {
                { "/Main.cs", "Hash1" },
                { "/DbService.cs", "Hash2" },
                { "/Window.cs", "Hash3" },
            };

            codeCacheServiceMock
                .Setup(codeCacheService => codeCacheService.GetFilePathToHashDictionary(It.IsAny<List<string>>()))
                .Returns(extendFilePathToHashDict);

            var analysisResults = new AnalysisResult
            {
                Status = AnalysisStatus.Done,
                Progress = 1,
                FileAnalyses = new List<FileAnalysis>
                {
                    new FileAnalysis
                    {
                        FileName = "Main.cs",
                    },
                    new FileAnalysis
                    {
                        FileName = "DbService.cs",
                    },
                    new FileAnalysis
                    {
                        FileName = "Window.cs",
                    },
                },
            };

            codeCacheServiceMock
                .Setup(codeCacheService => codeCacheService.SetAnalysisResult(analysisResults));

            codeCacheServiceMock
                .Setup(codeCacheService => codeCacheService.SetCachedBundleId(bundleId));

            filtersServiceMock
                .Setup(filtersService => filtersService.FilterFilesAsync(changedFiles).Result)
                .Returns(changedFiles);

            dcIgnoreServiceMock
                .Setup(dcIgnoreService => dcIgnoreService.FilterFiles(It.IsAny<string>(), changedFiles))
                .Returns(changedFiles);

            var extendedBundle = new Bundle { Id = bundleId, };

            bundleServiceMock
                .Setup(bundleService => bundleService.ExtendBundleAsync(
                    bundleId,
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()).Result)
                .Returns(extendedBundle);

            bundleServiceMock
                .Setup(bundleService => bundleService.UploadMissingFilesAsync(extendedBundle, codeCacheServiceMock.Object, It.IsAny<CancellationToken>()));

            analysisServiceMock
                .Setup(analysisService => analysisService.GetAnalysisAsync(extendedBundle.Id, It.IsAny<CancellationToken>()).Result)
                .Returns(analysisResults);

            var analysisResult = await snykCodeService.ScanAsync(fileProviderMock.Object);

            Assert.NotNull(analysisResult);
            Assert.Equal(3, analysisResult.FileAnalyses.Count);
        }

        [Fact]
        public async Task SnykCodeService_CodeCacheExists_ReturnWithoutRemoteQueryAsync()
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

            var filtersServiceMock = new Mock<IFiltersService>();
            var bundleServiceMock = new Mock<IBundleService>();
            var analysisServiceMock = new Mock<IAnalysisService>();

            var codeCacheServiceMock = new Mock<ICodeCacheService>();
            var dcIgnoreServiceMock = new Mock<IDcIgnoreService>();

            codeCacheServiceMock
                .Setup(analysisService => analysisService.GetCachedAnalysisResult())
                .Returns(analysisResults);

            codeCacheServiceMock
                .Setup(analysisService => analysisService.CacheValid())
                .Returns(true);

            var snykCodeService = new SnykCodeService(
                bundleServiceMock.Object,
                analysisServiceMock.Object,
                filtersServiceMock.Object,
                codeCacheServiceMock.Object,
                dcIgnoreServiceMock.Object);

            var solutionServiceMock = new Mock<ISolutionService>();

            solutionServiceMock
                .Setup(solutionService => solutionService.GetPath())
                .Returns(TestResource.GetResourcesPath());

            var fileProvider = new SnykCodeFileProvider(solutionServiceMock.Object);

            var analysisResult = await snykCodeService.ScanAsync(fileProvider);

            Assert.NotNull(analysisResult);

            analysisServiceMock
                .Verify(analysisService => analysisService.GetAnalysisAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(0));

            codeCacheServiceMock
                .Verify(codeCacheService => codeCacheService.GetCachedAnalysisResult(), Times.Exactly(1));
        }

        [Fact]
        public void SnykCodeService_JsonErrorProvided_GetSnykCodeErrorMessageReturnClearMessageWithoutJson()
        {
            var bundleServiceMock = new Mock<IBundleService>();
            var filtersServiceMock = new Mock<IFiltersService>();
            var analysisServiceMock = new Mock<IAnalysisService>();
            var codeCacheServiceMock = new Mock<ICodeCacheService>();
            var dcIgnoreServiceMock = new Mock<IDcIgnoreService>();

            var snykCodeService = new SnykCodeService(
                bundleServiceMock.Object,
                analysisServiceMock.Object,
                filtersServiceMock.Object,
                codeCacheServiceMock.Object,
                dcIgnoreServiceMock.Object);

            string error = snykCodeService.GetSnykCodeErrorMessage(new Exception("{\"code\": 401, \"message\": \"Not authorised\"}"));

            Assert.NotNull(error);
        }

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

            string bundleId = "dummyId";

            var bundleServiceMock = new Mock<IBundleService>();

            var bundle = new Bundle
            {
                Id = bundleId,
                MissingFiles = new string[] { filePath1, filePath2 },
            };

            bundleServiceMock
                .Setup(bundleService => bundleService.CreateBundleAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()).Result)
                .Returns(bundle);

            bundleServiceMock
                .Setup(bundleService => bundleService.UploadFilesAsync(bundleId, It.IsAny<IDictionary<string, string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()).Result)
                .Returns(true);

            bundleServiceMock
                .Setup(bundleService => bundleService.CheckBundleAsync(bundleId, It.IsAny<CancellationToken>()).Result)
                .Callback<string, CancellationToken>((id, cancellationToken) => bundle.MissingFiles = new string[] { })
                .Returns(bundle);

            var analysisServiceMock = new Mock<IAnalysisService>();

            analysisServiceMock
                .Setup(analysisService => analysisService.GetAnalysisAsync(bundleId, It.IsAny<CancellationToken>()).Result)
                .Returns(analysisResults);

            var codeCacheServiceMock = new Mock<ICodeCacheService>();

            codeCacheServiceMock
                .Setup(analysisService => analysisService.GetCachedAnalysisResult())
                .Returns((AnalysisResult)null);

            codeCacheServiceMock
                .Setup(analysisService => analysisService.CacheNotExists())
                .Returns(true);

            var dcIgnoreServiceMock = new Mock<IDcIgnoreService>();

            var snykCodeService = new SnykCodeService(
                bundleServiceMock.Object,
                analysisServiceMock.Object,
                filtersServiceMock.Object,
                codeCacheServiceMock.Object,
                dcIgnoreServiceMock.Object);

            var fileProviderMock = new Mock<IFileProvider>();

            codeCacheServiceMock
                .Setup(analysisService => analysisService.GetCachedAnalysisResult())
                .Returns((AnalysisResult)null);

            var analysisResult = await snykCodeService.ScanAsync(fileProviderMock.Object);

            Assert.NotNull(analysisResult);
            Assert.Equal(2, analysisResult.FileAnalyses.Count);

            Assert.Equal("app1.js", analysisResult.FileAnalyses[0].FileName);
            Assert.Equal("app2.js", analysisResult.FileAnalyses[1].FileName);

            bundleServiceMock
                .Verify(bundleService => bundleService.CreateBundleAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

            bundleServiceMock
                .Verify(bundleService => bundleService.UploadMissingFilesAsync(It.IsAny<Bundle>(), It.IsAny<ICodeCacheService>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

            analysisServiceMock
                .Verify(analysisService => analysisService.GetAnalysisAsync(bundleId, It.IsAny<CancellationToken>()), Times.Exactly(1));
        }
    }
}
