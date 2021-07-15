namespace Snyk.Code.Library.Tests.Api
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Moq;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Api.Dto;
    using Snyk.Code.Library.Service;
    using Xunit;

    public class FiltersServiceTest
    {
        [Fact]
        public async Task FiltersService_FiveCodeFilesProvided_FilterFilesCheckPassAsync()
        {
            IList<string> files = new List<string>();

            files.Add("/home/test/projects/main/app.js");
            files.Add("/home/test/projects/main/app.js.dev");
            files.Add("/home/test/projects/main/main.ts");
            files.Add("C:\\projects\\main\\Db.cs");
            files.Add("C:\\projects\\Db.cs.dev");

            var codeClientMock = new Mock<ISnykCodeClient>();

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

            var filterService = new FiltersService(codeClientMock.Object);

            var filteredFiles = await filterService.FilterFilesAsync(files);

            Assert.NotNull(filteredFiles);
            Assert.Equal(3, filteredFiles.Count);

            Assert.True(filteredFiles.Contains("/home/test/projects/main/app.js"));
            Assert.True(filteredFiles.Contains("/home/test/projects/main/main.ts"));
            Assert.True(filteredFiles.Contains("C:\\projects\\main\\Db.cs"));

            codeClientMock.Verify(codeClient => codeClient.GetFiltersAsync());
        }

        [Fact]
        public async Task FiltersService_FiveConfigFilesProvided_FilterFilesCheckPassAsync()
        {
            IList<string> files = new List<string>();

            files.Add("/home/test/projects/main/.dcignore");
            files.Add("/home/test/projects/main/.gitignore");
            files.Add("/home/test/projects/main/main.ts.dev");
            files.Add("C:\\projects\\main\\Db.cs.dev");
            files.Add("C:\\projects\\.gitignore");

            var configFiles = new List<string>();
            configFiles.Add(".dcignore");
            configFiles.Add(".gitignore");

            var filtersDto = new FiltersDto
            {
                Extensions = new List<string>(),
                ConfigFiles = configFiles,
            };

            var codeClientMock = new Mock<ISnykCodeClient>();

            codeClientMock
                .Setup(codeClient => codeClient.GetFiltersAsync().Result)
                .Returns(filtersDto);

            var filterService = new FiltersService(codeClientMock.Object);

            var filteredFiles = await filterService.FilterFilesAsync(files);

            Assert.NotNull(filteredFiles);
            Assert.Equal(3, filteredFiles.Count);

            Assert.True(filteredFiles.Contains("/home/test/projects/main/.dcignore"));
            Assert.True(filteredFiles.Contains("/home/test/projects/main/.gitignore"));
            Assert.True(filteredFiles.Contains("C:\\projects\\.gitignore"));

            codeClientMock.Verify(codeClient => codeClient.GetFiltersAsync());
        }

        [Fact]
        public async Task FiltersService_FveCodeFilesAndFiveConfigFilesProvided_FilterFilesCheckPassAsync()
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

            var filterService = new FiltersService(codeClientMock.Object);

            var filteredFiles = await filterService.FilterFilesAsync(files);

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
