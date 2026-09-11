// The extension's Serilog fix rests on one property: the Serilog identity we ship sits ABOVE the
// ceiling of the binding redirect GitLab for Visual Studio registers process-wide, so that
// redirect never rewrites our binds. Drop back inside the range and Visual Studio demands a
// Serilog nobody ships, and the package fails to load.
//
// Serilog's AssemblyVersion is major.minor.0.0, so every Serilog minor release is a distinct
// binding identity and this margin is narrow by construction. Removing the direct Serilog
// PackageReference is enough to reopen the bug: the transitive floor resolves 2.12.0, whose
// identity is 2.0.0.0, well inside the range.
//
// This pins the decision, not the CLR's behaviour. Whether Visual Studio then loads the package is
// not checkable here and stays a manual check on Windows with the real GitLab extension installed.
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class SerilogAssemblyIdentityTests
    {
        // GitLab for Visual Studio 0.80.0, GitLab.Extension.pkgdef:
        //   oldVersion="0.0.0.0-4.3.0.0" newVersion="4.3.0.0"
        private static readonly Version ForeignRedirectCeiling = new Version("4.3.0.0");

        [Fact]
        public void ShippedSerilog_HasAnIdentityAboveTheForeignRedirectCeiling()
        {
            var extensionDirectory = Path.GetDirectoryName(typeof(SnykVSPackage).Assembly.Location);
            var serilogPath = Path.Combine(extensionDirectory, "Serilog.dll");

            Assert.True(File.Exists(serilogPath), "Serilog.dll does not ship next to the extension assembly: " + serilogPath);

            var shipped = AssemblyName.GetAssemblyName(serilogPath).Version;

            Assert.True(
                shipped > ForeignRedirectCeiling,
                $"Shipped Serilog identity is {shipped}, which is inside the {ForeignRedirectCeiling} " +
                "redirect range GitLab for Visual Studio registers process-wide. Visual Studio will " +
                "rewrite our binds to a Serilog we do not ship and SnykVSPackage will fail to load.");
        }

        [Fact]
        public void ExtensionAssembly_ReferencesTheSerilogItShips()
        {
            var extensionAssembly = typeof(SnykVSPackage).Assembly;
            var referenced = Array.Find(
                extensionAssembly.GetReferencedAssemblies(),
                name => string.Equals(name.Name, "Serilog", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(referenced);

            var serilogPath = Path.Combine(Path.GetDirectoryName(extensionAssembly.Location), "Serilog.dll");
            var shipped = AssemblyName.GetAssemblyName(serilogPath).Version;

            Assert.True(
                referenced.Version == shipped,
                $"The extension is compiled against Serilog {referenced.Version} but ships {shipped}. " +
                "The bind then depends on a redirect being present, which is the failure mode this fix exists to avoid.");
        }
    }
}
