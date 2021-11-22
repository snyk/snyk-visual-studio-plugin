namespace Snyk.Code.Library.Tests.Api
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto;
    using Snyk.Code.Library.Api.Dto.Analysis;
    using Snyk.Common;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="SnykCodeClient"/>.
    /// </summary>
    public class SnykCodeClientTest
    {
        [Fact]
        public async Task SnykCodeClient_TwoFilesWithIssuesProvided_GetAnalysisSuccessAsync()
        {
            var bundleFiles = new Dictionary<string, string>();

            string fileContent1 = this.GetResourceContent("app1.js");
            string filePath1 = "/app1.js";
            string fileHash1 = Sha256.ComputeHash(fileContent1);

            bundleFiles.Add(filePath1, fileHash1);

            string fileContent2 = this.GetResourceContent("app2.js");
            string filePath2 = "/app2.js";
            string fileHash2 = Sha256.ComputeHash(fileContent2);

            bundleFiles.Add(filePath2, fileHash2);

            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var createdBundle = await snykCodeClient.CreateBundleAsync(bundleFiles);

            Assert.NotNull(createdBundle);
            Assert.True(!string.IsNullOrEmpty(createdBundle.Hash));

            var hashToContentDict = new Dictionary<string, string>();

            hashToContentDict.Add(fileHash1, fileContent2);
            hashToContentDict.Add(fileHash2, fileContent2);

            var codeFile1 = new CodeFileDto(fileHash1, fileContent1);
            var codeFile2 = new CodeFileDto(fileHash2, fileContent2);

            var fileNameToContentDict = new Dictionary<string, CodeFileDto>();
            fileNameToContentDict.Add(filePath1, codeFile1);
            fileNameToContentDict.Add(filePath2, codeFile2);

            var extendedBundle = await snykCodeClient.ExtendBundleAsync(createdBundle.Hash, fileNameToContentDict);

            Assert.True(extendedBundle.MissingFiles.IsEmpty());

            AnalysisResultDto analysisResult = await snykCodeClient.GetAnalysisAsync(createdBundle.Hash);

            Assert.NotNull(analysisResult);

            if (analysisResult.Status == "WAITING")
            {
                System.Threading.Thread.Sleep(5000);

                analysisResult = await snykCodeClient.GetAnalysisAsync(createdBundle.Hash);
            }

            Assert.NotNull(analysisResult);
            Assert.Equal("COMPLETE", analysisResult.Status);
            Assert.NotNull(analysisResult.Files);
            Assert.NotEmpty(analysisResult.Files);
        }

        [Fact]
        public async Task SnykCodeClient_OneFileWithIssuesProvided_GetAnalysisSuccessAsync()
        {
            var bundleFiles = new Dictionary<string, string>();

            string fileContent1 = this.GetResourceContent("app1.js");
            string filePath1 = "/app.js";
            string fileHash1 = Sha256.ComputeHash(fileContent1);

            bundleFiles.Add(filePath1, fileHash1);

            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var createdBundle = await snykCodeClient.CreateBundleAsync(bundleFiles);

            Assert.NotNull(createdBundle);
            Assert.True(!string.IsNullOrEmpty(createdBundle.Hash));

            var pathToContentDict = new Dictionary<string, CodeFileDto>();

            pathToContentDict.Add(filePath1, new CodeFileDto(fileHash1, fileContent1));

            var extendedBundle = await snykCodeClient.ExtendBundleAsync(createdBundle.Hash, pathToContentDict);

            Assert.True(extendedBundle.MissingFiles.IsEmpty());

            AnalysisResultDto analysisResult = await snykCodeClient.GetAnalysisAsync(createdBundle.Hash);

            Assert.NotNull(analysisResult);

            if (analysisResult.Status == "WAITING")
            {
                System.Threading.Thread.Sleep(15000);

                analysisResult = await snykCodeClient.GetAnalysisAsync(createdBundle.Hash);
            }

            Assert.NotNull(analysisResult);
            Assert.Equal("COMPLETE", analysisResult.Status);
            Assert.NotEmpty(analysisResult.Files);
            Assert.NotEmpty(analysisResult.Suggestions);
        }

        [Fact]
        public async Task SnykCodeClient_SimpleBundleProvided_GetAnalysisPassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var bundleFiles = new Dictionary<string, string>();

            string fileContent1 = "namespace HelloWorld {public class HelloWorld {}}";
            string filePath1 = "/HelloWorld.cs";
            string fileHash1 = Sha256.ComputeHash(fileContent1);

            bundleFiles.Add(filePath1, fileHash1);

            string fileContent2 = "namespace HelloWorld {public class HelloWorld1Test {}}";
            string filePath2 = "/HelloWorldTest1.cs";
            string fileHash2 = Sha256.ComputeHash(fileContent2);

            bundleFiles.Add(filePath2, fileHash2);

            string fileContent3 = "namespace HelloWorld {public class HelloWorldService {}}";
            string filePath3 = "/HelloWorldService.cs";
            string fileHash3 = Sha256.ComputeHash(fileContent3);

            bundleFiles.Add(filePath3, fileHash3);

            var createdBundle = await snykCodeClient.CreateBundleAsync(bundleFiles);

            Assert.NotNull(createdBundle);
            Assert.True(!string.IsNullOrEmpty(createdBundle.Hash));

            var pathToContentDict = new Dictionary<string, CodeFileDto>();

            pathToContentDict.Add(filePath1, new CodeFileDto(fileHash1, fileContent1));
            pathToContentDict.Add(filePath2, new CodeFileDto(fileHash2, fileContent2));
            pathToContentDict.Add(filePath3, new CodeFileDto(fileHash3, fileContent3));

            var extendBundle = await snykCodeClient.ExtendBundleAsync(createdBundle.Hash, pathToContentDict);

            Assert.True(extendBundle.MissingFiles.IsEmpty());

            var analysisResult = await snykCodeClient.GetAnalysisAsync(createdBundle.Hash);

            Assert.NotNull(analysisResult);

            if (analysisResult.Status == "WAITING")
            {
                System.Threading.Thread.Sleep(5000);

                analysisResult = await snykCodeClient.GetAnalysisAsync(createdBundle.Hash);
            }

            Assert.NotNull(analysisResult);
        }

        [Fact]
        public async Task SnykCodeClient_ThreeFilesProvided_UploadedSuccessfullyAsync()
        {
            var pathToHashFilesDict = new Dictionary<string, string>();

            string fileContent1 = "namespace HelloWorld {public class HelloWorld {}}";
            string filePath1 = "/HelloWorld.cs";
            string fileHash1 = Sha256.ComputeHash(fileContent1);

            pathToHashFilesDict.Add(filePath1, fileHash1);

            string fileContent2 = "namespace HelloWorld {public class HelloWorldTest {}}";
            string filePath2 = "/HelloWorldTest.cs";
            string fileHash2 = Sha256.ComputeHash(fileContent2);

            pathToHashFilesDict.Add(filePath2, fileHash2);

            string fileContent3 = "namespace HelloWorld {public class HelloWorldService {}}";
            string filePath3 = "/HelloWorldService.cs";
            string fileHash3 = Sha256.ComputeHash(fileContent3);

            pathToHashFilesDict.Add(filePath3, fileHash3);

            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var createdBundle = await snykCodeClient.CreateBundleAsync(pathToHashFilesDict);

            Assert.NotNull(createdBundle);
            Assert.NotEmpty(createdBundle.Hash);

            var pathToContentDict = new Dictionary<string, CodeFileDto>();

            pathToContentDict.Add(filePath1, new CodeFileDto(fileHash1, fileContent1));
            pathToContentDict.Add(filePath2, new CodeFileDto(fileHash2, fileContent2));
            pathToContentDict.Add(filePath3, new CodeFileDto(fileHash3, fileContent3));

            var bundle = await snykCodeClient.ExtendBundleAsync(createdBundle.Hash, pathToContentDict);

            Assert.True(bundle.MissingFiles.IsEmpty());
        }

        [Fact]
        public async Task SnykCodeClient_UploadFileProvided_UploadedSuccessfullyAsync()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            string fileContent = "namespace HelloWorld {public class HelloWorld {}}";

            string filePath = "/HelloWorld.cs";
            string fileHash = Sha256.ComputeHash(fileContent);

            filePathToHashDict.Add(filePath, fileHash);

            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var createdBundle = await snykCodeClient.CreateBundleAsync(filePathToHashDict);

            Assert.NotNull(createdBundle);
            Assert.True(!string.IsNullOrEmpty(createdBundle.Hash));

            var pathToContentDict = new Dictionary<string, CodeFileDto>();

            pathToContentDict.Add(filePath, new CodeFileDto(fileHash, fileContent));

            var bundle = await snykCodeClient.ExtendBundleAsync(createdBundle.Hash, pathToContentDict);

            Assert.True(bundle.MissingFiles.IsEmpty());
        }

        [Fact]
        public async Task SnykCodeClient_CreateBundleSmallPayloadProvided_ChecksPassAsync()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            for (int i = 0; i < 10; i++)
            {
                string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest" + i + ".cs";

                filePathToHashDict.Add(fileName, Sha256.ComputeHash(fileName));
            }

            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var uploadedBundle = await snykCodeClient.CreateBundleAsync(filePathToHashDict);

            Assert.NotNull(uploadedBundle);
            Assert.True(!string.IsNullOrEmpty(uploadedBundle.Hash));
            Assert.True(uploadedBundle.MissingFiles.Length == 10);
        }

        [Fact]
        public async Task SnykCodeClient_ExtendBundleAddFilesProvied_ChecksPassAsync()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            filePathToHashDict.Add("/Test1.cs", Sha256.ComputeHash("/Test1.cs"));
            filePathToHashDict.Add("/Test2.cs", Sha256.ComputeHash("/Test2.cs"));
            filePathToHashDict.Add("/Test3.cs", Sha256.ComputeHash("/Test3.cs"));

            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var resultBundleDto = await snykCodeClient.CreateBundleAsync(filePathToHashDict);

            Assert.NotNull(resultBundleDto);
            Assert.False(string.IsNullOrEmpty(resultBundleDto.Hash));

            var extendBundleFilePathToHashDict = new Dictionary<string, string>();

            extendBundleFilePathToHashDict.Add("/Test4.cs", Sha256.ComputeHash("/Test4.cs"));
            extendBundleFilePathToHashDict.Add("/Test5.cs", Sha256.ComputeHash("/Test5.cs"));
            extendBundleFilePathToHashDict.Add("/Test6.cs", Sha256.ComputeHash("/Test6.cs"));

            BundleResponseDto extendedBundleDto = await snykCodeClient.ExtendBundleAsync(resultBundleDto.Hash, extendBundleFilePathToHashDict, new List<string>());

            Assert.NotNull(extendedBundleDto);
            Assert.False(string.IsNullOrEmpty(extendedBundleDto.Hash));
            Assert.Equal(6, extendedBundleDto.MissingFiles.Length);
            Assert.False(resultBundleDto.Hash == extendedBundleDto.Hash);
        }

        [Fact]
        public async Task SnykCodeClient_ExtendBundleRemoveFilesProvied_ChecksPassAsync()
        {
            Dictionary<string, string> filePathToHashDict = new Dictionary<string, string>();

            filePathToHashDict.Add("/Test1.cs", Sha256.ComputeHash("/Test1.cs"));
            filePathToHashDict.Add("/Test2.cs", Sha256.ComputeHash("/Test2.cs"));
            filePathToHashDict.Add("/Test3.cs", Sha256.ComputeHash("/Test3.cs"));

            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var resultBundleDto = await snykCodeClient.CreateBundleAsync(filePathToHashDict);

            Assert.NotNull(resultBundleDto);
            Assert.False(string.IsNullOrEmpty(resultBundleDto.Hash));

            var extendFilePathToHashDict = new Dictionary<string, string>();

            extendFilePathToHashDict.Add("/Test4.cs", Sha256.ComputeHash("/Test4.cs"));
            extendFilePathToHashDict.Add("/Test5.cs", Sha256.ComputeHash("/Test5.cs"));
            extendFilePathToHashDict.Add("/Test6.cs", Sha256.ComputeHash("/Test6.cs"));

            var removedFiles = new List<string>();

            removedFiles.Add("/Test1.cs");
            removedFiles.Add("/Test2.cs");

            BundleResponseDto extendedBundleDto = await snykCodeClient
                .ExtendBundleAsync(resultBundleDto.Hash, extendFilePathToHashDict, removedFiles);

            Assert.NotNull(extendedBundleDto);
            Assert.False(string.IsNullOrEmpty(extendedBundleDto.Hash));
            Assert.Equal(4, extendedBundleDto.MissingFiles.Length);
            Assert.False(resultBundleDto.Hash == extendedBundleDto.Hash);
        }

        [Fact]
        public async Task SnykCodeClient_ProperBundleProvided_CheckBundlePassAsync()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            filePathToHashDict.Add("/Test.cs", Sha256.ComputeHash("/Test.cs"));

            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var resultBundleDto = await snykCodeClient.CreateBundleAsync(filePathToHashDict);

            Assert.NotNull(resultBundleDto);
            Assert.False(string.IsNullOrEmpty(resultBundleDto.Hash));

            var checkedBundle = await snykCodeClient.CheckBundleAsync(resultBundleDto.Hash);

            Assert.NotNull(checkedBundle);
            Assert.False(string.IsNullOrEmpty(checkedBundle.Hash));
        }

        [Fact]
        public void SnykCodeClient_WrongBundleProvided_CheckBundleFailed()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            _ = Assert.ThrowsAsync<AggregateException>(() => snykCodeClient.CheckBundleAsync("dummy"));
        }

        [Fact]
        public async Task SnykCodeClient_SimpleFileProvided_CreateBundlePassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var filePathToHashDict = new Dictionary<string, string>();

            filePathToHashDict.Add("/Test.cs", Sha256.ComputeHash("/Test.cs"));

            var uploadedBundle = await snykCodeClient.CreateBundleAsync(filePathToHashDict);

            Assert.NotNull(uploadedBundle);
            Assert.True(!string.IsNullOrEmpty(uploadedBundle.Hash));
        }

        [Fact]
        public async Task SnykCodeClient_GetFilters_ChecksPassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var filters = await snykCodeClient.GetFiltersAsync();

            Assert.NotNull(filters);
            Assert.NotNull(filters.Extensions);
            Assert.NotNull(filters.ConfigFiles);
        }

        [Fact]
        public void SnykCodeClient_WrongPayloadProvided_ChecksFailed()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, string.Empty);

            _ = Assert.ThrowsAsync<AggregateException>(() => snykCodeClient.LoginAsync("\\{"));
        }

        [Fact]
        public void SnykCodeClient_WaitingAnalysisResultJsonProvided_DeserialisationAnalysisJsonSuccess()
        {
            var analysisResultDto = Json.Deserialize<AnalysisResultDto>("{\"status\":\"ANALYZING\",\"progress\":0.5,\"complete\":false}");

            Assert.NotNull(analysisResultDto);
        }

        private string GetResourceContent(string resourceName) 
            => File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Resources", resourceName));
    }
}
