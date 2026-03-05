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
        public void BuildClientScript_CallsWindowExternalIdeExecuteCommand()
        {
            var script = ExecuteCommandBridge.BuildClientScript();
            Assert.Contains("window.external.__ideExecuteCommand__", script);
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
    }
}
