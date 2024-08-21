using Newtonsoft.Json.Linq;
using Serilog;
using Snyk.Common;
using StreamJsonRpc;

namespace Snyk.VisualStudio.Extension.Language
{
    public class SnykLanguageClientCustomTarget
    {
        private static readonly ILogger _logger = LogManager.ForContext<SnykLanguageClientCustomTarget>();
        public SnykLanguageClientCustomTarget()
        {
        }

        [JsonRpcMethod("$/snyk.scan")]
        public void OnSnykScan(JToken arg)
        {
            var lspAnalysisResult = arg.TryParse<LspAnalysisResult>();
            if (lspAnalysisResult == null) return;
        }

        [JsonRpcMethod("$/snyk.getFeatureFlagStatus")]
        public void OnSnykGetFeatureFlagStatusd(JToken arg)
        {

        }
    }
}
