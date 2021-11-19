namespace Snyk.Code.Library.Tests.Api
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto;
    using Snyk.Code.Library.Domain;
    using Snyk.Code.Library.Service;
    using Snyk.Common;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="SnykCodeService"/>.
    /// </summary>
    public class BundleServiceTest
    {
        [Fact]
        public async Task BundleService_TwoFilesProvided_UploadSuccessfullAfterTreeAttemptsAsync()
        {
            string filePath1 = TestResource.GetFileFullPath("app1.js");
            string filePath2 = TestResource.GetFileFullPath("app2.js");

            var codeClientMock = new Mock<ISnykCodeClient>();

            var bundle = new Bundle
            {
                Id = "testId",
                MissingFiles = new string[] { filePath1, filePath2 },
            };

            var bundleDto = new BundleResponseDto
            {
                Id = bundle.Id,
                MissingFiles = new string[] { filePath1, filePath2 },
            };

            codeClientMock
                .Setup(codeClient => codeClient.CheckBundleAsync(bundle.Id, It.IsAny<CancellationToken>()).Result)
                .Returns(bundleDto);

            string fileContent1 = TestResource.GetFileContent("app1.js");
            string fileContent2 = TestResource.GetFileContent("app2.js");

            string fileHash1 = Sha256.ComputeHash(fileContent1);
            string fileHash2 = Sha256.ComputeHash(fileContent2);

            var codeFileDtos = new List<CodeFileDto>
            {
                new CodeFileDto(fileHash1, fileContent1),
                new CodeFileDto(fileHash2, fileContent2),
            };

            var mockMethodCallsCount = 1;

            codeClientMock
                .Setup(codeClient => codeClient.UploadFilesAsync(bundle.Id, It.IsAny<IEnumerable<CodeFileDto>>(), It.IsAny<CancellationToken>()).Result)
                .Returns(true)
                .Callback<string, IEnumerable<CodeFileDto>, CancellationToken> ((str, codeFiles, cancellationToken) =>
                {
                    mockMethodCallsCount++;

                    if (mockMethodCallsCount > 3)
                    {
                        bundleDto.MissingFiles = new string[] { };
                    }
                });

            var bundleService = new BundleService(codeClientMock.Object);

            var solutionServiceMock = new Mock<ISolutionService>();
            var filtersServiceMock = new Mock<IFiltersService>();
            var codeCacheServiceMock = new Mock<ICodeCacheService>();

            var fileHashToContentDictionary = new Dictionary<string, string>();
            fileHashToContentDictionary.Add(fileHash1, fileContent1);
            fileHashToContentDictionary.Add(fileHash2, fileContent2);

            codeCacheServiceMock
                .Setup(codeCacheService => codeCacheService.GetFileHashToContentDictionary(It.IsAny<IEnumerable<string>>()))
                .Returns(fileHashToContentDictionary);

            await bundleService.UploadMissingFilesAsync(bundle, codeCacheServiceMock.Object, It.IsAny<CancellationToken>());

            codeClientMock
                .Verify(codeClient => codeClient.CheckBundleAsync(bundle.Id, It.IsAny<CancellationToken>()), Times.Exactly(3));

            codeClientMock
                .Verify(codeClient => codeClient.UploadFilesAsync(bundle.Id, It.IsAny<IEnumerable<CodeFileDto>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Fact]
        public async Task BundleService_ActiveBundleProvided_CheckBundleSuccessfullAsync()
        {
            var codeClientMock = new Mock<ISnykCodeClient>();

            var dummyBundleDto = new BundleResponseDto { Id = "dummy id" };

            codeClientMock
                .Setup(codeClient => codeClient.CheckBundleAsync(dummyBundleDto.Id, It.IsAny<CancellationToken>()).Result)
                .Returns(dummyBundleDto);

            var bundleService = new BundleService(codeClientMock.Object);

            var bundle = await bundleService.CheckBundleAsync(dummyBundleDto.Id);

            Assert.NotNull(bundle);
            Assert.Equal(dummyBundleDto.Id, bundle.Id);
        }

        [Fact]
        public async Task BundleService_ThreeFilesProvided_UploadedSuccessfullyAsync()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            string fileContent1 = "namespace HelloWorld {public class HelloWorld {}}";
            string filePath1 = "/HelloWorld.cs";
            string fileHash1 = Sha256.ComputeHash(fileContent1);

            filePathToHashDict.Add(filePath1, fileHash1);

            string fileContent2 = "namespace HelloWorld {public class HelloWorldTest {}}";
            string filePath2 = "/HelloWorldTest.cs";
            string fileHash2 = Sha256.ComputeHash(fileContent2);

            filePathToHashDict.Add(filePath2, fileHash2);

            string fileContent3 = "namespace HelloWorld {public class HelloWorldService {}}";
            string filePath3 = "/HelloWorldService.cs";
            string fileHash3 = Sha256.ComputeHash(fileContent3);

            filePathToHashDict.Add(filePath3, fileHash3);

            var codeClientMock = new Mock<ISnykCodeClient>();

            var dummyBundleDto = new BundleResponseDto { Id = "dummy id" };

            codeClientMock
                .Setup(codeClient => codeClient.CreateBundleAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()).Result)
                .Returns(dummyBundleDto);

            var bundleService = new BundleService(codeClientMock.Object);

            var createdBundle = await bundleService.CreateBundleAsync(filePathToHashDict);

            Assert.NotNull(createdBundle);
            Assert.True(!string.IsNullOrEmpty(createdBundle.Id));

            var fileHashToContentDict = new Dictionary<string, string>();

            fileHashToContentDict.Add(fileHash1, fileContent1);
            fileHashToContentDict.Add(fileHash2, fileContent2);
            fileHashToContentDict.Add(fileHash3, fileContent3);

            codeClientMock
                .Setup(codeClient => codeClient.UploadFilesAsync(dummyBundleDto.Id, It.IsAny<IEnumerable<CodeFileDto>>(), It.IsAny<CancellationToken>()).Result)
                .Returns(true);

            bool isSuccess = await bundleService.UploadFilesAsync(createdBundle.Id, fileHashToContentDict, 200);

            Assert.True(isSuccess);

            codeClientMock
                .Verify(codeClient => codeClient.CreateBundleAsync(filePathToHashDict, It.IsAny<CancellationToken>()));

            codeClientMock
                .Verify(codeClient => codeClient.UploadFilesAsync(dummyBundleDto.Id, It.IsAny<IEnumerable<CodeFileDto>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Fact]
        public async Task BundleService_TwoFilesAndRemoveOneFileProvided_ExtendBundleChecksPassAsync()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";

            filePathToHashDict.Add(fileName1, Sha256.ComputeHash(fileName1));
            filePathToHashDict.Add(fileName2, Sha256.ComputeHash(fileName2));

            var codeClientMock = new Mock<ISnykCodeClient>();

            var bundleService = new BundleService(codeClientMock.Object);

            var dummyBundleDto = new BundleResponseDto { Id = "dummy id" };

            codeClientMock
                .Setup(codeClient => codeClient.CreateBundleAsync(filePathToHashDict, It.IsAny<CancellationToken>()).Result)
                .Returns(dummyBundleDto);

            var firstBundleDto = await bundleService.CreateBundleAsync(filePathToHashDict);

            var extendFilePathToHashDict = new Dictionary<string, string>();

            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";

            extendFilePathToHashDict.Add(fileName3, Sha256.ComputeHash(fileName3));
            extendFilePathToHashDict.Add(fileName4, Sha256.ComputeHash(fileName4));

            var filesToRemovePaths = new List<string>();

            filesToRemovePaths.Add(fileName1);

            var resultExtendBundleDto = new BundleResponseDto
            {
                Id = dummyBundleDto.Id,
                MissingFiles = new string[3],
            };

            codeClientMock
                .Setup(codeClient => codeClient.ExtendBundleAsync(dummyBundleDto.Id, It.IsAny<Dictionary<string, string>>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()).Result)
                .Returns(resultExtendBundleDto);

            var uploadedBundle = await bundleService.ExtendBundleAsync(firstBundleDto.Id, extendFilePathToHashDict, filesToRemovePaths, 200);

            Assert.NotNull(uploadedBundle);
            Assert.NotEmpty(uploadedBundle.Id);
            Assert.Equal(3, uploadedBundle.MissingFiles.Count);

            codeClientMock
                .Verify(codeClient => codeClient.CreateBundleAsync(filePathToHashDict, It.IsAny<CancellationToken>()));

            codeClientMock
                .Verify(codeClient => codeClient.ExtendBundleAsync(dummyBundleDto.Id, It.IsAny<Dictionary<string, string>>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Fact]
        public async Task BundleService_FiveFilesProvided_ExtendBundleChecksPassAsync()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest.cs";

            filePathToHashDict.Add(fileName, Sha256.ComputeHash(fileName));

            var codeClientMock = new Mock<ISnykCodeClient>();

            var bundleService = new BundleService(codeClientMock.Object);

            var dummyBundleDto = new BundleResponseDto { Id = "dummy id" };

            codeClientMock
                .Setup(codeClient => codeClient.CreateBundleAsync(filePathToHashDict, It.IsAny<CancellationToken>()).Result)
                .Returns(dummyBundleDto);

            var firstBundleDto = await bundleService.CreateBundleAsync(filePathToHashDict);

            var extendFilePathToHashDict = new Dictionary<string, string>();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";
            string fileName5 = "/Snyk/Code/Tests/SnykCodeBigBundleTest5.cs";

            extendFilePathToHashDict.Add(fileName1, Sha256.ComputeHash(fileName1));
            extendFilePathToHashDict.Add(fileName2, Sha256.ComputeHash(fileName2));
            extendFilePathToHashDict.Add(fileName3, Sha256.ComputeHash(fileName3));
            extendFilePathToHashDict.Add(fileName4, Sha256.ComputeHash(fileName4));
            extendFilePathToHashDict.Add(fileName5, Sha256.ComputeHash(fileName5));

            var resultExtendBundleDto = new BundleResponseDto
            {
                Id = dummyBundleDto.Id,
                MissingFiles = new string[6],
            };

            codeClientMock
                .Setup(codeClient => codeClient.ExtendBundleAsync(dummyBundleDto.Id, It.IsAny<Dictionary<string, string>>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()).Result)
                .Returns(resultExtendBundleDto);

            var extendedBundle = await bundleService.ExtendBundleAsync(firstBundleDto.Id, extendFilePathToHashDict, new List<string>(), 150);

            Assert.NotNull(extendedBundle);
            Assert.True(!string.IsNullOrEmpty(extendedBundle.Id));
            Assert.Equal(6, extendedBundle.MissingFiles.Count);

            codeClientMock
                .Verify(codeClient => codeClient.CreateBundleAsync(filePathToHashDict, It.IsAny<CancellationToken>()));

            codeClientMock
                .Verify(codeClient => codeClient.ExtendBundleAsync(dummyBundleDto.Id, It.IsAny<Dictionary<string, string>>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
        }

        [Fact]
        public async Task BundleService_FiveFilesProvided_ProcessExtendLargeBundleChecksPassAsync()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest.cs";

            filePathToHashDict.Add(fileName, Sha256.ComputeHash(fileName));

            var codeClientMock = new Mock<ISnykCodeClient>();

            var bundleService = new BundleService(codeClientMock.Object);

            var dummyBundleDto = new BundleResponseDto { Id = "dummy id" };

            codeClientMock
                .Setup(codeClient => codeClient.CreateBundleAsync(filePathToHashDict, It.IsAny<CancellationToken>()).Result)
                .Returns(dummyBundleDto);

            var firstBundleDto = await bundleService.CreateBundleAsync(filePathToHashDict);

            var extendFilePathToHashDict = new Dictionary<string, string>();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";
            string fileName5 = "/Snyk/Code/Tests/SnykCodeBigBundleTest5.cs";

            extendFilePathToHashDict.Add(fileName1, Sha256.ComputeHash(fileName1));
            extendFilePathToHashDict.Add(fileName2, Sha256.ComputeHash(fileName2));
            extendFilePathToHashDict.Add(fileName3, Sha256.ComputeHash(fileName3));
            extendFilePathToHashDict.Add(fileName4, Sha256.ComputeHash(fileName4));
            extendFilePathToHashDict.Add(fileName5, Sha256.ComputeHash(fileName5));

            var resultExtendBundleDto = new BundleResponseDto
            {
                Id = dummyBundleDto.Id,
                MissingFiles = new string[6],
            };

            codeClientMock
                .Setup(codeClient => codeClient.ExtendBundleAsync(dummyBundleDto.Id, It.IsAny<Dictionary<string, string>>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()).Result)
                .Returns(resultExtendBundleDto);

            var extendedBundle = await bundleService.ProcessExtendLargeBundleAsync(firstBundleDto.Id, extendFilePathToHashDict, null, 150);

            Assert.NotNull(extendedBundle);
            Assert.NotEmpty(extendedBundle.Id);
            Assert.Equal(6, extendedBundle.MissingFiles.Length);

            codeClientMock
                .Verify(codeClient => codeClient.CreateBundleAsync(filePathToHashDict, It.IsAny<CancellationToken>()));

            codeClientMock
                .Verify(codeClient => codeClient.ExtendBundleAsync(dummyBundleDto.Id, It.IsAny<Dictionary<string, string>>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
        }

        [Fact]
        public async Task BundleService_FiftyFilesProvided_CreateBundleChecksPassAsync()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            for (int i = 0; i < 50; i++)
            {
                string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest" + i + ".cs";

                filePathToHashDict.Add(fileName, Sha256.ComputeHash(fileName));
            }

            var codeClientMock = new Mock<ISnykCodeClient>();

            var bundleService = new BundleService(codeClientMock.Object);

            var dummyBundleDto = new BundleResponseDto { Id = "dummy id" };

            codeClientMock
                .Setup(codeClient => codeClient.CreateBundleAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()).Result)
                .Returns(dummyBundleDto);

            var resultExtendBundleDto = new BundleResponseDto
            {
                Id = dummyBundleDto.Id,
                MissingFiles = new string[50],
            };

            codeClientMock
                .Setup(codeClient => codeClient.ExtendBundleAsync(dummyBundleDto.Id, It.IsAny<Dictionary<string, string>>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()).Result)
                .Returns(resultExtendBundleDto);

            var bundleDto = await bundleService.CreateBundleAsync(filePathToHashDict, 150);

            Assert.NotNull(bundleDto);
            Assert.NotEmpty(bundleDto.Id);
            Assert.Equal(50, bundleDto.MissingFiles.Count);

            codeClientMock
                .Verify(codeClient => codeClient.CreateBundleAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()));

            codeClientMock
                .Verify(codeClient => codeClient.ExtendBundleAsync(dummyBundleDto.Id, It.IsAny<Dictionary<string, string>>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Exactly(49));
        }

        [Fact]
        public async Task BundleService_ThreeFilesProvided_ProcessCreateLargeBundleChecksPassAsync()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";

            filePathToHashDict.Add(fileName1, Sha256.ComputeHash(fileName1));
            filePathToHashDict.Add(fileName2, Sha256.ComputeHash(fileName2));
            filePathToHashDict.Add(fileName3, Sha256.ComputeHash(fileName3));

            var codeClientMock = new Mock<ISnykCodeClient>();

            var bundleService = new BundleService(codeClientMock.Object);

            var dummyBundleDto = new BundleResponseDto { Id = "dummy id" };

            codeClientMock
                .Setup(codeClient => codeClient.CreateBundleAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()).Result)
                .Returns(dummyBundleDto);

            var resultExtendBundleDto = new BundleResponseDto
            {
                Id = dummyBundleDto.Id,
                MissingFiles = new string[3],
            };

            codeClientMock
                .Setup(codeClient => codeClient.ExtendBundleAsync(dummyBundleDto.Id, It.IsAny<Dictionary<string, string>>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()).Result)
                .Returns(resultExtendBundleDto);

            var bundleDto = await bundleService.ProcessCreateLargeBundleAsync(filePathToHashDict, 175);

            Assert.NotNull(bundleDto);
            Assert.NotEmpty(bundleDto.Id);
            Assert.Equal(3, bundleDto.MissingFiles.Length);

            codeClientMock
                .Verify(codeClient => codeClient.CreateBundleAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()));

            codeClientMock
                .Verify(codeClient => codeClient.ExtendBundleAsync(dummyBundleDto.Id, It.IsAny<Dictionary<string, string>>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public void BundleService_FourFilesProvided_SplitFilesToChunksBySizeCheckPass()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";

            filePathToHashDict.Add(fileName1, Sha256.ComputeHash(fileName1));
            filePathToHashDict.Add(fileName2, Sha256.ComputeHash(fileName2));
            filePathToHashDict.Add(fileName3, Sha256.ComputeHash(fileName3));
            filePathToHashDict.Add(fileName4, Sha256.ComputeHash(fileName4));

            var codeClientMock = new Mock<ISnykCodeClient>();

            var bundleService = new BundleService(codeClientMock.Object);

            var fileDictionaries = bundleService.SplitFilesToChunkListsBySize(filePathToHashDict, 150);

            Assert.NotNull(fileDictionaries);
            Assert.Equal(4, fileDictionaries.Count);
        }

        [Fact]
        public void BundleService_SixFilesProvided_SplitRemovedFilesToChunksBySizeCheckPass()
        {
            var fileToRemovePaths = new List<string>();

            fileToRemovePaths.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest5.cs");
            fileToRemovePaths.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest6.cs");
            fileToRemovePaths.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest7.cs");
            fileToRemovePaths.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest8.cs");
            fileToRemovePaths.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest9.cs");
            fileToRemovePaths.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest10.cs");

            var codeClientMock = new Mock<ISnykCodeClient>();

            var bundleService = new BundleService(codeClientMock.Object);

            var removedFileChunkLists = bundleService.SplitRemovedFilesToChunkListsBySize(fileToRemovePaths, 100);

            Assert.NotNull(removedFileChunkLists);
            Assert.Equal(3, removedFileChunkLists.Count());
        }

        [Fact]
        public void BundleService_FiveFilesProvided_SplitFilesToChunksBySizeCheckPass()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";
            string fileName5 = "/Snyk/Code/Tests/SnykCodeBigBundleTest5.cs";

            filePathToHashDict.Add(fileName1, Sha256.ComputeHash(fileName1));
            filePathToHashDict.Add(fileName2, Sha256.ComputeHash(fileName2));
            filePathToHashDict.Add(fileName3, Sha256.ComputeHash(fileName3));
            filePathToHashDict.Add(fileName4, Sha256.ComputeHash(fileName4));
            filePathToHashDict.Add(fileName5, Sha256.ComputeHash(fileName5));

            var codeClientMock = new Mock<ISnykCodeClient>();

            var bundleService = new BundleService(codeClientMock.Object);

            var fileDictionaries = bundleService.SplitFilesToChunkListsBySize(filePathToHashDict, 145);

            Assert.NotNull(fileDictionaries);
            Assert.Equal(5, fileDictionaries.Count);
        }
    }
}
