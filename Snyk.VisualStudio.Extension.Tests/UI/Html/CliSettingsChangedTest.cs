using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    /// <summary>
    /// Pins which saved settings mean the language server has to be moved onto a different CLI. A false
    /// positive restarts the server on every settings save; a false negative leaves it serving the old
    /// binary until Visual Studio is restarted.
    /// </summary>
    public class CliSettingsChangedTest
    {
        private const string AppDataCli = @"C:\Users\dev\AppData\Local\Snyk\snyk-win.exe";
        private const string CustomCli = @"C:\Users\dev\Code\test_binaries\snyk-win.exe";

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
                previousCliPath: AppDataCli, previousAutoUpdate: false,
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
        public void NotChanged_WhenAnUnsetPathArrivesAsNull()
        {
            // The typed model yields null for an absent field and "" for a cleared one; neither is a
            // change from the other, and treating them as one would restart on an unrelated save.
            Assert.False(Changed(previousCliPath: null, cliPath: ""));
            Assert.False(Changed(previousCliPath: "", cliPath: null));
        }
    }
}
