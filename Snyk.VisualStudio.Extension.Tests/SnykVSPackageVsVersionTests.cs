using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    /// <summary>
    /// Unit tests for <see cref="SnykVSPackage.ToReadableVsVersion"/>, the pure mapping from a
    /// Visual Studio major version to the IDE name reported to the Language Server.
    ///
    /// The Language Server derives its config storage filename from that name, so an unmapped major
    /// falls back to the shared unknown bucket and reads an orphaned config — losing trusted folders
    /// and the auth token. These tests pin the mapping for every major the plugin supports.
    /// </summary>
    public class SnykVSPackageVsVersionTests
    {
        [Theory]
        [InlineData("18", "Visual Studio 2026")]
        [InlineData("17", "Visual Studio 2022")]
        [InlineData("16", "Visual Studio 2019")]
        [InlineData("15", "Visual Studio 2017")]
        [InlineData("14", "Visual Studio 2015")]
        public void ToReadableVsVersion_MapsSupportedMajorsToTheirProductName(string major, string expected)
        {
            Assert.Equal(expected, SnykVSPackage.ToReadableVsVersion(major));
        }

        [Theory]
        [InlineData("19")]      // a VS release newer than any mapped here
        [InlineData("13")]      // older than any mapped here
        [InlineData("0")]       // GetVsVersionAsync's failure sentinel is "0.0.0"
        [InlineData("")]
        [InlineData(null)]
        [InlineData("nonsense")]
        public void ToReadableVsVersion_FallsBackToUnknownForUnmappedMajors(string major)
        {
            Assert.Equal("Unknown Visual Studio version", SnykVSPackage.ToReadableVsVersion(major));
        }

        [Fact]
        public void ToReadableVsVersion_DistinguishesVs2026FromVs2022()
        {
            // The two must not collide: sharing a name means sharing one stored config, which is how
            // an upgrade silently inherits (or loses) the other release's trusted folders and token.
            Assert.NotEqual(
                SnykVSPackage.ToReadableVsVersion("17"),
                SnykVSPackage.ToReadableVsVersion("18"));
        }
    }
}
