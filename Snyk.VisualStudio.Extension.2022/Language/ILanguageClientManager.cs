using System.Threading;
using System.Threading.Tasks;
using Snyk.Common;
using Snyk.Common.Service;
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
    }
}
