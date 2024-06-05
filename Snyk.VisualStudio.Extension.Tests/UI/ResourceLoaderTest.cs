using System.Reflection;
using System;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI
{
    /// <summary>
    /// Unit tests for <see cref="ResourceLoader"/>.
    /// </summary>
    public class ResourceLoaderTest
    {
        private const string AssemblyName = "Snyk.VisualStudio.Extension.Tests";
        private const string ResourcesDirectory = "/Resources";

        public ResourceLoaderTest()
        {
            var resourcesType = typeof(ResourceLoader);

            resourcesType.GetField("_resourceBasePath", BindingFlags.NonPublic | BindingFlags.Static)?
                .SetValue(null, null);

            resourcesType.GetField("_assemblyName", BindingFlags.NonPublic | BindingFlags.Static)?
                .SetValue(null, AssemblyName);


            resourcesType.GetField("_resourcesDirectory", BindingFlags.NonPublic | BindingFlags.Static)?
                .SetValue(null, ResourcesDirectory);


            if (!UriParser.IsKnownScheme("pack")) 
                // ReSharper disable once ObjectCreationAsStatement
                new System.Windows.Application();
        }

        [Fact]
        public void GetResourcePath_ValidImageName_ReturnsCorrectPath()
        {
            const string imageName = "TestImage.png";

            var resourcePath = ResourceLoader.GetResourcePath(imageName);

            Assert.NotNull(resourcePath);
            Assert.Contains($"pack://application:,,,/{AssemblyName};component/", resourcePath);
            Assert.Contains(imageName, resourcePath);
        }

        [Fact]
        public void GetResourcePath_EmptyImageName_ThrowsArgumentNullException()
        {
            string imageName = null;

            // ReSharper disable once ExpressionIsAlwaysNull
            var ex = Assert.Throws<ArgumentNullException>(() => ResourceLoader.GetResourcePath(imageName));
            Assert.Equal("imageName", ex.ParamName);
        }

        [Fact]
        public void GetResourcePath_InvalidImageName_ThrowsArgumentException()
        {
            const string imageName = "InvalidImage.png";

            var ex = Assert.Throws<ArgumentException>(() => ResourceLoader.GetResourcePath(imageName));
            Assert.Equal($"Image with Path {imageName} not found", ex.Message);
        }

        [Fact]
        public void GetBaseResourcePath_ValidImageName_ReturnsBasePath()
        {
            const string imageName = "TestImage.png";

            var basePath = ResourceLoader.GetBaseResourcePath(AssemblyName, ResourcesDirectory, imageName);

            Assert.NotNull(basePath);
            Assert.Contains($"pack://application:,,,/{AssemblyName};component/", basePath);
        }

        [Fact]
        public void GetBaseResourcePath_InvalidImageName_ReturnsNull()
        {
            const string imageName = "InvalidImage.png";

            var basePath = ResourceLoader.GetBaseResourcePath(AssemblyName, ResourcesDirectory, imageName);

            Assert.Null(basePath);
        }
    }
}
