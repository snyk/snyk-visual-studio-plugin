using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Snyk.VisualStudio.Extension.Analytics;
using StreamJsonRpc;

namespace Snyk.VisualStudio.Extension.Language
{
    public interface ILanguageClientManager
    {
        Task StartServerAsync(bool shouldStart = false);
        Task RestartServerAsync();
        Task StopServerAsync();
        bool IsReady { get; set; }
        JsonRpc Rpc { get; set; }
        Task<object> InvokeWorkspaceScanAsync(CancellationToken cancellationToken);
        Task<SastSettings> InvokeGetSastEnabled(CancellationToken cancellationToken);
        Task<string> InvokeLogin(CancellationToken cancellationToken);
        Task<object> InvokeLogout(CancellationToken cancellationToken);
        Task<object> DidChangeConfigurationAsync(CancellationToken cancellationToken);
        Task<string> InvokeCopyLinkAsync(CancellationToken cancellationToken);
        Task<string> InvokeGenerateIssueDescriptionAsync(string issueId, CancellationToken cancellationToken);
        event AsyncEventHandler<SnykLanguageServerEventArgs> OnLanguageServerReadyAsync;
        event AsyncEventHandler<SnykLanguageServerEventArgs> OnLanguageClientNotInitializedAsync;
        void FireOnLanguageClientNotInitializedAsync();
        Task InvokeReportAnalyticsAsync(IAbstractAnalyticsEvent analyticsEvent, CancellationToken cancellationToken);
        Task<FeatureFlagResponse> InvokeGetFeatureFlagStatusAsync(string featureFlagName, CancellationToken cancellationToken);
    }
}
