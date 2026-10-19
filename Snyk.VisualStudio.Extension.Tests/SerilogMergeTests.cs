using System.Linq;
using Snyk.VisualStudio.Extension.Language;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class SerilogMergeTests
    {
        [Fact]
        public void ExtensionAssembly_ReferencesNoSeparateSerilogAssembly()
        {
            var referenced = typeof(SnykVSPackage).Assembly.GetReferencedAssemblies().Select(a => a.Name);

            Assert.DoesNotContain(referenced, name => name.StartsWith("Serilog"));
        }

        [Fact]
        public void SerilogTypes_AreDefinedInExtensionAssembly()
        {
            Assert.Same(typeof(SnykVSPackage).Assembly, typeof(Serilog.ILogger).Assembly);
        }

        [Fact]
        public void SnykLanguageClient_KeepsEveryDeclaredContentType()
        {
            var contentTypes = typeof(SnykLanguageClient)
                .GetCustomAttributes(typeof(SnykContentTypeAttribute), false)
                .Cast<SnykContentTypeAttribute>()
                .Select(a => a.ContentTypes)
                .ToList();

            Assert.Contains("CSharp", contentTypes);
            Assert.True(contentTypes.Count > 40);
            Assert.Equal(contentTypes.Count, contentTypes.Distinct().Count());
        }
    }
}
