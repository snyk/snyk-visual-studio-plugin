namespace Snyk.Code.Library.Tests.Api
{
    using System.Collections.Generic;
    using System.IO;
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

            string tempPath = this.GetTemporaryDirectory();

            files.Add($"{tempPath}\\projects\\main\\app.js");
            files.Add($"{tempPath}\\projects\\main\\app.js.dev");
            files.Add($"{tempPath}\\projects\\main\\main.ts");
            files.Add($"{tempPath}\\projects\\main\\Db.cs");
            files.Add($"{tempPath}\\projects\\Db.cs.dev");

            Directory.CreateDirectory($"{tempPath}\\projects");
            Directory.CreateDirectory($"{tempPath}\\projects\\main");

            foreach (string file in files)
            {
                File.Create(file);
            }

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

            Assert.True(filteredFiles.Contains($"{tempPath}\\projects\\main\\app.js"));
            Assert.True(filteredFiles.Contains($"{tempPath}\\projects\\main\\main.ts"));
            Assert.True(filteredFiles.Contains($"{tempPath}\\projects\\main\\Db.cs"));

            codeClientMock.Verify(codeClient => codeClient.GetFiltersAsync());
        }

        [Fact]
        public async Task FiltersService_FiveConfigFilesProvided_FilterFilesCheckPassAsync()
        {
            IList<string> files = new List<string>();

            string tempPath = this.GetTemporaryDirectory();

            files.Add($"{tempPath}\\projects\\main\\.dcignore");
            files.Add($"{tempPath}\\projects\\main\\.gitignore");
            files.Add($"{tempPath}\\projects\\main\\main.ts.dev");
            files.Add($"{tempPath}\\projects\\main\\Db.cs.dev");
            files.Add($"{tempPath}\\projects\\.gitignore");

            Directory.CreateDirectory($"{tempPath}\\projects");
            Directory.CreateDirectory($"{tempPath}\\projects\\main");

            foreach (string file in files)
            {
                File.Create(file);
            }

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
            Assert.Empty(filteredFiles);

            codeClientMock.Verify(codeClient => codeClient.GetFiltersAsync());
        }

        [Fact]
        public async Task FiltersService_FiveCodeFilesAndFiveConfigFilesProvided_FilterFilesCheckPassAsync()
        {
            IList<string> files = new List<string>();

            string tempPath = this.GetTemporaryDirectory();

            files.Add($"{tempPath}\\projects\\main\\.dcignore");
            files.Add($"{tempPath}\\projects/main\\.gitignore");
            files.Add($"{tempPath}\\projects\\main\\main.ts.dev");
            files.Add($"{tempPath}\\projects\\main\\Db.cs.dev");
            files.Add($"{tempPath}\\projects\\.gitignore");
            files.Add($"{tempPath}\\projects\\main\\app.js");
            files.Add($"{tempPath}\\projects\\main\\app.js.dev");
            files.Add($"{tempPath}\\projects\\main\\main.ts");
            files.Add($"{tempPath}\\projects\\main\\Db.cs");
            files.Add($"{tempPath}\\projects\\Db.cs.dev");

            Directory.CreateDirectory($"{tempPath}\\projects");
            Directory.CreateDirectory($"{tempPath}\\projects\\main");

            foreach (string file in files)
            {
                File.Create(file);
            }

            var codeClientMock = new Mock<ISnykCodeClient>();

            var filtersDto = GetFiltersDto();

            codeClientMock
                .Setup(codeClient => codeClient.GetFiltersAsync().Result)
                .Returns(filtersDto);

            var filterService = new FiltersService(codeClientMock.Object);

            var filteredFiles = await filterService.FilterFilesAsync(files);

            Assert.NotNull(filteredFiles);
            Assert.Equal(3, filteredFiles.Count);

            Assert.True(filteredFiles.Contains($"{tempPath}\\projects\\main\\app.js"));
            Assert.True(filteredFiles.Contains($"{tempPath}\\projects\\main\\main.ts"));
            Assert.True(filteredFiles.Contains($"{tempPath}\\projects\\main\\Db.cs"));

            codeClientMock.Verify(codeClient => codeClient.GetFiltersAsync());
        }

        [Fact]
        public async Task FilterServices_OversizedFile_FileIsIgnored()
        {
            // Arrange
            const int fileSizeLimit = 1_000_000;
            var smallFileSize = fileSizeLimit / 2;
            var largeFileSize = fileSizeLimit * 2;
            var codeClientMock = new Mock<ISnykCodeClient>();
            codeClientMock.Setup(codeClient => codeClient.GetFiltersAsync())
                .ReturnsAsync(GetFiltersDto());

            var filtersService = new FiltersService(codeClientMock.Object);

            using (var smallFile = TempFile.Create("smallFile.cs", smallFileSize))
            using (var largeFile = TempFile.Create("largeFile.cs", largeFileSize))
            {
                // Act
                var filteredFiles = await filtersService.FilterFilesAsync(new[] { smallFile.FilePath, largeFile.FilePath });

                // Assert
                Assert.Equal(1, filteredFiles.Count);
                Assert.Contains(smallFile.FilePath, filteredFiles);
                Assert.DoesNotContain(largeFile.FilePath, filteredFiles);
            }
        }

        private string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            Directory.CreateDirectory(tempDirectory);

            return tempDirectory;
        }

        private static FiltersDto GetFiltersDto()
        {
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
            return filtersDto;
        }
    }
}
