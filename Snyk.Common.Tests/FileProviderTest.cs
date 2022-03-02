namespace Snyk.Common.Tests
{
    using Moq;
    using Snyk.Code.Library.Service;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="IFileProvider"/>.
    /// </summary>
    public class FileProviderTest
    {
        [Fact]
        public void FileProvider_ChangedAndRemovedFilesProvided_ReturnOnlyChangedFiles()
        {
            var solutionServiceMock = new Mock<ISolutionService>();

            var fileProvider = new SnykCodeFileProvider(solutionServiceMock.Object);

            fileProvider.AddChangedFile("C:\\Projects\\TestProj\\Test1.cs");
            fileProvider.AddChangedFile("C:\\Projects\\TestProj\\Test2.cs");
            fileProvider.AddChangedFile("C:\\Projects\\TestProj\\Test1.cs");
            fileProvider.AddChangedFile("C:\\Projects\\TestProj\\Test2.cs");

            var allChangedFiles = fileProvider.GetAllChangedFiles();

            Assert.Equal(2, allChangedFiles.Count());
        }
    }
}
