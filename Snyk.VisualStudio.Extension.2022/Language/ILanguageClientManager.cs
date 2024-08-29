using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Snyk.VisualStudio.Extension.Language
{
    public interface ILanguageClientManager
    {
        Task StartServerAsync();
        Task RestartServerAsync();
        Task StopServerAsync();
        bool IsReady { get; set; }
        JsonRpc Rpc { get; set; }
        Task<object> InvokeWorkspaceScanAsync(CancellationToken cancellationToken);
    }
}
