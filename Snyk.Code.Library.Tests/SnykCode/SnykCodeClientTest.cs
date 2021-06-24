namespace Snyk.Code.Library.Tests.SnykCode
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Snyk.Code.Library.SnykCode;    
    using Xunit;

    public class SnykCodeClientTest
    {
        private const string TestUserAgent = "Test-VisualStudio";             

        [Fact]
        public async Task SnykCodeClient_CreateBundleSmallPayloadProvided_ChecksPassAsync()
        {
            var snykCodeClient = new SnykCodeClient(TestSettings.SnykCodeApiUrl, TestSettings.Instance.ApiToken);

            Bundle newBundle = new Bundle();

            for (int i = 0; i < 10; i++)
            {
                string fileName = "/Snyk/Code/Tests/SnykCodeBigBundleTest" + i + ".cs";

                newBundle.Files.Add(fileName, fileName.GetHashCode().ToString());
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

            newBundle.Files.Add("/Test1.cs", "/Test1.cs".GetHashCode().ToString());
            newBundle.Files.Add("/Test2.cs", "/Test2.cs".GetHashCode().ToString());
            newBundle.Files.Add("/Test3.cs", "/Test3.cs".GetHashCode().ToString());

            Bundle resultBundle = await snykCodeClient.CreateBundle(newBundle);

            Assert.NotNull(resultBundle);
            Assert.False(string.IsNullOrEmpty(resultBundle.Id));

            Bundle extendBundle = new Bundle();

            extendBundle.Files.Add("/Test4.cs", "/Test4.cs".GetHashCode().ToString());
            extendBundle.Files.Add("/Test5.cs", "/Test5.cs".GetHashCode().ToString());
            extendBundle.Files.Add("/Test6.cs", "/Test6.cs".GetHashCode().ToString());

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

            newBundle.Files.Add("/Test1.cs", "/Test1.cs".GetHashCode().ToString());
            newBundle.Files.Add("/Test2.cs", "/Test2.cs".GetHashCode().ToString());
            newBundle.Files.Add("/Test3.cs", "/Test3.cs".GetHashCode().ToString());

            Bundle resultBundle = await snykCodeClient.CreateBundle(newBundle);

            Assert.NotNull(resultBundle);
            Assert.False(string.IsNullOrEmpty(resultBundle.Id));

            Bundle extendBundle = new Bundle();

            extendBundle.Files.Add("/Test4.cs", "/Test4.cs".GetHashCode().ToString());
            extendBundle.Files.Add("/Test5.cs", "/Test5.cs".GetHashCode().ToString());
            extendBundle.Files.Add("/Test6.cs", "/Test6.cs".GetHashCode().ToString());

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

            newBundle.Files.Add("/Test.cs", "/Test.cs".GetHashCode().ToString());

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

            newBundle.Files.Add("/Test.cs", "/Test.cs".GetHashCode().ToString());

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
