using System;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Snyk.VisualStudio.Extension.UI.Toolwindow;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Toolwindow
{
    /// <summary>
    /// Pins the IDisposable chain that prevents WebView2 user-data folders / msedgewebview2.exe
    /// processes from leaking when the tool window is destroyed.
    /// <para>
    /// <see cref="Microsoft.VisualStudio.Shell.WindowPane.Dispose(bool)"/> does NOT chain into the
    /// <c>Content</c> control, so <see cref="SnykToolWindow"/> overrides Dispose to do it. The
    /// behavioural tests below exercise that chaining via the internal seam
    /// (<c>SnykToolWindow.DisposeContentOnce</c> + the createContent:false test constructor) so the
    /// real WebView2-hosting panels — which need a live WPF + WebView2 runtime — don't have to be
    /// constructed. The underlying host cleanup itself is covered by
    /// <c>WebView2NavigationPreparerTest.Dispose_SweepsAllRemainingTempFiles</c>.
    /// </para>
    /// </summary>
    [Collection(MockedVS.Collection)]
    public class PanelDisposalContractTest
    {
        public PanelDisposalContractTest(GlobalServiceProvider sp)
        {
            sp.Reset();
        }

        [Fact]
        public void SnykToolWindow_DisposesContent_WhenDisposed()
        {
            var content = new Mock<IDisposable>();
            var toolWindow = new SnykToolWindow(createContent: false) { Content = content.Object };

            toolWindow.DisposeContentOnce();

            content.Verify(c => c.Dispose(), Times.Once);
        }

        [Fact]
        public void SnykToolWindow_DisposesContentExactlyOnce_AcrossRepeatedDispose()
        {
            var content = new Mock<IDisposable>();
            var toolWindow = new SnykToolWindow(createContent: false) { Content = content.Object };

            toolWindow.DisposeContentOnce();
            toolWindow.DisposeContentOnce();
            toolWindow.DisposeContentOnce();

            // The _disposed guard means the chained Dispose fires exactly once even though the base
            // WindowPane.Dispose and our override can both run at teardown.
            content.Verify(c => c.Dispose(), Times.Once);
        }

        [Fact]
        public void SnykToolWindow_DoesNotThrow_WhenContentIsNotDisposable()
        {
            var toolWindow = new SnykToolWindow(createContent: false) { Content = new object() };

            // Must be a no-op rather than throwing when Content doesn't implement IDisposable.
            toolWindow.DisposeContentOnce();
        }

        // Secondary structural guards: the chain above only cleans up if each panel (the Content's
        // children) is itself IDisposable. A refactor dropping IDisposable from a panel would
        // silently reintroduce the leak, so keep these as a tripwire.
        [Fact]
        public void Panels_ImplementIDisposable()
        {
            Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(HtmlDescriptionPanel)));
            Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(SummaryHtmlPanel)));
            Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(TreeHtmlPanel)));
            Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(SnykToolWindowControl)));
        }

        // Regression guard for IDE-1842: TreeHtmlPanel.Dispose() calls cts.Dispose() after
        // cts.Cancel(). On .NET Framework 4.8, reading CancellationTokenSource.Token after Dispose()
        // throws ObjectDisposedException, which would corrupt any lambda that closed over cts.Token
        // directly. The fix captures the token once at construction (ctsToken = cts.Token) and uses
        // the captured struct in all lambdas. This test pins the BCL contract that a CancellationToken
        // struct captured before disposal is safe to read (IsCancellationRequested) after the source
        // is disposed — it must not throw. If this test fails the fix's fundamental assumption is
        // wrong and needs revisiting.
        [Fact]
        public void CapturedCancellationToken_IsSafeToRead_AfterSourceDisposed()
        {
            var cts = new System.Threading.CancellationTokenSource();
            // Capture the token struct before disposal — mirrors what TreeHtmlPanel does in its ctor.
            var captured = cts.Token;

            cts.Cancel();
            cts.Dispose();

            // Must not throw ObjectDisposedException; captured token reflects cancelled state.
            Assert.True(captured.IsCancellationRequested);
        }
    }
}
