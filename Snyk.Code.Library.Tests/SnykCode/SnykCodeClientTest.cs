namespace Snyk.Code.Library.Tests.Api
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto;
    using Snyk.Code.Library.Common;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="SnykCodeClient"/>.
    /// </summary>
    public class SnykCodeClientTest
    {
        private const string TestUserAgent = "Test-VisualStudio";

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
            Assert.NotEmpty(createdBundle.Id);

            var fileHashToContentDict = new Dictionary<string, string>();

            fileHashToContentDict.Add(fileHash1, fileContent1);
            fileHashToContentDict.Add(fileHash2, fileContent2);
            fileHashToContentDict.Add(fileHash3, fileContent3);

            bool isSuccess = await snykCodeClient.UploadFilesAsync(createdBundle.Id, fileHashToContentDict);

            Assert.True(isSuccess);
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
            Assert.True(!string.IsNullOrEmpty(createdBundle.Id));

            var fileHashToContentDict = new Dictionary<string, string>();

            fileHashToContentDict.Add(fileHash, fileContent);

            bool isSuccess = await snykCodeClient.UploadFilesAsync(createdBundle.Id, fileHashToContentDict);

            Assert.True(isSuccess);
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
            Assert.True(!string.IsNullOrEmpty(uploadedBundle.Id));
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
            Assert.False(string.IsNullOrEmpty(resultBundleDto.Id));

            var extendBundleFilePathToHashDict = new Dictionary<string, string>();

            extendBundleFilePathToHashDict.Add("/Test4.cs", Sha256.ComputeHash("/Test4.cs"));
            extendBundleFilePathToHashDict.Add("/Test5.cs", Sha256.ComputeHash("/Test5.cs"));
            extendBundleFilePathToHashDict.Add("/Test6.cs", Sha256.ComputeHash("/Test6.cs"));

            BundleResponseDto extendedBundleDto = await snykCodeClient.ExtendBundleAsync(resultBundleDto.Id, extendBundleFilePathToHashDict, new List<string>());

            Assert.NotNull(extendedBundleDto);
            Assert.False(string.IsNullOrEmpty(extendedBundleDto.Id));
            Assert.Equal(6, extendedBundleDto.MissingFiles.Length);
            Assert.False(resultBundleDto.Id == extendedBundleDto.Id);
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
            Assert.False(string.IsNullOrEmpty(resultBundleDto.Id));

            var extendFilePathToHashDict = new Dictionary<string, string>();

            extendFilePathToHashDict.Add("/Test4.cs", Sha256.ComputeHash("/Test4.cs"));
            extendFilePathToHashDict.Add("/Test5.cs", Sha256.ComputeHash("/Test5.cs"));
            extendFilePathToHashDict.Add("/Test6.cs", Sha256.ComputeHash("/Test6.cs"));

            var removedFiles = new List<string>();

            removedFiles.Add("/Test1.cs");
            removedFiles.Add("/Test2.cs");

            BundleResponseDto extendedBundleDto = await snykCodeClient
                .ExtendBundleAsync(resultBundleDto.Id, extendFilePathToHashDict, removedFiles);

            Assert.NotNull(extendedBundleDto);
            Assert.False(string.IsNullOrEmpty(extendedBundleDto.Id));
            Assert.Equal(4, extendedBundleDto.MissingFiles.Length);
            Assert.False(resultBundleDto.Id == extendedBundleDto.Id);
        }

        [Fact]
        public async Task SnykCodeClient_ProperBundleProvided_CheckBundlePassAsync()
        {
            var filePathToHashDict = new Dictionary<string, string>();

            filePathToHashDict.Add("/Test.cs", Sha256.ComputeHash("/Test.cs"));

            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var resultBundleDto = await snykCodeClient.CreateBundleAsync(filePathToHashDict);

            Assert.NotNull(resultBundleDto);
            Assert.False(string.IsNullOrEmpty(resultBundleDto.Id));

            var checkedBundle = await snykCodeClient.CheckBundleAsync(resultBundleDto.Id);

            Assert.NotNull(checkedBundle);
            Assert.False(string.IsNullOrEmpty(checkedBundle.Id));
        }

        [Fact]
        public void SnykCodeClient_WrongBundleProvided_CheckBundleFailed()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            _ = Assert.ThrowsAsync<AggregateException>(() => snykCodeClient.CheckBundleAsync("dummy"));
        }

        [Fact]
        public async Task SnykCodeClient_CreateBundleEmptyFilesInBundleProvided_CheckFailAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var bundleDto = await snykCodeClient.CreateBundleAsync(new Dictionary<string, string>());

            Assert.NotNull(bundleDto);
            Assert.NotEmpty(bundleDto.Id);
            Assert.Empty(bundleDto.MissingFiles);
        }

        [Fact]
        public async Task SnykCodeClient_SimpleFileProvided_CreateBundlePassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var filePathToHashDict = new Dictionary<string, string>();

            filePathToHashDict.Add("/Test.cs", Sha256.ComputeHash("/Test.cs"));

            var uploadedBundle = await snykCodeClient.CreateBundleAsync(filePathToHashDict);

            Assert.NotNull(uploadedBundle);
            Assert.True(!string.IsNullOrEmpty(uploadedBundle.Id));
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
        public async Task SnykCodeClient_ProperLoginDataProvided_ChecksPassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var response = await snykCodeClient.LoginAsync(TestUserAgent);

            Assert.NotNull(response);
            Assert.NotEmpty(response.SessionToken);
        }

        [Fact]
        public void SnykCodeClient_WrongPayloadProvided_ChecksFailed()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, string.Empty);

            _ = Assert.ThrowsAsync<AggregateException>(() => snykCodeClient.LoginAsync("\\{"));
        }

        [Fact]
        public async Task SnykCodeClient_ChessSessionProperApiTokenProvided_CheckPassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            _ = await snykCodeClient.LoginAsync(TestUserAgent);

            var status = await snykCodeClient.CheckSessionAsync();

            Assert.True(status.IsSucccess);
        }
    }
}
