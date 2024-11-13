using System.Threading.Tasks;
using Serilog;
using Snyk.Common;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.Service;

public class FeatureFlagService(ILanguageClientManager languageClient, ISnykOptions settings)
{
    private readonly ILogger logger = LogManager.ForContext<FeatureFlagService>();

    public async Task OnLanguageServerReadyAsync(object sender, SnykLanguageServerEventArgs args)
    {
        var result = await languageClient.InvokeGetFeatureFlagStatusAsync(LsConstants.SnykGetFeatureFlagStatus, SnykVSPackage.Instance.DisposalToken);
        var response = Json.Deserialize<FeatureFlagResponse>(result);
        settings.ConsistentIgnoresEnabled = response.Ok;
        if (!response.Ok)
        {
            if (!response.UserMessage.IsNullOrEmpty()) this.logger.Error("feature flag not enabled: {UserMessage}", response.UserMessage);
        }
    }
    
    private class FeatureFlagResponse
    {
        public bool Ok { get; set; }
        public string UserMessage { get; set; }
    }
}