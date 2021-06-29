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
        public async Task BundleService_UploadFilesProvided_ChecksPassAsync()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var files = new Dictionary<string, string>();

            string fileContent1 = "namespace HelloWorld {public class HelloWorld {}}";
            string filePath1 = "/HelloWorld.cs";
            string fileHash1 = Sha256.ComputeHash(fileContent1);

            files.Add(filePath1, fileHash1);

            string fileContent2 = "namespace HelloWorld {public class HelloWorldTest {}}";
            string filePath2 = "/HelloWorldTest.cs";
            string fileHash2 = Sha256.ComputeHash(fileContent2);

            files.Add(filePath2, fileHash2);

            string fileContent3 = "namespace HelloWorld {public class HelloWorldService {}}";
            string filePath3 = "/HelloWorldService.cs";
            string fileHash3 = Sha256.ComputeHash(fileContent3);

            files.Add(filePath3, fileHash3);

            var createdBundle = await bundleService.CreateBundle(files);

            Assert.NotNull(createdBundle);
            Assert.True(!string.IsNullOrEmpty(createdBundle.Id));

            var codeFiles = new Dictionary<string, string>();

            codeFiles.Add(fileHash1, fileContent1);
            codeFiles.Add(fileHash2, fileContent2);
            codeFiles.Add(fileHash3, fileContent3);

            bool isSuccess = await bundleService.UploadFiles(createdBundle.Id, codeFiles, 100);

            Assert.True(isSuccess);
        }

        [Fact]
        public async Task BundleService_ExtendBundleAddTwoFilesAndRemoveOneFileProvided_ChecksPassAsync()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var initialFiles = new Dictionary<string, string>();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";

            initialFiles.Add(fileName1, Sha256.ComputeHash(fileName1));
            initialFiles.Add(fileName2, Sha256.ComputeHash(fileName2));

            var firstBundleDto = await bundleService.CreateBundle(initialFiles);

            var extendFiles = new Dictionary<string, string>();

            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";

            extendFiles.Add(fileName3, Sha256.ComputeHash(fileName3));
            extendFiles.Add(fileName4, Sha256.ComputeHash(fileName4));

            var removedFiles = new List<string>();

            removedFiles.Add(fileName1);

            var uploadedBundle = await bundleService.ExtendBundle(firstBundleDto.Id, extendFiles, removedFiles, 200);

            Assert.NotNull(uploadedBundle);
            Assert.NotEmpty(uploadedBundle.Id);
            Assert.Equal(3, uploadedBundle.MissingFiles.Length);
        }

        [Fact]
        public async Task BundleService_ExtendBundleFiveFilesProvided_ChecksPassAsync()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var initialFiles = new Dictionary<string, string>();

            string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest.cs";

            initialFiles.Add(fileName, Sha256.ComputeHash(fileName));

            var firstBundleDto = await bundleService.CreateBundle(initialFiles);

            var extendFiles = new Dictionary<string, string>();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";
            string fileName5 = "/Snyk/Code/Tests/SnykCodeBigBundleTest5.cs";

            extendFiles.Add(fileName1, Sha256.ComputeHash(fileName1));
            extendFiles.Add(fileName2, Sha256.ComputeHash(fileName2));
            extendFiles.Add(fileName3, Sha256.ComputeHash(fileName3));
            extendFiles.Add(fileName4, Sha256.ComputeHash(fileName4));
            extendFiles.Add(fileName5, Sha256.ComputeHash(fileName5));

            var uploadedBundle = await bundleService.ExtendBundle(firstBundleDto.Id, extendFiles, new List<string>(), 150);

            Assert.NotNull(uploadedBundle);
            Assert.True(!string.IsNullOrEmpty(uploadedBundle.Id));
            Assert.Equal(6, uploadedBundle.MissingFiles.Length);
        }

        [Fact]
        public async Task BundleService_ExtendMultiChunkBundleFiveFilesProvided_ChecksPassAsync()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var initialFiles = new Dictionary<string, string>();

            string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest.cs";

            initialFiles.Add(fileName, Sha256.ComputeHash(fileName));

            var firstBundleDto = await bundleService.CreateBundle(initialFiles);

            var extendFiles = new Dictionary<string, string>();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";
            string fileName5 = "/Snyk/Code/Tests/SnykCodeBigBundleTest5.cs";

            extendFiles.Add(fileName1, Sha256.ComputeHash(fileName1));
            extendFiles.Add(fileName2, Sha256.ComputeHash(fileName2));
            extendFiles.Add(fileName3, Sha256.ComputeHash(fileName3));
            extendFiles.Add(fileName4, Sha256.ComputeHash(fileName4));
            extendFiles.Add(fileName5, Sha256.ComputeHash(fileName5));

            var uploadedBundle = await bundleService.ProcessExtendLargeBundle(firstBundleDto.Id, extendFiles, null, 150);

            Assert.NotNull(uploadedBundle);
            Assert.NotEmpty(uploadedBundle.Id);
            Assert.Equal(6, uploadedBundle.MissingFiles.Length);
        }

        [Fact]
        public async Task BundleService_CreateBundleBigPayloadProvided_ChecksPassAsync()
        {
            var files = new Dictionary<string, string>();

            for (int i = 0; i < 50; i++)
            {
                string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest" + i + ".cs";

                files.Add(fileName, Sha256.ComputeHash(fileName));
            }

            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var bundleDto = await bundleService.CreateBundle(files, 150);

            Assert.NotNull(bundleDto);
            Assert.NotEmpty(bundleDto.Id);
            Assert.Equal(50, bundleDto.MissingFiles.Length);
        }

        [Fact]
        public async Task BundleService_CreateMultiBundleThreeFilesProvided_ChecksPassAsync()
        {
            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var files = new Dictionary<string, string>();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";

            files.Add(fileName1, Sha256.ComputeHash(fileName1));
            files.Add(fileName2, Sha256.ComputeHash(fileName2));
            files.Add(fileName3, Sha256.ComputeHash(fileName3));

            var bundleDto = await bundleService.ProcessCreateLargeBundle(files, 175);

            Assert.NotNull(bundleDto);
            Assert.NotEmpty(bundleDto.Id);
            Assert.Equal(3, bundleDto.MissingFiles.Length);
        }

        [Fact]
        public void BundleService_SplitFilesToChunksBySizeFourFilesProvided_CheckPass()
        {
            var files = new Dictionary<string, string>();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";

            files.Add(fileName1, Sha256.ComputeHash(fileName1));
            files.Add(fileName2, Sha256.ComputeHash(fileName2));
            files.Add(fileName3, Sha256.ComputeHash(fileName3));
            files.Add(fileName4, Sha256.ComputeHash(fileName4));

            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            // 150 is max bundle size (chunk size).
            var fileDictionaries = bundleService.SplitFilesToChunkListsBySize(files, 150);

            Assert.NotNull(fileDictionaries);
            Assert.Equal(4, fileDictionaries.Count);
        }

        [Fact]
        public void BundleService_SplitRemovedFilesToChunksBySizeSixFilesProvided_CheckPass()
        {
            var removedFiles = new List<string>();

            removedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest5.cs");
            removedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest6.cs");
            removedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest7.cs");
            removedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest8.cs");
            removedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest9.cs");
            removedFiles.Add("/Snyk/Code/Tests/SnykCodeBigBundleTest10.cs");

            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            // 150 is max bundle size (chunk size).
            var removedFileChunkLists = bundleService.SplitRemovedFilesToChunkListsBySize(removedFiles, 100);

            Assert.NotNull(removedFileChunkLists);
            Assert.Equal(3, removedFileChunkLists.Count);
        }

        [Fact]
        public void BundleService_SplitFilesToChunksBySizeFiveFilesProvided_CheckPass()
        {
            var files = new Dictionary<string, string>();

            string fileName1 = "/Snyk/Code/Tests/SnykCodeBigBundleTest1.cs";
            string fileName2 = "/Snyk/Code/Tests/SnykCodeBigBundleTest2.cs";
            string fileName3 = "/Snyk/Code/Tests/SnykCodeBigBundleTest3.cs";
            string fileName4 = "/Snyk/Code/Tests/SnykCodeBigBundleTest4.cs";
            string fileName5 = "/Snyk/Code/Tests/SnykCodeBigBundleTest5.cs";

            files.Add(fileName1, Sha256.ComputeHash(fileName1));
            files.Add(fileName2, Sha256.ComputeHash(fileName2));
            files.Add(fileName3, Sha256.ComputeHash(fileName3));
            files.Add(fileName4, Sha256.ComputeHash(fileName4));
            files.Add(fileName5, Sha256.ComputeHash(fileName5));

            var bundleService = new BundleService(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            // 150 is max bundle size (chunk size).
            var fileDictionaries = bundleService.SplitFilesToChunkListsBySize(files, 145);

            Assert.NotNull(fileDictionaries);
            Assert.Equal(5, fileDictionaries.Count);
        }
    }
}
