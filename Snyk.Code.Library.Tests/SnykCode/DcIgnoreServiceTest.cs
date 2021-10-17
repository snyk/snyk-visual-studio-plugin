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
        public void DcIgnoreService_MultipleGitIgnoreAndDcIgnoreFilesProvided_FilterFiles()
        {
            var projectFiles = new List<string>
            {
                "/SubProject1/Main.cs",
                "/SubProject1/RestService.cs",
                "/SubProject1/Debug/Main.cs",
                "/SubProject1/Debug/RestService.cs",
                "/SubProject1/Backup/Main.cs",
                "/SubProject1/Backup/RestService.cs",

                "/SubProject2/App.cs",
                "/SubProject2/AppService.cs",
                "/SubProject2/obj/App.cs",
                "/SubProject2/obj/AppService.cs",
                "/SubProject2/bin/Main.cs",

                "/SubProject3/App.cs",
                "/SubProject3/AppService.cs",
                "/SubProject3/bin/App.cs",
                "/SubProject3/bin/AppService.cs",
                "/SubProject3/Packages/App.cs",
                "/SubProject3/Packages/AppService.cs",

                "/bin/Main.cs",
                "/Main.cs",
                "/AppService.cs",
            };

            var dcIgnoreService = new DcIgnoreService();
            var filteredFiles = dcIgnoreService.FilterFiles(TestResource.GetFileFullPath("TestProject"), projectFiles).ToList();

            Assert.Equal(8, filteredFiles.Count);
        }

        [Fact]
        public void DcIgnoreService_MultipleGitIgnoreFilesProvided_FilterFilesByMultipleGitignore()
        {
            var projectFiles = new List<string>
            {
                "/SubProject1/Main.cs",
                "/SubProject1/RestService.cs",
                "/SubProject1/Debug/Main.cs",
                "/SubProject1/Debug/RestService.cs",

                "/SubProject2/App.cs",
                "/SubProject2/AppService.cs",
                "/SubProject2/obj/App.cs",
                "/SubProject2/obj/AppService.cs",
                "/SubProject2/bin/Main.cs",

                "/SubProject3/App.cs",
                "/SubProject3/AppService.cs",
                "/SubProject3/bin/App.cs",
                "/SubProject3/bin/AppService.cs",

                "/bin/Main.cs",
                "/Main.cs",
                "/AppService.cs",
            };

            var dcIgnoreService = new DcIgnoreService();
            var filteredFiles = dcIgnoreService.FilterFiles(TestResource.GetFileFullPath("TestProject"), projectFiles).ToList();

            Assert.Equal(8, filteredFiles.Count);
        }

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

            var dcIgnoreService = new DcIgnoreService();
            var filteredFiles = dcIgnoreService.FilterFiles(folderPath, projectFiles).ToList();

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

            var dcIgnoreService = new DcIgnoreService();
            dcIgnoreService.CreateDcIgnoreIfNeeded(folderPath);

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

            var dcIgnoreService = new DcIgnoreService();
            var filteredFiles = dcIgnoreService.FilterFilesByGitIgnore(TestResource.GetResourcesPath(), projectFiles).ToList();

            Assert.Equal(4, filteredFiles.Count);
        }
    }
}
