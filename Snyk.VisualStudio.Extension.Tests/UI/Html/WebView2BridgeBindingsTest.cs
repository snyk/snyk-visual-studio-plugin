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
            var expected = new[]
            {
                "__saveIdeConfig__",
                "__onFormDirtyChange__",
                "__ideSaveAttemptFinished__",
                "__ideExecuteCommand__",
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
