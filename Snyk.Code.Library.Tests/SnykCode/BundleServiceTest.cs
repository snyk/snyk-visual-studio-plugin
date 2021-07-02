namespace Snyk.Code.Library.Tests.Api
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Common;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="SnykCodeService"/>.
    /// </summary>
    public class BundleServiceTest
    {
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

            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var createdBundle = await bundleService.CreateBundleAsync(filePathToHashDict);

            Assert.NotNull(createdBundle);
            Assert.True(!string.IsNullOrEmpty(createdBundle.Id));

            var fileHashToContentDict = new Dictionary<string, string>();

            fileHashToContentDict.Add(fileHash1, fileContent1);
            fileHashToContentDict.Add(fileHash2, fileContent2);
            fileHashToContentDict.Add(fileHash3, fileContent3);

            bool isSuccess = await bundleService.UploadFilesAsync(createdBundle.Id, fileHashToContentDict, 100);

            Assert.True(isSuccess);
        }

        [Fact]
        public async Task BundleService_ExtendBundleAddTwoFilesAndRemoveOneFileProvided_ChecksPassAsync()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var filePathToHashDict = new Dictionary<string, string>();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";

            filePathToHashDict.Add(fileName1, Sha256.ComputeHash(fileName1));
            filePathToHashDict.Add(fileName2, Sha256.ComputeHash(fileName2));

            var firstBundleDto = await bundleService.CreateBundleAsync(filePathToHashDict);

            var extendFilePathToHashDict = new Dictionary<string, string>();

            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";

            extendFilePathToHashDict.Add(fileName3, Sha256.ComputeHash(fileName3));
            extendFilePathToHashDict.Add(fileName4, Sha256.ComputeHash(fileName4));

            var filesToRemovePaths = new List<string>();

            filesToRemovePaths.Add(fileName1);

            var uploadedBundle = await bundleService.ExtendBundleAsync(firstBundleDto.Id, extendFilePathToHashDict, filesToRemovePaths, 200);

            Assert.NotNull(uploadedBundle);
            Assert.NotEmpty(uploadedBundle.Id);
            Assert.Equal(3, uploadedBundle.MissingFiles.Length);
        }

        [Fact]
        public async Task BundleService_ExtendBundleFiveFilesProvided_ChecksPassAsync()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest.cs";

            filePathToHashDict.Add(fileName, Sha256.ComputeHash(fileName));

            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

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

            var extendedBundle = await bundleService.ExtendBundleAsync(firstBundleDto.Id, extendFilePathToHashDict, new List<string>(), 150);

            Assert.NotNull(extendedBundle);
            Assert.True(!string.IsNullOrEmpty(extendedBundle.Id));
            Assert.Equal(6, extendedBundle.MissingFiles.Length);
        }

        [Fact]
        public async Task BundleService_ExtendMultiChunkBundleFiveFilesProvided_ChecksPassAsync()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest.cs";

            filePathToHashDict.Add(fileName, Sha256.ComputeHash(fileName));

            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

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

            var extendedBundle = await bundleService.ProcessExtendLargeBundleAsync(firstBundleDto.Id, extendFilePathToHashDict, null, 150);

            Assert.NotNull(extendedBundle);
            Assert.NotEmpty(extendedBundle.Id);
            Assert.Equal(6, extendedBundle.MissingFiles.Length);
        }

        [Fact]
        public async Task BundleService_CreateBundleBigPayloadProvided_ChecksPassAsync()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            for (int i = 0; i < 50; i++)
            {
                string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest" + i + ".cs";

                filePathToHashDict.Add(fileName, Sha256.ComputeHash(fileName));
            }

            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var bundleDto = await bundleService.CreateBundleAsync(filePathToHashDict, 150);

            Assert.NotNull(bundleDto);
            Assert.NotEmpty(bundleDto.Id);
            Assert.Equal(50, bundleDto.MissingFiles.Length);
        }

        [Fact]
        public async Task BundleService_CreateMultiBundleThreeFilesProvided_ChecksPassAsync()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";

            filePathToHashDict.Add(fileName1, Sha256.ComputeHash(fileName1));
            filePathToHashDict.Add(fileName2, Sha256.ComputeHash(fileName2));
            filePathToHashDict.Add(fileName3, Sha256.ComputeHash(fileName3));

            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var bundleDto = await bundleService.ProcessCreateLargeBundleAsync(filePathToHashDict, 175);

            Assert.NotNull(bundleDto);
            Assert.NotEmpty(bundleDto.Id);
            Assert.Equal(3, bundleDto.MissingFiles.Length);
        }

        [Fact]
        public void BundleService_SplitFilesToChunksBySizeFourFilesProvided_CheckPass()
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

            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            // 150 is max bundle size (chunk size).
            var fileDictionaries = bundleService.SplitFilesToChunkListsBySize(filePathToHashDict, 150);

            Assert.NotNull(fileDictionaries);
            Assert.Equal(4, fileDictionaries.Count);
        }

        [Fact]
        public void BundleService_SplitRemovedFilesToChunksBySizeSixFilesProvided_CheckPass()
        {
            var fileToRemovePaths = new List<string>();

            fileToRemovePaths.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest5.cs");
            fileToRemovePaths.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest6.cs");
            fileToRemovePaths.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest7.cs");
            fileToRemovePaths.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest8.cs");
            fileToRemovePaths.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest9.cs");
            fileToRemovePaths.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest10.cs");

            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            // 150 is max bundle size (chunk size).
            var removedFileChunkLists = bundleService.SplitRemovedFilesToChunkListsBySize(fileToRemovePaths, 100);

            Assert.NotNull(removedFileChunkLists);
            Assert.Equal(3, removedFileChunkLists.Count);
        }

        [Fact]
        public void BundleService_SplitFilesToChunksBySizeFiveFilesProvided_CheckPass()
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

            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            // 150 is max bundle size (chunk size).
            var fileDictionaries = bundleService.SplitFilesToChunkListsBySize(filePathToHashDict, 145);

            Assert.NotNull(fileDictionaries);
            Assert.Equal(5, fileDictionaries.Count);
        }
    }
}
