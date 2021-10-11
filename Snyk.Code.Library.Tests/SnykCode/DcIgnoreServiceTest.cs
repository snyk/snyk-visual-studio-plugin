namespace Snyk.Code.Library.Tests.SnykCode
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Snyk.Code.Library.Service;
    using Snyk.Code.Library.Tests.Api;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="IDcIgnoreService"/>.
    /// </summary>
    public class DcIgnoreServiceTest
    {
        [Fact]
        public void DcIgnoreService_ListOfProjectFilesProvided_CreateDcIgnoreFileAndFilterFiles()
        {
            string folderPath = Path.GetTempPath();

            string dcIGnorePath = Path.Combine(folderPath, ".dcignore");

            if (File.Exists(dcIGnorePath))
            {
                File.Delete(dcIGnorePath);
            }

            Assert.False(File.Exists(dcIGnorePath));

            var projectFiles = new List<string>
            {
                "/Bin/Main.cs",
                "/bin/Main.cs",
                "/Src/Main.cs",
                "/.vs/App.cs",
                "/DocProject/Help/html/index.js",
                "/App.cs",
                "/build/Service.cs",
            };

            var dcIgnoreService = new DcIgnoreService(folderPath);
            var filteredFiles = dcIgnoreService.FilterFiles(projectFiles).ToList();

            Assert.Equal(3, filteredFiles.Count);

            Assert.True(File.Exists(dcIGnorePath));

            File.Delete(dcIGnorePath);
        }

        [Fact]
        public void DcIgnoreService_SolutionWithoutDcIgnoreProvided_CreateDcIgnore()
        {
            string folderPath = Path.GetTempPath();

            string dcIGnorePath = Path.Combine(folderPath, ".dcignore");

            if (File.Exists(dcIGnorePath))
            {
                File.Delete(dcIGnorePath);
            }

            Assert.False(File.Exists(dcIGnorePath));

            var dcIgnoreService = new DcIgnoreService(folderPath);
            dcIgnoreService.CreateDcIgnoreIfNeeded();

            Assert.True(File.Exists(dcIGnorePath));

            File.Delete(dcIGnorePath);
        }

        [Fact]
        public void DcIgnoreService_ListOfProjectFilesProvided_FilterFilesByGitIgnoreCheck()
        {
            var projectFiles = new List<string>
            {
                "/Bin/Main.cs",
                "/bin/Main.cs",
                "/Src/Main.cs",
                "/.vs/App.cs",
                "/DocProject/Help/html/index.js",
                "/App.cs",
                "/build/Service.cs",
            };

            var dcIgnoreService = new DcIgnoreService(TestResource.GetResourcesPath());
            var filteredFiles = dcIgnoreService.FilterFilesByGitIgnore(projectFiles).ToList();

            Assert.Equal(4, filteredFiles.Count);
        }
    }
}
