namespace Snyk.Code.Library.Tests.SnykCode
{
    using System.Collections.Generic;
    using System.IO;
    using Moq;
    using Snyk.Code.Library.Service;
    using Snyk.Common;
    using Xunit;

    /// <summary>
    /// Test case for <see cref="ICodeCacheService"/>.
    /// </summary>
    public class CodeCacheServiceTest
    {
        [Fact]
        public void CodeCacheService_CodeCacheExists_RemoveDeletedFiles()
        {
            string projectPath = Path.Combine(Path.GetTempPath(), "CodeCacheService_CodeCacheExists_CollectDeletedFiles");

            if (Directory.Exists(projectPath))
            {
                Directory.Delete(projectPath, true);
            }

            Directory.CreateDirectory(projectPath);

            string appCsPath = Path.Combine(projectPath, "App.cs");

            File.WriteAllText(appCsPath, "namespace Tests {}");

            var solutionServiceMock = new Mock<ISolutionService>();

            solutionServiceMock
                .Setup(solutionService => solutionService.GetPath())
                .Returns(projectPath);

            solutionServiceMock
                .Setup(solutionService => solutionService.GetFiles())
                .Returns(new List<string>());

            var fileProvider = new SnykCodeFileProvider(solutionServiceMock.Object);
            var codeCacheService = new CodeCacheService(fileProvider.GetSolutionPath());

            codeCacheService.Initialize(new List<string> { appCsPath });

            Assert.Single(codeCacheService.GetFilePathToHashDictionary());

            fileProvider.RemoveFile(appCsPath);

            codeCacheService.Update(fileProvider);

            Assert.Empty(codeCacheService.GetFilePathToHashDictionary());

            Directory.Delete(projectPath, true);
        }

        [Fact]
        public void CodeCacheService_CodeCacheExists_ValidCache()
        {
            string projectPath = Path.Combine(Path.GetTempPath(), "CodeCacheService_CodeCacheExists_ValidCache");

            var solutionServiceMock = new Mock<ISolutionService>();

            solutionServiceMock
                .Setup(solutionService => solutionService.GetPath())
                .Returns(projectPath);

            var fileProvider = new SnykCodeFileProvider(solutionServiceMock.Object);

            var codeCacheService = new CodeCacheService(fileProvider.GetSolutionPath());

            codeCacheService.SetAnalysisResult(new Domain.Analysis.AnalysisResult());

            Assert.True(codeCacheService.CacheValid());
        }

        [Fact]
        public void CodeCacheService_CodeCacheExists_InvalidCache()
        {
            string projectPath = Path.Combine(Path.GetTempPath(), "CodeCacheService_CodeCacheExists_InvalidCache");

            if (Directory.Exists(projectPath))
            {
                Directory.Delete(projectPath, true);
            }

            Directory.CreateDirectory(projectPath);

            string appCsPath = Path.Combine(projectPath, "App.cs");

            File.WriteAllText(appCsPath, "namespace Tests {}");

            var solutionServiceMock = new Mock<ISolutionService>();

            solutionServiceMock
                .Setup(solutionService => solutionService.GetPath())
                .Returns(projectPath);

            solutionServiceMock
                .Setup(solutionService => solutionService.GetFiles())
                .Returns(new List<string>());

            var fileProvider = new SnykCodeFileProvider(solutionServiceMock.Object);
            var codeCacheService = new CodeCacheService(fileProvider.GetSolutionPath());

            codeCacheService.SetAnalysisResult(new Domain.Analysis.AnalysisResult());

            Assert.True(codeCacheService.CacheValid());

            fileProvider.AddNewFile(appCsPath);

            codeCacheService.Update(fileProvider);

            Assert.False(codeCacheService.CacheValid());

            Directory.Delete(projectPath, true);
        }

        [Fact]
        public void CodeCacheService_UpdateFile_CacheUpdated()
        {
            string projectPath = Path.Combine(Path.GetTempPath(), "CodeCacheService_UpdateFile_CacheUpdated");

            if (Directory.Exists(projectPath))
            {
                Directory.Delete(projectPath, true);
            }

            Directory.CreateDirectory(projectPath);

            string appCsPath = Path.Combine(projectPath, "App.cs");

            File.WriteAllText(appCsPath, "namespace Tests {}");

            var solutionServiceMock = new Mock<ISolutionService>();
            var filtersServiceMock = new Mock<IFiltersService>();

            solutionServiceMock
                .Setup(solutionService => solutionService.GetPath())
                .Returns(projectPath);

            solutionServiceMock
                .Setup(solutionService => solutionService.GetFiles())
                .Returns(new List<string>());

            var fileProvider = new SnykCodeFileProvider(solutionServiceMock.Object);
            var codeCacheService = new CodeCacheService(fileProvider.GetSolutionPath());

            codeCacheService.Initialize(new List<string> { appCsPath });

            string oldFileContent = codeCacheService.GetFileHashToContentDictionary()["/App.cs"];

            File.WriteAllText(appCsPath, "namespace Tests { public class AppTest {} }");

            fileProvider.AddChangedFile(appCsPath);

            codeCacheService.Update(fileProvider);

            string newFileContent = codeCacheService.GetFileHashToContentDictionary()["/App.cs"];

            Assert.NotEqual(oldFileContent, newFileContent);

            Directory.Delete(projectPath, true);
        }

        [Fact]
        public void CodeCacheService_AddFiles_CacheContainsFiles()
        {
            string projectPath = Path.Combine(Path.GetTempPath(), "CodeCacheService_AddFiles_CacheContainsFiles");

            if (Directory.Exists(projectPath))
            {
                Directory.Delete(projectPath, true);
            }

            Directory.CreateDirectory(projectPath);

            string app1CsPath = Path.Combine(projectPath, "App1.cs");
            string app2CsPath = Path.Combine(projectPath, "App2.cs");

            File.WriteAllText(app1CsPath, "namespace Tests1 {}");
            File.WriteAllText(app2CsPath, "namespace Tests2 {}");

            var solutionServiceMock = new Mock<ISolutionService>();
            var filtersServiceMock = new Mock<IFiltersService>();

            solutionServiceMock
                .Setup(solutionService => solutionService.GetPath())
                .Returns(projectPath);

            solutionServiceMock
                .Setup(solutionService => solutionService.GetFiles())
                .Returns(new List<string>());

            var fileProvider = new SnykCodeFileProvider(solutionServiceMock.Object);
            var codeCacheService = new CodeCacheService(fileProvider.GetSolutionPath());

            codeCacheService.Initialize(new List<string> { app1CsPath, app2CsPath });

            Assert.NotNull(codeCacheService.GetFileHashToContentDictionary()["/App1.cs"]);
            Assert.NotNull(codeCacheService.GetFileHashToContentDictionary()["/App2.cs"]);

            Directory.Delete(projectPath, true);
        }
    }
}
