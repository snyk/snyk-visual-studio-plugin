using System.Linq;
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
    }
}
