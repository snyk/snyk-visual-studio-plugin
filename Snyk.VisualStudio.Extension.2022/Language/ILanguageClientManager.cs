using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Snyk.Common;
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
        event AsyncEventHandler<SnykLanguageServerEventArgs> OnLanguageServerReadyAsync;
        event AsyncEventHandler<SnykLanguageServerEventArgs> OnLanguageClientNotInitializedAsync;
    }
}
