using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    public class ExecuteCommandBridgeTest
    {
        [Fact]
        public void BuildClientScript_ContainsIdeExecuteCommandDefinition()
        {
            var script = ExecuteCommandBridge.BuildClientScript();
            Assert.Contains("window.__ideExecuteCommand__", script);
        }

        [Fact]
        public void BuildClientScript_ContainsIdeCallbacksInitialization()
        {
            var script = ExecuteCommandBridge.BuildClientScript();
            Assert.Contains("window.__ideCallbacks__", script);
        }

        [Fact]
        public void BuildClientScript_PostsIdeExecuteCommandToHost()
        {
            var script = ExecuteCommandBridge.BuildClientScript();
            Assert.Contains("chrome.webview.postMessage", script);
            Assert.Contains("method: '__ideExecuteCommand__'", script);
        }

        [Fact]
        public void BuildClientScript_IncludesCallbackIdHandling()
        {
            var script = ExecuteCommandBridge.BuildClientScript();
            Assert.Contains("callbackId", script);
            Assert.Contains("callback", script);
        }

        [Fact]
        public void BuildClientScript_IsEs5Compatible()
        {
            var script = ExecuteCommandBridge.BuildClientScript();
            // ES5: var (not const/let), function keyword (not arrow functions)
            Assert.Contains("var ", script);
            Assert.DoesNotContain("const ", script);
            Assert.DoesNotContain("let ", script);
            Assert.DoesNotContain("=>", script);
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("__cb_1", true)]
        [InlineData("__cb_123", true)]
        [InlineData("__cb_9999999", true)]
        [InlineData(null, true)]
        public void IsValidCallbackId_AcceptsValidFormats(string callbackId, bool expected)
        {
            Assert.Equal(expected, ExecuteCommandBridge.IsValidCallbackId(callbackId));
        }

        [Theory]
        [InlineData("<script>alert(1)</script>")]
        [InlineData("__cb_abc")]
        [InlineData(";drop table--")]
        [InlineData("__cb_1; alert(1)")]
        [InlineData("__cb_")]
        [InlineData("_cb_8")]
        [InlineData(" ")]
        public void IsValidCallbackId_RejectsInvalidFormats(string callbackId)
        {
            Assert.False(ExecuteCommandBridge.IsValidCallbackId(callbackId));
        }

        [Theory]
        [InlineData("snyk.login", true)]
        [InlineData("snyk.logout", true)]
        [InlineData("snyk.navigateToRange", true)]
        [InlineData("snyk.setNodeExpanded", true)]
        [InlineData("snyk.showScanErrorDetails", true)]
        [InlineData("snyk.toggleTreeFilter", true)]
        [InlineData("snyk.updateFolderConfig", true)]
        // Not on the allowlist — a snyk.* prefix is no longer sufficient.
        [InlineData("snyk.anyCommand", false)]
        [InlineData("snyk.clearCache", false)]
        public void IsAllowedCommand_AllowsOnlyAllowlistedCommands(string command, bool expected)
        {
            Assert.Equal(expected, ExecuteCommandBridge.IsAllowedCommand(command));
        }

        [Theory]
        [InlineData("workbench.action.terminal.new")]
        [InlineData("vscode.open")]
        [InlineData("")]
        [InlineData(null)]
        public void IsAllowedCommand_RejectsNonSnykCommands(string command)
        {
            Assert.False(ExecuteCommandBridge.IsAllowedCommand(command));
        }

        [Fact]
        public void BuildSetAuthTokenScript_GuardsOnFunctionExisting()
        {
            var script = ExecuteCommandBridge.BuildSetAuthTokenScript("tok", "https://snyk.io");

            Assert.Contains("typeof window.setAuthToken === 'function'", script);
        }

        [Fact]
        public void BuildSetAuthTokenScript_EscapesArgsAsDoubleQuotedLiterals()
        {
            // A token with characters that could break out of a JS string literal or the inline
            // <script> (double quote, </script>) must be neutralised by the JSON-literal escaping.
            var script = ExecuteCommandBridge.BuildSetAuthTokenScript("a\"b</script>", "https://snyk.io");

            // Args are emitted as double-quoted JSON literals, not hand-wrapped single-quoted strings.
            Assert.Contains("window.setAuthToken(\"", script);
            // The raw breakout sequences do not survive escaping.
            Assert.DoesNotContain("</script>", script);
            Assert.DoesNotContain("a\"b", script);
        }

        [Fact]
        public void BuildSetAuthTokenScript_TreatsNullApiUrlAsEmptyString()
        {
            var script = ExecuteCommandBridge.BuildSetAuthTokenScript("tok", null);

            Assert.Contains("window.setAuthToken(\"tok\", \"\")", script);
        }

        [Fact]
        public void ToJsStringLiteral_ProducesQuotedAndEscapedLiteral()
        {
            Assert.Equal("\"plain\"", ExecuteCommandBridge.ToJsStringLiteral("plain"));
            Assert.Equal("\"\"", ExecuteCommandBridge.ToJsStringLiteral(null));

            var escaped = ExecuteCommandBridge.ToJsStringLiteral("</script><x a=\"1\">");
            Assert.StartsWith("\"", escaped);
            Assert.EndsWith("\"", escaped);
            Assert.DoesNotContain("</script>", escaped); // < and > are escaped
            Assert.DoesNotContain("a=\"1\"", escaped);    // inner double quotes are escaped
        }

        [Fact]
        public void BuildSetAuthTokenScript_IsImmediatelyInvoked()
        {
            var script = ExecuteCommandBridge.BuildSetAuthTokenScript("tok", "url").Trim();

            Assert.StartsWith("(function", script);
            Assert.EndsWith("})();", script);
        }

        [Fact]
        public void BuildCommandCallbackScript_PopsCallbackAndInvokesItWithResult()
        {
            var script = ExecuteCommandBridge.BuildCommandCallbackScript("__cb_42", "{\"ok\":true}");

            Assert.Contains("window.__ideCallbacks__[\"__cb_42\"]", script);
            Assert.Contains("delete window.__ideCallbacks__[\"__cb_42\"]", script);
            Assert.Contains("({\"ok\":true})", script);
        }

        [Fact]
        public void BuildCommandCallbackScript_GuardsOnCallbacksObjectExisting()
        {
            var script = ExecuteCommandBridge.BuildCommandCallbackScript("__cb_1", "null");

            Assert.Contains("if(window.__ideCallbacks__", script);
        }

        [Fact]
        public void BuildCommandCallbackScript_EmitsCallbackIdAsDoubleQuotedLiteral()
        {
            // Defence-in-depth: even a callbackId containing a quote can't break out of the
            // bracket access — it is emitted as a double-quoted, escaped JSON literal.
            var script = ExecuteCommandBridge.BuildCommandCallbackScript("__cb_1'evil", "null");

            Assert.DoesNotContain("['__cb_1'evil']", script); // no single-quoted breakout form
            Assert.Contains("window.__ideCallbacks__[\"", script);
        }
    }
}
