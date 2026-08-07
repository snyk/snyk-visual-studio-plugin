using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    /// <summary>
    /// Pins which saved settings mean the language server has to be moved onto a different CLI. A false
    /// positive restarts the server on every settings save, dropping a working session; a false negative
    /// leaves it serving the old binary until Visual Studio is restarted.
    /// </summary>
    public class CliSettingsChangedTest
    {
        private static readonly string DefaultCli = SnykCli.GetSnykCliDefaultPath();
        private const string CustomCli = @"C:\Users\dev\Code\test_binaries\snyk-win.exe";
        private const string OtherCli = @"C:\Users\dev\Code\other_binaries\snyk-win.exe";

        private static bool Changed(
            string previousCliPath = "", bool previousAutoUpdate = true, string previousReleaseChannel = "stable",
            string cliPath = "", bool autoUpdate = true, string releaseChannel = "stable") =>
            HtmlSettingsScriptingBridge.DidCliSettingsChange(
                previousCliPath, previousAutoUpdate, previousReleaseChannel,
                cliPath, autoUpdate, releaseChannel);

        [Fact]
        public void NotChanged_WhenTheSaveTouchedNothingRelevant()
        {
            // The settings page saves on every OK, so an unrelated edit must not restart the server.
            Assert.False(Changed());
        }

        [Fact]
        public void NotChanged_WhenABlankPathIsPostedBackAsTheResolvedDefault()
        {
            // The settings page is populated from the resolved values sent to the language server, so a
            // user with no custom path sees the default location in the box and posts it back verbatim on
            // the first save. Same executable, so no restart.
            Assert.False(Changed(previousCliPath: "", cliPath: DefaultCli));
        }

        [Fact]
        public void NotChanged_WhenABlankChannelIsPostedBackAsStable()
        {
            // Blank resolves to stable, so this is the same release channel.
            Assert.False(Changed(previousReleaseChannel: "", releaseChannel: "stable"));
        }

        [Fact]
        public void Changed_WhenAutomaticManagementIsTurnedOffAgainstACustomPath()
        {
            Assert.True(Changed(
                previousCliPath: CustomCli, previousAutoUpdate: true,
                cliPath: CustomCli, autoUpdate: false));
        }

        [Fact]
        public void Changed_WhenTheCustomPathIsRepointed()
        {
            Assert.True(Changed(
                previousCliPath: OtherCli, previousAutoUpdate: false,
                cliPath: CustomCli, autoUpdate: false));
        }

        [Fact]
        public void Changed_WhenAutomaticManagementIsTurnedBackOn()
        {
            Assert.True(Changed(previousAutoUpdate: false, autoUpdate: true));
        }

        [Fact]
        public void Changed_WhenTheCustomPathIsCleared()
        {
            // Clearing falls back to the default location, which is a different executable.
            Assert.True(Changed(
                previousCliPath: CustomCli, previousAutoUpdate: false,
                cliPath: "", autoUpdate: false));
        }

        [Fact]
        public void Changed_WhenTheReleaseChannelIsSwitched()
        {
            // Same path, different binary: the check-and-download decides what belongs there.
            Assert.True(Changed(previousReleaseChannel: "stable", releaseChannel: "preview"));
        }

        [Fact]
        public void NotChanged_WhenOnlyThePathCasingDiffers()
        {
            Assert.False(Changed(previousCliPath: CustomCli, cliPath: CustomCli.ToUpperInvariant()));
        }

        [Fact]
        public void NotChanged_WhenOnlySurroundingWhitespaceDiffers()
        {
            // Both resolvers trim, so a stray space is not a new binary.
            Assert.False(Changed(previousCliPath: CustomCli, cliPath: "  " + CustomCli + "  "));
            Assert.False(Changed(previousReleaseChannel: "stable", releaseChannel: " stable "));
        }

        [Fact]
        public void NotChanged_WhenAnUnsetValueArrivesAsNull()
        {
            // The typed model yields null for an absent field and "" for a cleared one; both resolve to
            // the same default, so neither is a change from the other.
            Assert.False(Changed(previousCliPath: null, cliPath: ""));
            Assert.False(Changed(previousCliPath: "", cliPath: null));
            Assert.False(Changed(previousReleaseChannel: null, releaseChannel: "stable"));
        }
    }
}
