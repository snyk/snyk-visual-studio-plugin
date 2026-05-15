using System;
using Snyk.VisualStudio.Extension.UI.Toolwindow;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Toolwindow
{
    /// <summary>
    /// Pins the IDisposable chain that prevents temp-file accumulation in
    /// <c>%LOCALAPPDATA%\Snyk\WebView2\&lt;pid&gt;\toolwindow\scratch</c> when the tool window
    /// is destroyed. Constructing the panels directly is impractical (requires WPF + a real
    /// WebView2 runtime), so we settle for the structural contract — anyone removing
    /// <c>IDisposable</c> in a future refactor will fail this test as a flag.
    /// The underlying cleanup behaviour itself is covered by
    /// <c>WebView2NavigationPreparerTest.Dispose_SweepsAllRemainingTempFiles</c>.
    /// </summary>
    public class PanelDisposalContractTest
    {
        [Fact]
        public void HtmlDescriptionPanel_ImplementsIDisposable()
        {
            Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(HtmlDescriptionPanel)));
        }

        [Fact]
        public void SummaryHtmlPanel_ImplementsIDisposable()
        {
            Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(SummaryHtmlPanel)));
        }

        [Fact]
        public void SnykToolWindowControl_ImplementsIDisposable()
        {
            // ToolWindowPane.Dispose chains into Content.Dispose if Content is IDisposable —
            // this is the hook that makes the panels actually get cleaned up.
            Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(SnykToolWindowControl)));
        }
    }
}
