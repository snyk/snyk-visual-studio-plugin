using Snyk.VisualStudio.Extension.UI.Toolwindow;
using Xunit;

using ServerAction = Snyk.VisualStudio.Extension.UI.Toolwindow.SnykToolWindowControl.ServerAction;

namespace Snyk.VisualStudio.Extension.Tests.UI.Toolwindow
{
    /// <summary>
    /// Pins what each CLI-check outcome does about the language server. The three inputs are independent
    /// facts the tool window records rather than infers, and getting any of them wrong is silent: too
    /// eager and a working session is discarded on every settings save, too shy and the server keeps
    /// serving the previous executable until Visual Studio restarts.
    /// </summary>
    public class DownloadOutcomeServerActionTest
    {
        private static ServerAction Decide(
            bool cliUsable = true, bool stopIssued = false, bool serverRunning = false, bool cliSettingsChanged = false) =>
            SnykToolWindowControl.DecideServerAction(cliUsable, stopIssued, serverRunning, cliSettingsChanged);

        [Fact]
        public void LeavesAServingServerAlone_AtStartup()
        {
            // The regression this guards: restarting here discarded a working session.
            Assert.Equal(ServerAction.None, Decide(serverRunning: true));
        }

        [Fact]
        public void Restarts_WhenTheCliSettingsChangedUnderAServingServer()
        {
            // The process was launched from the previous executable and cannot switch to the new one.
            Assert.Equal(ServerAction.Restart, Decide(serverRunning: true, cliSettingsChanged: true));
        }

        [Fact]
        public void Starts_WhenWeStoppedItForTheDownloadAndItIsDown()
        {
            Assert.Equal(ServerAction.Start, Decide(stopIssued: true, serverRunning: false));
        }

        [Fact]
        public void Restarts_WhenOurStopHasNotLandedYet()
        {
            // The stop issued for the download is fire-and-forget, so an outcome arriving quickly can still
            // see a running server. Raising a start there is discarded by Visual Studio for a server it
            // considers started, and the in-flight stop then takes it down with nothing to bring it back.
            // Restart also tolerates the reading having gone stale the other way: stopping an
            // already-stopped server is a no-op, where a discarded start is not recoverable.
            Assert.Equal(ServerAction.Restart, Decide(stopIssued: true, serverRunning: true));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Starts_WhenNothingIsRunning(bool cliSettingsChanged)
        {
            Assert.Equal(ServerAction.Start, Decide(serverRunning: false, cliSettingsChanged: cliSettingsChanged));
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void NeverActs_WhenTheConfiguredCliCannotRun(bool stopIssued, bool serverRunning, bool cliSettingsChanged)
        {
            // Usability is the outer gate: a settings change pointing at a binary that cannot run must not
            // take down a working server, and must not launch the unusable one either.
            Assert.Equal(
                ServerAction.None,
                Decide(cliUsable: false, stopIssued: stopIssued, serverRunning: serverRunning, cliSettingsChanged: cliSettingsChanged));
        }
    }
}
