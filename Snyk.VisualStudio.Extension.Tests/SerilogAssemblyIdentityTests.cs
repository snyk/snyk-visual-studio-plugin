// The extension's Serilog fix rests on one property: the Serilog identity we bind to sits ABOVE
// the ceiling of the binding redirect GitLab for Visual Studio registers process-wide, so that
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
        public void ExtensionBindsToASerilogAboveTheForeignRedirectCeiling()
        {
            var referenced = ReferencedSerilogName();

            Assert.True(
                referenced.Version > ForeignRedirectCeiling,
                $"The extension binds to Serilog {referenced.Version}, inside the {ForeignRedirectCeiling} " +
                "redirect range GitLab for Visual Studio registers process-wide. Visual Studio will " +
                "rewrite that bind to a Serilog we do not ship and SnykVSPackage will fail to load.");
        }

        [Fact]
        public void ExtensionShipsTheSerilogItBindsTo()
        {
            var referenced = ReferencedSerilogName();
            var serilogPath = Path.Combine(ExtensionOutputDirectory(), "Serilog.dll");

            Assert.True(File.Exists(serilogPath), "Serilog.dll does not sit next to the extension assembly: " + serilogPath);

            var shipped = AssemblyName.GetAssemblyName(serilogPath).Version;

            Assert.True(
                referenced.Version == shipped,
                $"The extension is compiled against Serilog {referenced.Version} but ships {shipped}. " +
                "The bind then depends on a redirect being present, which is the failure mode this fix exists to avoid.");
        }

        private static AssemblyName ReferencedSerilogName()
        {
            var referenced = Array.Find(
                typeof(SnykVSPackage).Assembly.GetReferencedAssemblies(),
                name => string.Equals(name.Name, "Serilog", StringComparison.OrdinalIgnoreCase));

            Assert.True(referenced != null, "The extension assembly has no reference to Serilog at all.");
            return referenced;
        }

        /// <summary>
        /// CodeBase, not Location: the test host shadow-copies assemblies, so Location points into a
        /// temp folder that holds the extension assembly alone, without the dependencies shipped
        /// beside it. CodeBase keeps naming the original build output.
        /// </summary>
        private static string ExtensionOutputDirectory()
        {
            var assembly = typeof(SnykVSPackage).Assembly;
            var codeBase = assembly.CodeBase;

            if (string.IsNullOrEmpty(codeBase))
            {
                return Path.GetDirectoryName(assembly.Location);
            }

            return Path.GetDirectoryName(new Uri(codeBase).LocalPath);
        }
    }
}
