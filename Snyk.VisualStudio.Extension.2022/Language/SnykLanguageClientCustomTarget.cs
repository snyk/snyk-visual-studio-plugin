using Newtonsoft.Json.Linq;
using Snyk.Code.Library.Api.Dto.Analysis;
using StreamJsonRpc;

namespace Snyk.VisualStudio.Extension.Shared.Language
{
    public class SnykLanguageClientCustomTarget
    {
        [JsonRpcMethod("$/snyk.scan")]
        public void OnSnykScan(JToken arg)
        {
            
        }

        [JsonRpcMethod("$/snyk.getFeatureFlagStatus")]
        public void OnSnykGetFeatureFlagStatusd(JToken arg)
        {

        }

    }
}
