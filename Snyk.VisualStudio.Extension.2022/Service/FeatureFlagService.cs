using System.Threading.Tasks;
using Serilog;
using Snyk.Common;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.Service;

public class FeatureFlagService
{
    private readonly ILanguageClientManager languageClient;
    private readonly ISnykOptions settings;
    private static readonly ILogger Logger = LogManager.ForContext<FeatureFlagService>();
    private static FeatureFlagService instance;

    public FeatureFlagService(ILanguageClientManager languageClient, ISnykOptions settings)
    {
        this.languageClient = languageClient;
        this.settings = settings;
    }

    public static FeatureFlagService Instance => instance;

    /// <summary>
    /// Initialize service.
    /// </summary>
    /// <param name="languageClient"></param>
    /// <param name="settings"></param>
    /// <returns>Task.</returns>
    public static FeatureFlagService Initialize(ILanguageClientManager languageClient, ISnykOptions settings)
    {
        if (instance != null)
            return instance;

        instance = new FeatureFlagService(languageClient, settings);

        Logger.Information("FeatureFlagService initialized");
        return instance;
    }

    public async Task RefreshAsync()
    {
        var result = await languageClient.InvokeGetFeatureFlagStatusAsync(LsConstants.SnykConsistentIgnoresEnabled, SnykVSPackage.Instance.DisposalToken);
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