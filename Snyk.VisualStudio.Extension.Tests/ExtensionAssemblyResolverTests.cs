// Unit tests for ExtensionAssemblyResolver.ResolveCandidatePath, the pure decision
// seam behind the extension-scoped AppDomain.AssemblyResolve fallback.
//
// These exercise only the injectable, side-effect-free decision logic — no real AppDomain,
// no real file system, no Serilog.
using System.IO;
using Snyk.VisualStudio.Extension;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class ExtensionAssemblyResolverTests
    {
        private static readonly string ExtensionDir =
            Path.Combine("C:", "Program Files", "Microsoft Visual Studio", "Extensions", "Snyk");

        private static readonly string OtherExtensionDir =
            Path.Combine("C:", "Program Files", "Microsoft Visual Studio", "Extensions", "GitLab");

        private const string SerilogFullName =
            "Serilog, Version=2.10.0.0, Culture=neutral, PublicKeyToken=24c2f752a8e58a10";

        private static readonly string ExpectedSerilogCandidatePath =
            Path.Combine(ExtensionDir, "Serilog.dll");

        private static readonly string RequestingFromExtensionDir =
            Path.Combine(ExtensionDir, "Snyk.VisualStudio.Extension.dll");

        private static readonly string RequestingFromForeignExtensionDir =
            Path.Combine(OtherExtensionDir, "GitLab.VisualStudio.dll");

        // Covers: resolving from inside the extension dir when the file exists (TS-001), declining
        // when it's missing (TS-002), the no-requesting-assembly case (TS-003), a foreign
        // extension's request (TS-004), and the two new "requesting assembly exists but its
        // location is unknown" cases that must be declined rather than treated as ours.
        [Theory]
        [InlineData(true, "extensionDir", true, true)]
        [InlineData(true, "extensionDir", false, false)]
        [InlineData(false, null, true, true)]
        [InlineData(true, "foreignDir", true, false)]
        [InlineData(true, null, true, false)]
        [InlineData(true, "", true, false)]
        public void ResolveCandidatePath_ScopingAndExistence_ResolvesOnlyWhenOwnedAndPresent(
            bool hasRequestingAssembly, string requestingLocationKind, bool fileExists, bool expectResolved)
        {
            string requestingLocation;
            if (requestingLocationKind == "extensionDir")
            {
                requestingLocation = RequestingFromExtensionDir;
            }
            else if (requestingLocationKind == "foreignDir")
            {
                requestingLocation = RequestingFromForeignExtensionDir;
            }
            else if (requestingLocationKind == "")
            {
                requestingLocation = string.Empty;
            }
            else
            {
                requestingLocation = null;
            }

            var result = ExtensionAssemblyResolver.ResolveCandidatePath(
                SerilogFullName,
                hasRequestingAssembly,
                requestingLocation,
                ExtensionDir,
                _ => fileExists);

            Assert.Equal(expectResolved ? ExpectedSerilogCandidatePath : null, result);
        }

        [Fact]
        public void ResolveCandidatePath_SatelliteResourceRequest_ReturnsNullWithoutProbing()
        {
            var probed = false;

            var result = ExtensionAssemblyResolver.ResolveCandidatePath(
                "Snyk.VisualStudio.Extension.resources, Version=1.0.0.0, Culture=en, PublicKeyToken=null",
                hasRequestingAssembly: false,
                requestingAssemblyLocation: null,
                extensionDirectory: ExtensionDir,
                fileExists: _ => { probed = true; return true; });

            Assert.Null(result);
            Assert.False(probed, "Satellite resource requests must not be probed for on disk.");
        }

        [Fact]
        public void ResolveCandidatePath_TraversalShapedSimpleName_ReturnsNullWithoutProbing()
        {
            var probed = false;

            var result = ExtensionAssemblyResolver.ResolveCandidatePath(
                "../../evil, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                hasRequestingAssembly: false,
                requestingAssemblyLocation: null,
                extensionDirectory: ExtensionDir,
                fileExists: _ => { probed = true; return true; });

            Assert.Null(result);
            Assert.False(probed, "A traversal-shaped assembly name must not be probed for on disk.");
        }

        [Fact]
        public void ResolveCandidatePath_RootedSimpleName_ReturnsNullWithoutProbing()
        {
            var probed = false;

            var result = ExtensionAssemblyResolver.ResolveCandidatePath(
                "C:evil, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                hasRequestingAssembly: false,
                requestingAssemblyLocation: null,
                extensionDirectory: ExtensionDir,
                fileExists: _ => { probed = true; return true; });

            Assert.Null(result);
            Assert.False(probed, "A rooted assembly name must not be probed for on disk.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ResolveCandidatePath_EmptyRequestedName_ReturnsNull(string requestedName)
        {
            var result = ExtensionAssemblyResolver.ResolveCandidatePath(
                requestedName,
                hasRequestingAssembly: false,
                requestingAssemblyLocation: null,
                extensionDirectory: ExtensionDir,
                fileExists: _ => true);

            Assert.Null(result);
        }

        [Fact]
        public void ResolveCandidatePath_UnparseableAssemblyName_DoesNotThrow_ReturnsNull()
        {
            var ex = Record.Exception(() => ExtensionAssemblyResolver.ResolveCandidatePath(
                "not a valid assembly name / / \0",
                hasRequestingAssembly: false,
                requestingAssemblyLocation: null,
                extensionDirectory: ExtensionDir,
                fileExists: _ => true));

            Assert.Null(ex);
        }

        [Fact]
        public void ResolveCandidatePath_MissingExtensionDirectory_ReturnsNull()
        {
            var result = ExtensionAssemblyResolver.ResolveCandidatePath(
                SerilogFullName,
                hasRequestingAssembly: false,
                requestingAssemblyLocation: null,
                extensionDirectory: null,
                fileExists: _ => true);

            Assert.Null(result);
        }

        // The simple name is what's probed for — full/versioned names must not leak into the
        // candidate file name.
        [Fact]
        public void ResolveCandidatePath_UsesSimpleNameOnly_ForCandidateFileName()
        {
            string capturedPath = null;

            ExtensionAssemblyResolver.ResolveCandidatePath(
                "Serilog.Exceptions, Version=8.4.0.0, Culture=neutral, PublicKeyToken=null",
                hasRequestingAssembly: false,
                requestingAssemblyLocation: null,
                extensionDirectory: ExtensionDir,
                fileExists: path =>
                {
                    capturedPath = path;
                    return true;
                });

            Assert.Equal(Path.Combine(ExtensionDir, "Serilog.Exceptions.dll"), capturedPath);
        }
    }
}
