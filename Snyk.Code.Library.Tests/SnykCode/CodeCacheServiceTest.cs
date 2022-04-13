namespace Snyk.Code.Library.Tests.SnykCode
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Moq;
    using Snyk.Code.Library.Service;
    using Snyk.Common;
    using Xunit;

    /// <summary>
    /// Test case for <see cref="ICodeCacheService"/>.
    /// </summary>
    public class CodeCacheServiceTest : IDisposable
    {
        private string projectPath;
        private string appCsPath;

        public CodeCacheServiceTest()
        {
            this.projectPath = Path.Combine(Path.GetTempPath(), "CodeCacheService_Tests_Project");

            if (Directory.Exists(this.projectPath))
            {
                Directory.Delete(this.projectPath, true);
            }

            Directory.CreateDirectory(this.projectPath);

            this.appCsPath = Path.Combine(this.projectPath, "App.cs");

            File.WriteAllText(this.appCsPath, "namespace Tests {}");
        }

        public void Dispose() => Directory.Delete(this.projectPath, true);

        [Fact]
        public void CodeCacheService_CodeCacheExists_RemoveDeletedFiles()
        {
            var solutionServiceMock = new Mock<ISolutionService>();

            solutionServiceMock
                .Setup(solutionService => solutionService.GetPathAsync().Result)
                .Returns(this.projectPath);

            solutionServiceMock
                .Setup(solutionService => solutionService.GetFilesAsync().Result)
                .Returns(new List<string>());

            var fileProvider = new SnykCodeFileProvider(solutionServiceMock.Object);
            var codeCacheService = new CodeCacheService(fileProvider);

            codeCacheService.Initialize(new List<string> { this.appCsPath });

            Assert.Single(codeCacheService.GetFilePathToHashDictionary());

            fileProvider.RemoveFile(this.appCsPath);

            codeCacheService.Update(fileProvider);

            Assert.Empty(codeCacheService.GetFilePathToHashDictionary());
        }

        [Fact]
        public void CodeCacheService_CodeCacheExists_ValidCache()
        {
            var solutionServiceMock = new Mock<ISolutionService>();

            solutionServiceMock
                .Setup(solutionService => solutionService.GetPathAsync().Result)
                .Returns(this.projectPath);

            var fileProvider = new SnykCodeFileProvider(solutionServiceMock.Object);

            var codeCacheService = new CodeCacheService(fileProvider);

            codeCacheService.SetAnalysisResult(new Domain.Analysis.AnalysisResult());

            Assert.True(codeCacheService.IsCacheValid());
        }

        [Fact]
        public void CodeCacheService_CodeCacheExists_InvalidCache()
        {
            var solutionServiceMock = new Mock<ISolutionService>();

            solutionServiceMock
                .Setup(solutionService => solutionService.GetPathAsync().Result)
                .Returns(this.projectPath);

            solutionServiceMock
                .Setup(solutionService => solutionService.GetFilesAsync().Result)
                .Returns(new List<string>());

            var fileProvider = new SnykCodeFileProvider(solutionServiceMock.Object);
            var codeCacheService = new CodeCacheService(fileProvider);

            codeCacheService.SetAnalysisResult(new Domain.Analysis.AnalysisResult());

            Assert.True(codeCacheService.IsCacheValid());

            fileProvider.AddChangedFile(this.appCsPath);

            codeCacheService.Update(fileProvider);

            Assert.False(codeCacheService.IsCacheValid());
        }

        [Fact]
        public void CodeCacheService_UpdateFile_CacheUpdated()
        {
            var solutionServiceMock = new Mock<ISolutionService>();
            var filtersServiceMock = new Mock<IFiltersService>();

            solutionServiceMock
                .Setup(solutionService => solutionService.GetPathAsync().Result)
                .Returns(this.projectPath);

            solutionServiceMock
                .Setup(solutionService => solutionService.GetFilesAsync().Result)
                .Returns(new List<string>());

            var fileProvider = new SnykCodeFileProvider(solutionServiceMock.Object);
            var codeCacheService = new CodeCacheService(fileProvider);

            codeCacheService.Initialize(new List<string> { this.appCsPath });

            string oldFileContent = codeCacheService.GetFileHashToContentDictionary()["/App.cs"];

            File.WriteAllText(this.appCsPath, "namespace Tests { public class AppTest {} }");

            fileProvider.AddChangedFile(this.appCsPath);

            codeCacheService.Update(fileProvider);

            string newFileContent = codeCacheService.GetFileHashToContentDictionary()["/App.cs"];

            Assert.NotEqual(oldFileContent, newFileContent);
        }

        [Fact]
        public void CodeCacheService_AddFiles_CacheContainsFiles()
        {
            string app2CsPath = Path.Combine(this.projectPath, "App2.cs");
            File.WriteAllText(app2CsPath, "namespace Tests2 {}");

            var solutionServiceMock = new Mock<ISolutionService>();
            var filtersServiceMock = new Mock<IFiltersService>();

            solutionServiceMock
                .Setup(solutionService => solutionService.GetPathAsync().Result)
                .Returns(this.projectPath);

            solutionServiceMock
                .Setup(solutionService => solutionService.GetFilesAsync().Result)
                .Returns(new List<string>());

            var fileProvider = new SnykCodeFileProvider(solutionServiceMock.Object);
            var codeCacheService = new CodeCacheService(fileProvider);

            codeCacheService.Initialize(new List<string> { this.appCsPath, app2CsPath });

            Assert.NotNull(codeCacheService.GetFileHashToContentDictionary()["/App.cs"]);
            Assert.NotNull(codeCacheService.GetFileHashToContentDictionary()["/App2.cs"]);
        }
    }
}
