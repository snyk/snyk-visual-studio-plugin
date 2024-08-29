using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Serilog;
using Snyk.Common;
using Snyk.VisualStudio.Extension.Service;
using StreamJsonRpc;

namespace Snyk.VisualStudio.Extension.Language
{
    public class SnykLanguageClientCustomTarget
    {
        private readonly ISnykServiceProvider serviceProvider;
        private readonly IDictionary<string, IEnumerable<Issue>> snykIssueDictionary = new Dictionary<string, IEnumerable<Issue>>();
        private static readonly ILogger _logger = LogManager.ForContext<SnykLanguageClientCustomTarget>();
        public SnykLanguageClientCustomTarget(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        [JsonRpcMethod("$/snyk.publishDiagnostics316")]
        public void OnPublishDiagnostics316(JToken arg)
        {
            var uri = arg["uri"];
            var diagnosticsArray = (JArray)arg["diagnostics"];
            if (uri == null)
            {
                return;
            }

            if (diagnosticsArray == null || diagnosticsArray.Count == 0)
            {
                snykIssueDictionary.Remove(uri.ToString());
                return;
            }

            var dataList = diagnosticsArray.Where(x => x["data"] != null).Select(x => x["data"].TryParse<Issue>());
            snykIssueDictionary.Add(uri.ToString(), dataList);
        }

        [JsonRpcMethod("$/snyk.scan")]
        public void OnSnykScan(JToken arg)
        {
            var lspAnalysisResult = arg.TryParse<LspAnalysisResult>();
            if (lspAnalysisResult == null) return;
            if (lspAnalysisResult.Status != "success")
            {
                return;
            }
        }

        [JsonRpcMethod("$/snyk.getFeatureFlagStatus")]
        public void OnSnykGetFeatureFlagStatusd(JToken arg)
        {

        }
    }
}
