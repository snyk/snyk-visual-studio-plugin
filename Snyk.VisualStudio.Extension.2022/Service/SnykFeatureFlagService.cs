using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Snyk.VisualStudio.Extension.Extension;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Service;

public class SnykFeatureFlagService : IFeatureFlagService
{
    private readonly ILanguageClientManager languageClient;
    private readonly ISnykOptions settings;
    private static readonly ILogger Logger = LogManager.ForContext<SnykFeatureFlagService>();

    public SnykFeatureFlagService(ILanguageClientManager languageClient, ISnykOptions settings)
    {
        this.languageClient = languageClient;
        this.settings = settings;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var result = await languageClient.InvokeGetFeatureFlagStatusAsync(LsConstants.SnykConsistentIgnoresEnabled, cancellationToken);
        if (result == null)
        {
            settings.ConsistentIgnoresEnabled = false;
            return;
        }
        settings.ConsistentIgnoresEnabled = result.Ok;
        if (!result.Ok)
        {
            if (!result.UserMessage.IsNullOrEmpty()) Logger.Error("feature flag not enabled: {UserMessage}", result.UserMessage);
        }
    }
}