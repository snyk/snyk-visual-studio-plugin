namespace Snyk.VisualStudio.Extension.Tests
{
    using System.Collections.Generic;
    using Snyk.VisualStudio.Extension.CLI;
    using Xunit;

    /// <summary>
    /// Test case for <see cref="SnykSolutionService"/>.
    /// </summary>
    public class SnykSolutionServiceTest
    {
        [Fact]
        public void SnykSolutionServiceTest_FindRootDirectoryForSolution_ReturnRootDirectory()
        {
            string soludtionDir = "C:\\projects\\superproj\\dummy\\";

            var paths = new List<string>
            {
                "C:\\projects\\superproj\\dummy\\sdf",
                "C:\\projects\\superproj\\testproj\\",
                "C:\\projects\\superproj\\sdf\testproj1\\",
                "C:\\projects\\superproj\\sdf",
                "C:\\projects\\superproj\\s\\d\f",
            };

            var solutionService = new SnykSolutionService();

            var rootDir = solutionService.FindRootDirectoryForSolutionProjects(soludtionDir, paths);

            Assert.Equal("C:\\projects\\superproj", rootDir);
        }
    }
}
