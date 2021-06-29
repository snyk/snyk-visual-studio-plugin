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
        public async Task SnykCodeClient_UploadFilesProvided_ChecksPassAsync()
        {
            var newBundleFiles = new Dictionary<string, string>();

            string fileContent1 = "namespace HelloWorld {public class HelloWorld {}}";
            string filePath1 = "/HelloWorld.cs";
            string fileHash1 = Sha256.ComputeHash(fileContent1);

            newBundleFiles.Add(filePath1, fileHash1);

            string fileContent2 = "namespace HelloWorld {public class HelloWorldTest {}}";
            string filePath2 = "/HelloWorldTest.cs";
            string fileHash2 = Sha256.ComputeHash(fileContent2);

            newBundleFiles.Add(filePath2, fileHash2);

            string fileContent3 = "namespace HelloWorld {public class HelloWorldService {}}";
            string filePath3 = "/HelloWorldService.cs";
            string fileHash3 = Sha256.ComputeHash(fileContent3);

            newBundleFiles.Add(filePath3, fileHash3);

            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var createdBundle = await snykCodeClient.CreateBundle(newBundleFiles);

            Assert.NotNull(createdBundle);
            Assert.NotEmpty(createdBundle.Id);

            var codeFiles = new Dictionary<string, string>();

            codeFiles.Add(fileHash1, fileContent1);
            codeFiles.Add(fileHash2, fileContent2);
            codeFiles.Add(fileHash3, fileContent3);

            bool isSuccess = await snykCodeClient.UploadFiles(createdBundle.Id, codeFiles);

            Assert.True(isSuccess);
        }

        [Fact]
        public async Task SnykCodeClient_UploadFileProvided_ChecksPassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var newBundleFiles = new Dictionary<string, string>();

            string fileContent = "namespace HelloWorld {public class HelloWorld {}}";

            string filePath = "/HelloWorld.cs";
            string fileHash = Sha256.ComputeHash(fileContent);

            newBundleFiles.Add(filePath, fileHash);

            var createdBundle = await snykCodeClient.CreateBundle(newBundleFiles);

            Assert.NotNull(createdBundle);
            Assert.True(!string.IsNullOrEmpty(createdBundle.Id));

            var codeFiles = new Dictionary<string, string>();

            bool isSuccess = await snykCodeClient.UploadFile(createdBundle.Id, fileHash, fileContent);

            Assert.True(isSuccess);
        }

        [Fact]
        public async Task SnykCodeClient_CreateBundleSmallPayloadProvided_ChecksPassAsync()
        {
            var files = new Dictionary<string, string>();

            for (int i = 0; i < 10; i++)
            {
                string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest" + i + ".cs";

                files.Add(fileName, Sha256.ComputeHash(fileName));
            }

            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var uploadedBundle = await snykCodeClient.CreateBundle(files);

            Assert.NotNull(uploadedBundle);
            Assert.True(!string.IsNullOrEmpty(uploadedBundle.Id));
            Assert.True(uploadedBundle.MissingFiles.Length == 10);
        }

        [Fact]
        public async Task SnykCodeClient_ExtendBundleAddFilesProvied_ChecksPassAsync()
        {
            var files = new Dictionary<string, string>();

            files.Add("/Test1.cs", Sha256.ComputeHash("/Test1.cs"));
            files.Add("/Test2.cs", Sha256.ComputeHash("/Test2.cs"));
            files.Add("/Test3.cs", Sha256.ComputeHash("/Test3.cs"));

            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var resultBundleDto = await snykCodeClient.CreateBundle(files);

            Assert.NotNull(resultBundleDto);
            Assert.False(string.IsNullOrEmpty(resultBundleDto.Id));

            var extendFiles = new Dictionary<string, string>();

            extendFiles.Add("/Test4.cs", Sha256.ComputeHash("/Test4.cs"));
            extendFiles.Add("/Test5.cs", Sha256.ComputeHash("/Test5.cs"));
            extendFiles.Add("/Test6.cs", Sha256.ComputeHash("/Test6.cs"));

            BundleResponseDto extendedBundleDto = await snykCodeClient.ExtendBundle(resultBundleDto.Id, extendFiles, new List<string>());

            Assert.NotNull(extendedBundleDto);
            Assert.False(string.IsNullOrEmpty(extendedBundleDto.Id));
            Assert.Equal(6, extendedBundleDto.MissingFiles.Length);
            Assert.False(resultBundleDto.Id == extendedBundleDto.Id);
        }

        [Fact]
        public async Task SnykCodeClient_ExtendBundleRemoveFilesProvied_ChecksPassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Dictionary<string, string> files = new Dictionary<string, string>();

            files.Add("/Test1.cs", Sha256.ComputeHash("/Test1.cs"));
            files.Add("/Test2.cs", Sha256.ComputeHash("/Test2.cs"));
            files.Add("/Test3.cs", Sha256.ComputeHash("/Test3.cs"));

            var resultBundleDto = await snykCodeClient.CreateBundle(files);

            Assert.NotNull(resultBundleDto);
            Assert.False(string.IsNullOrEmpty(resultBundleDto.Id));

            var updateFiles = new Dictionary<string, string>();

            updateFiles.Add("/Test4.cs", Sha256.ComputeHash("/Test4.cs"));
            updateFiles.Add("/Test5.cs", Sha256.ComputeHash("/Test5.cs"));
            updateFiles.Add("/Test6.cs", Sha256.ComputeHash("/Test6.cs"));

            var removedFiles = new List<string>();

            removedFiles.Add("/Test1.cs");
            removedFiles.Add("/Test2.cs");

            BundleResponseDto extendedBundleDto = await snykCodeClient.ExtendBundle(resultBundleDto.Id, updateFiles, removedFiles);

            Assert.NotNull(extendedBundleDto);
            Assert.False(string.IsNullOrEmpty(extendedBundleDto.Id));
            Assert.Equal(4, extendedBundleDto.MissingFiles.Length);
            Assert.False(resultBundleDto.Id == extendedBundleDto.Id);
        }

        [Fact]
        public async Task SnykCodeClient_ProperBundleProvided_CheckBundlePassAsync()
        {
            var files = new Dictionary<string, string>();

            files.Add("/Test.cs", Sha256.ComputeHash("/Test.cs"));

            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var resultBundleDto = await snykCodeClient.CreateBundle(files);

            Assert.NotNull(resultBundleDto);
            Assert.False(string.IsNullOrEmpty(resultBundleDto.Id));

            var checkedBundle = await snykCodeClient.CheckBundle(resultBundleDto.Id);

            Assert.NotNull(checkedBundle);
            Assert.False(string.IsNullOrEmpty(checkedBundle.Id));
        }

        [Fact]
        public void SnykCodeClient_WrongBundleProvided_CheckBundleFailed()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Assert.ThrowsAsync<AggregateException>(() => snykCodeClient.CheckBundle("dummy"));
        }

        [Fact]
        public async Task SnykCodeClient_CreateBundleEmptyFilesInBundleProvided_CheckFailAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var bundleDto = await snykCodeClient.CreateBundle(new Dictionary<string, string>());

            Assert.NotNull(bundleDto);
            Assert.NotEmpty(bundleDto.Id);
            Assert.Empty(bundleDto.MissingFiles);
        }

        [Fact]
        public async Task SnykCodeClient_SimpleFileProvided_CreateBundlePassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var files = new Dictionary<string, string>();

            files.Add("/Test.cs", Sha256.ComputeHash("/Test.cs"));

            var uploadedBundle = await snykCodeClient.CreateBundle(files);

            Assert.NotNull(uploadedBundle);
            Assert.True(!string.IsNullOrEmpty(uploadedBundle.Id));
        }

        [Fact]
        public async Task SnykCodeClient_GetFilters_ChecksPassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            var filters = await snykCodeClient.GetFilters();

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

            Assert.ThrowsAsync<AggregateException>(() => snykCodeClient.LoginAsync("\\{"));
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
