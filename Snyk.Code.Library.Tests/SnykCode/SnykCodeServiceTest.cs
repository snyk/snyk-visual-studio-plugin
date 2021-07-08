namespace Snyk.Code.Library.Tests.Api
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Moq;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto;
    using Xunit;

    /// <summary>
    /// Test cases for <see cref="SnykCodeService"/>.
    /// </summary>
    public class SnykCodeServiceTest
    {
        [Fact]
        public async Task SnykCodeService_FiveCodeFilesProvided_FilterFilesCheckPassAsync()
        {
            IList<string> files = new List<string>();

            files.Add("/home/test/projects/main/app.js");
            files.Add("/home/test/projects/main/app.js.dev");
            files.Add("/home/test/projects/main/main.ts");
            files.Add("C:\\projects\\main\\Db.cs");
            files.Add("C:\\projects\\Db.cs.dev");

            var codeClientMock = new Mock<ISnykCodeClient>();

            var codeService = new SnykCodeService(codeClientMock.Object);

            var extensions = new List<string>();
            extensions.Add(".cs");
            extensions.Add(".ts");
            extensions.Add(".js");

            var filtersDto = new FiltersDto
            {
                Extensions = extensions,
                ConfigFiles = new List<string>(),
            };

            codeClientMock
                .Setup(codeClient => codeClient.GetFiltersAsync().Result)
                .Returns(filtersDto);

            var filteredFiles = await codeService.FilterFilesAsync(files);

            Assert.NotNull(filteredFiles);
            Assert.Equal(3, filteredFiles.Count);

            Assert.True(filteredFiles.Contains("/home/test/projects/main/app.js"));
            Assert.True(filteredFiles.Contains("/home/test/projects/main/main.ts"));
            Assert.True(filteredFiles.Contains("C:\\projects\\main\\Db.cs"));

            codeClientMock.Verify(codeClient => codeClient.GetFiltersAsync());
        }

        [Fact]
        public async Task SnykCodeService_FiveConfigFilesProvided_FilterFilesCheckPassAsync()
        {
            IList<string> files = new List<string>();

            files.Add("/home/test/projects/main/.dcignore");
            files.Add("/home/test/projects/main/.gitignore");
            files.Add("/home/test/projects/main/main.ts.dev");
            files.Add("C:\\projects\\main\\Db.cs.dev");
            files.Add("C:\\projects\\.gitignore");

            var codeClientMock = new Mock<ISnykCodeClient>();

            var codeService = new SnykCodeService(codeClientMock.Object);

            var configFiles = new List<string>();
            configFiles.Add(".dcignore");
            configFiles.Add(".gitignore");

            var filtersDto = new FiltersDto
            {
                Extensions = new List<string>(),
                ConfigFiles = configFiles,
            };

            codeClientMock
                .Setup(codeClient => codeClient.GetFiltersAsync().Result)
                .Returns(filtersDto);

            var filteredFiles = await codeService.FilterFilesAsync(files);

            Assert.NotNull(filteredFiles);
            Assert.Equal(3, filteredFiles.Count);

            Assert.True(filteredFiles.Contains("/home/test/projects/main/.dcignore"));
            Assert.True(filteredFiles.Contains("/home/test/projects/main/.gitignore"));
            Assert.True(filteredFiles.Contains("C:\\projects\\.gitignore"));

            codeClientMock.Verify(codeClient => codeClient.GetFiltersAsync());
        }

        [Fact]
        public async Task SnykCodeService_FveCodeFilesAndFiveConfigFilesProvided_FilterFilesCheckPassAsync()
        {
            IList<string> files = new List<string>();

            files.Add("/home/test/projects/main/.dcignore");
            files.Add("/home/test/projects/main/.gitignore");
            files.Add("/home/test/projects/main/main.ts.dev");
            files.Add("C:\\projects\\main\\Db.cs.dev");
            files.Add("C:\\projects\\.gitignore");
            files.Add("/home/test/projects/main/app.js");
            files.Add("/home/test/projects/main/app.js.dev");
            files.Add("/home/test/projects/main/main.ts");
            files.Add("C:\\projects\\main\\Db.cs");
            files.Add("C:\\projects\\Db.cs.dev");

            var codeClientMock = new Mock<ISnykCodeClient>();

            var codeService = new SnykCodeService(codeClientMock.Object);

            var extensions = new List<string>();
            extensions.Add(".cs");
            extensions.Add(".ts");
            extensions.Add(".js");

            var configFiles = new List<string>();
            configFiles.Add(".dcignore");
            configFiles.Add(".gitignore");

            var filtersDto = new FiltersDto
            {
                Extensions = extensions,
                ConfigFiles = configFiles,
            };

            codeClientMock
                .Setup(codeClient => codeClient.GetFiltersAsync().Result)
                .Returns(filtersDto);

            var filteredFiles = await codeService.FilterFilesAsync(files);

            Assert.NotNull(filteredFiles);
            Assert.Equal(6, filteredFiles.Count);

            Assert.True(filteredFiles.Contains("/home/test/projects/main/app.js"));
            Assert.True(filteredFiles.Contains("/home/test/projects/main/main.ts"));
            Assert.True(filteredFiles.Contains("C:\\projects\\main\\Db.cs"));

            Assert.True(filteredFiles.Contains("/home/test/projects/main/.dcignore"));
            Assert.True(filteredFiles.Contains("/home/test/projects/main/.gitignore"));
            Assert.True(filteredFiles.Contains("C:\\projects\\.gitignore"));

            codeClientMock.Verify(codeClient => codeClient.GetFiltersAsync());
        }
    }
}
