using Snyk.VisualStudio.Extension.UI.Toolwindow;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Toolwindow
{
    /// <summary>
    /// Pins when the cancelled/failed download handlers (re)start the language server. Turning off
    /// automatic CLI management means no download completes, so those handlers are the only thing that
    /// brings the server up — but they run on startup as well as after a settings change, and only the
    /// latter may disturb a server that is already serving.
    /// </summary>
    public class DownloadFallbackStartTest
    {
        [Fact]
        public void DoesNotStart_AtStartup_WhenTheServerIsAlreadyServing()
        {
            // The regression this guards: restarting here discarded a working session.
            Assert.False(SnykToolWindowControl.ShouldStartLanguageServer(
                cliUsable: true, languageServerReady: true, forced: false));
        }

        [Fact]
        public void Restarts_WhenTheUserChangedACliSetting_EvenThoughTheServerIsServing()
        {
            // The running process was launched from the previous executable and cannot switch to the
            // newly configured one, so a healthy server still has to be replaced.
            Assert.True(SnykToolWindowControl.ShouldStartLanguageServer(
                cliUsable: true, languageServerReady: true, forced: true));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Starts_WhenAUsableCliIsPresentAndNoServerIsRunning(bool forced)
        {
            Assert.True(SnykToolWindowControl.ShouldStartLanguageServer(
                cliUsable: true, languageServerReady: false, forced));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void NeverStarts_WhenTheConfiguredCliCannotRun(bool languageServerReady, bool forced)
        {
            // Usability is the outer gate: a forced change to an unusable path must not take down a
            // working server, and the handlers report the problem instead.
            Assert.False(SnykToolWindowControl.ShouldStartLanguageServer(
                cliUsable: false, languageServerReady, forced));
        }
    }
}
