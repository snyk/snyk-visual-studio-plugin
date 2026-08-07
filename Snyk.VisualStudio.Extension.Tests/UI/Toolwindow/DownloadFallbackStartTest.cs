using Snyk.VisualStudio.Extension.UI.Toolwindow;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Toolwindow
{
    /// <summary>
    /// Pins the rule that the cancelled/failed download handlers only ever START the language server,
    /// never restart a running one. Turning off automatic CLI management means no download completes,
    /// so those handlers are the only thing that brings the server up — but by the time they run the
    /// server is often already serving, and stopping it there drops a working session.
    /// </summary>
    public class DownloadFallbackStartTest
    {
        [Fact]
        public void DoesNotStart_WhenTheServerIsAlreadyServing()
        {
            Assert.False(SnykToolWindowControl.ShouldStartLanguageServer(cliUsable: true, languageServerReady: true));
        }

        [Fact]
        public void Starts_WhenAUsableCliIsPresentAndTheServerIsStopped()
        {
            Assert.True(SnykToolWindowControl.ShouldStartLanguageServer(cliUsable: true, languageServerReady: false));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DoesNotStart_WhenThereIsNoUsableCli(bool languageServerReady)
        {
            // Nothing to launch: the handlers show the "specify a path" message instead.
            Assert.False(SnykToolWindowControl.ShouldStartLanguageServer(cliUsable: false, languageServerReady));
        }
    }
}
