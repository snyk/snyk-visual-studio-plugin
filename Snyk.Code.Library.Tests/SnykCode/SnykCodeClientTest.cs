namespace Snyk.Code.Library.Tests.Api
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;
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
        public async Task UploadFilesProvided_ChecksPassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            string fileContent1 = "namespace HelloWorld {public class HelloWorld {}}";
            string filePath1 = "/HelloWorld.cs";
            string fileHash1 = Sha256.ComputeHash(fileContent1);

            newBundle.Files.Add(filePath1, fileHash1);

            string fileContent2 = "namespace HelloWorld {public class HelloWorldTest {}}";
            string filePath2 = "/HelloWorldTest.cs";
            string fileHash2 = Sha256.ComputeHash(fileContent2);

            newBundle.Files.Add(filePath2, fileHash2);

            string fileContent3 = "namespace HelloWorld {public class HelloWorldService {}}";
            string filePath3 = "/HelloWorldService.cs";
            string fileHash3 = Sha256.ComputeHash(fileContent3);

            newBundle.Files.Add(filePath3, fileHash3);

            var createdBundle = await snykCodeClient.CreateBundle(newBundle);

            Assert.NotNull(createdBundle);
            Assert.True(!string.IsNullOrEmpty(createdBundle.Id));

            List<CodeFile> codeFiles = new List<CodeFile>();

            codeFiles.Add(new CodeFile
            {
                Hash = fileHash1,
                Content = fileContent1,
            });

            codeFiles.Add(new CodeFile
            {
                Hash = fileHash2,
                Content = fileContent2,
            });

            codeFiles.Add(new CodeFile
            {
                Hash = fileHash3,
                Content = fileContent3,
            });

            bool isSuccess = await snykCodeClient.UploadFiles(createdBundle.Id, codeFiles);

            Assert.True(isSuccess);
        }

        [Fact]
        public async Task UploadFileProvided_ChecksPassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            string fileContent = "namespace HelloWorld {public class HelloWorld {}}";

            string filePath = "/HelloWorld.cs";
            string fileHash = Sha256.ComputeHash(fileContent);

            newBundle.Files.Add(filePath, fileHash);

            var createdBundle = await snykCodeClient.CreateBundle(newBundle);

            Assert.NotNull(createdBundle);
            Assert.True(!string.IsNullOrEmpty(createdBundle.Id));

            CodeFile codeFile = new CodeFile
            {
                Hash = fileHash,
                Content = fileContent,
            };

            bool isSuccess = await snykCodeClient.UploadFile(createdBundle.Id, codeFile);

            Assert.True(isSuccess);
        }

        [Fact]
        public async Task SnykCodeClient_CreateBundleSmallPayloadProvided_ChecksPassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            for (int i = 0; i < 10; i++)
            {
                string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest" + i + ".cs";

                newBundle.Files.Add(fileName, Sha256.ComputeHash(fileName));
            }

            var uploadedBundle = await snykCodeClient.CreateBundle(newBundle);

            Assert.NotNull(uploadedBundle);
            Assert.True(!string.IsNullOrEmpty(uploadedBundle.Id));
            Assert.True(uploadedBundle.MissingFiles.Length == 10);
        }

        [Fact]
        public async Task SnykCodeClient_ExtendBundleAddFilesProvied_ChecksPassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            newBundle.Files.Add("/Test1.cs", Sha256.ComputeHash("/Test1.cs"));
            newBundle.Files.Add("/Test2.cs", Sha256.ComputeHash("/Test2.cs"));
            newBundle.Files.Add("/Test3.cs", Sha256.ComputeHash("/Test3.cs"));

            Bundle resultBundle = await snykCodeClient.CreateBundle(newBundle);

            Assert.NotNull(resultBundle);
            Assert.False(string.IsNullOrEmpty(resultBundle.Id));

            Bundle extendBundle = new Bundle();

            extendBundle.Files.Add("/Test4.cs", Sha256.ComputeHash("/Test4.cs"));
            extendBundle.Files.Add("/Test5.cs", Sha256.ComputeHash("/Test5.cs"));
            extendBundle.Files.Add("/Test6.cs", Sha256.ComputeHash("/Test6.cs"));

            Bundle extendedBundle = await snykCodeClient.ExtendBundle(resultBundle, extendBundle);

            Assert.NotNull(extendedBundle);
            Assert.False(string.IsNullOrEmpty(extendedBundle.Id));
            Assert.Equal(6, extendedBundle.MissingFiles.Length);
            Assert.False(newBundle.Id == extendedBundle.Id);
        }

        [Fact]
        public async Task SnykCodeClient_ExtendBundleRemoveFilesProvied_ChecksPassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            newBundle.Files.Add("/Test1.cs", Sha256.ComputeHash("/Test1.cs"));
            newBundle.Files.Add("/Test2.cs", Sha256.ComputeHash("/Test2.cs"));
            newBundle.Files.Add("/Test3.cs", Sha256.ComputeHash("/Test3.cs"));

            Bundle resultBundle = await snykCodeClient.CreateBundle(newBundle);

            Assert.NotNull(resultBundle);
            Assert.False(string.IsNullOrEmpty(resultBundle.Id));

            Bundle extendBundle = new Bundle();

            extendBundle.Files.Add("/Test4.cs", Sha256.ComputeHash("/Test4.cs"));
            extendBundle.Files.Add("/Test5.cs", Sha256.ComputeHash("/Test5.cs"));
            extendBundle.Files.Add("/Test6.cs", Sha256.ComputeHash("/Test6.cs"));

            extendBundle.RemovedFiles.Add("/Test1.cs");
            extendBundle.RemovedFiles.Add("/Test2.cs");

            Bundle extendedBundle = await snykCodeClient.ExtendBundle(resultBundle, extendBundle);

            Assert.NotNull(extendedBundle);
            Assert.False(string.IsNullOrEmpty(extendedBundle.Id));
            Assert.Equal(4, extendedBundle.MissingFiles.Length);
            Assert.False(newBundle.Id == extendedBundle.Id);
        }

        [Fact]
        public async Task SnykCodeClient_ProperBundleProvided_CheckBundlePassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            newBundle.Files.Add("/Test.cs", Sha256.ComputeHash("/Test.cs"));

            Bundle uploadedBundle = await snykCodeClient.CreateBundle(newBundle);

            Assert.NotNull(uploadedBundle);
            Assert.False(string.IsNullOrEmpty(uploadedBundle.Id));

            Bundle checkedBundle = await snykCodeClient.CheckBundle(uploadedBundle);

            Assert.NotNull(checkedBundle);
            Assert.False(string.IsNullOrEmpty(checkedBundle.Id));
        }

        [Fact]
        public void SnykCodeClient_WrongBundleProvided_CheckBundleFailed()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle dummyBundle = new Bundle
            {
                Id = "dummy",
            };

            Assert.ThrowsAsync<AggregateException>(() => snykCodeClient.CheckBundle(dummyBundle));
        }

        [Fact]
        public async Task SnykCodeClient_CreateBundleEmptyFilesInBundleProvided_CheckFailAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            var uploadedBundle = await snykCodeClient.CreateBundle(newBundle);

            Assert.NotNull(uploadedBundle);
            Assert.NotEmpty(uploadedBundle.Id);
            Assert.Empty(uploadedBundle.MissingFiles);
        }

        [Fact]
        public async Task SnykCodeClient_SimpleFileProvided_CreateBundlePassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            newBundle.Files.Add("/Test.cs", Sha256.ComputeHash("/Test.cs"));

            var uploadedBundle = await snykCodeClient.CreateBundle(newBundle);

            Assert.NotNull(uploadedBundle);
            Assert.True(!string.IsNullOrEmpty(uploadedBundle.Id));
        }

        [Fact]
        public async Task SnykCodeClient_GetFilters_ChecksPassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Filters filters = await snykCodeClient.GetFilters();

            Assert.NotNull(filters);
            Assert.NotNull(filters.Extensions);
            Assert.NotNull(filters.ConfigFiles);
        }

        [Fact]
        public async Task SnykCodeClient_ProperLoginDataProvided_ChecksPassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);
            
            LoginResponse response = await snykCodeClient.LoginAsync(TestUserAgent);

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

            LoginStatus status = await snykCodeClient.CheckSessionAsync();

            Assert.True(status.IsSucccess);
        }

        private string GetFileContents(string resourceFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceFilePath = $"Resources.{resourceFileName}";

            using (var inputStream = assembly.GetEmbeddedResourceStream(resourceFilePath))
            {
                if (inputStream != null)
                {
                    var streamReader = new StreamReader(inputStream);

                    return streamReader.ReadToEnd();
                }
            }

            return String.Empty;
        }
    }

    static class AssemblyExtensions
    {
        public static Stream GetEmbeddedResourceStream(this Assembly assembly, string relativeResourcePath)
        {
            if (string.IsNullOrEmpty(relativeResourcePath))
            {
                throw new ArgumentNullException("relativeResourcePath");
            }

            var resourcePath = string.Format("{0}.{1}",
                Regex.Replace(assembly.ManifestModule.Name, @"\.(exe|dll)$",
                      string.Empty, RegexOptions.IgnoreCase), relativeResourcePath);

            var stream = assembly.GetManifestResourceStream(resourcePath);

            if (stream == null)
            {
                throw new ArgumentException(String.Format("The specified embedded resource \"{0}\" is not found.", relativeResourcePath));
            }

            return stream;
        }
    }
}
