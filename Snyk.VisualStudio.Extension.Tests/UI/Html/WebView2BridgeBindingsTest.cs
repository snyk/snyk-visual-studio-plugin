using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    public class WebView2BridgeBindingsTest
    {
        [Fact]
        public void BuildScript_AssignsEachKnownMethodToWindow()
        {
            var script = WebView2BridgeBindings.BuildScript();

            foreach (var method in WebView2BridgeBindings.KnownMethods)
            {
                Assert.Contains($"window.{method} = function", script);
            }
        }

        [Fact]
        public void BuildScript_DispatchesViaPostMessage()
        {
            var script = WebView2BridgeBindings.BuildScript();

            Assert.Contains("chrome.webview.postMessage", script);
        }

        [Fact]
        public void BuildScript_IsWrappedInImmediatelyInvokedFunction()
        {
            var script = WebView2BridgeBindings.BuildScript().Trim();

            Assert.StartsWith("(function", script);
            Assert.EndsWith("})();", script);
        }

        [Fact]
        public void KnownMethods_CoversEveryBridgeCallSite()
        {
            // __ideExecuteCommand__ is deliberately absent — it's defined separately by
            // ExecuteCommandBridge.BuildClientScript() because it needs callback-id handling
            // that a raw postMessage forwarder can't provide.
            var expected = new[]
            {
                "__saveIdeConfig__",
                "__onFormDirtyChange__",
                "__ideSaveAttemptFinished__",
                "OpenLink",
                "OpenFileInEditor",
                "EnableDelta",
                "GenerateFixes",
                "ApplyFixDiff",
                "SubmitIgnoreRequest",
                "FocusToolWindow",
            };

            Assert.Equal(expected, WebView2BridgeBindings.KnownMethods);
        }

        [Fact]
        public void BuildScript_DoesNotDefineIdeExecuteCommand()
        {
            // The LS HTML calls window.__ideExecuteCommand__ with a JS callback function,
            // which raw postMessage can't carry — ExecuteCommandBridge.BuildClientScript()
            // defines a proper wrapper. The bindings must not shadow it.
            var script = WebView2BridgeBindings.BuildScript();
            Assert.DoesNotContain("window.__ideExecuteCommand__", script);
        }

        [Fact]
        public void BuildScript_PassesMethodNameAsFirstPostArgument()
        {
            var script = WebView2BridgeBindings.BuildScript();

            foreach (var method in WebView2BridgeBindings.KnownMethods)
            {
                Assert.Contains($"'{method}'", script);
            }
        }
    }
}
