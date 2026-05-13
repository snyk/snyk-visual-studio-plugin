using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    public class WebView2ExternalPolyfillTest
    {
        [Fact]
        public void BuildScript_DefinesWindowExternal()
        {
            var script = WebView2ExternalPolyfill.BuildScript();

            Assert.Contains("window.external", script);
        }

        [Fact]
        public void BuildScript_DispatchesViaPostMessage()
        {
            var script = WebView2ExternalPolyfill.BuildScript();

            Assert.Contains("chrome.webview.postMessage", script);
        }

        [Fact]
        public void BuildScript_IsWrappedInImmediatelyInvokedFunction()
        {
            var script = WebView2ExternalPolyfill.BuildScript().Trim();

            Assert.StartsWith("(function", script);
            Assert.EndsWith("})();", script);
        }

        [Fact]
        public void KnownMethods_CoversEveryWindowExternalCallSite()
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

            Assert.Equal(expected, WebView2ExternalPolyfill.KnownMethods);
        }

        [Fact]
        public void BuildScript_DefinesEveryKnownMethod()
        {
            var script = WebView2ExternalPolyfill.BuildScript();

            foreach (var method in WebView2ExternalPolyfill.KnownMethods)
            {
                Assert.Contains(method, script);
            }
        }

        [Fact]
        public void BuildScript_PassesMethodNameAsFirstPostArgument()
        {
            var script = WebView2ExternalPolyfill.BuildScript();

            foreach (var method in WebView2ExternalPolyfill.KnownMethods)
            {
                Assert.Contains($"'{method}'", script);
            }
        }
    }
}
