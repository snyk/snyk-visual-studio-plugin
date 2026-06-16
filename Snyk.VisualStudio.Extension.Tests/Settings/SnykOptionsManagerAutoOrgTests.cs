using System.IO;
using System.Threading.Tasks;
using Moq;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Settings
{
    /// <summary>
    /// Direct coverage for the still-shipping global organization helpers on
    /// <see cref="SnykOptionsManager"/> (<c>GetOrganizationAsync</c> / <c>SaveOrganizationAsync</c>).
    /// The solution-scoped org helpers were removed with the disk-persistence cleanup; the Language
    /// Server is the source of truth for per-folder orgs.
    /// </summary>
    public class SnykOptionsManagerAutoOrgTests
    {
        private readonly SnykOptionsManager cut;
        private readonly Mock<ISnykOptions> optionsMock;
        private readonly string testSettingsPath;

        public SnykOptionsManagerAutoOrgTests()
        {
            this.testSettingsPath = Path.GetTempFileName();
            this.optionsMock = new Mock<ISnykOptions>();
            this.optionsMock.SetupProperty(o => o.Organization);

            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            serviceProviderMock.Setup(x => x.Options).Returns(this.optionsMock.Object);

            this.cut = new SnykOptionsManager(this.testSettingsPath, serviceProviderMock.Object);
        }

        [Fact]
        public async Task GetOrganizationAsync_Defaults_ToEmptyString()
        {
            Assert.Equal(string.Empty, await this.cut.GetOrganizationAsync());
        }

        [Fact]
        public async Task SaveOrganizationAsync_RoundTrips_AndUpdatesLiveOptions()
        {
            await this.cut.SaveOrganizationAsync("my-org");

            Assert.Equal("my-org", await this.cut.GetOrganizationAsync());
            Assert.Equal("my-org", this.optionsMock.Object.Organization); // pushed to live Options too
        }

        [Fact]
        public async Task SaveOrganizationAsync_Persists_AcrossReload()
        {
            await this.cut.SaveOrganizationAsync("persisted-org");

            // A fresh manager over the same file must read the saved org back from disk.
            var reloaded = new SnykOptionsManager(this.testSettingsPath, BuildServiceProvider());
            Assert.Equal("persisted-org", await reloaded.GetOrganizationAsync());
        }

        private static ISnykServiceProvider BuildServiceProvider()
        {
            var sp = new Mock<ISnykServiceProvider>();
            var options = new Mock<ISnykOptions>();
            options.SetupProperty(o => o.Organization);
            sp.Setup(x => x.Options).Returns(options.Object);
            return sp.Object;
        }
    }
}
