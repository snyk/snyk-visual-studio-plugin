using Snyk.Common.Settings;
using StreamJsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.Shared.Language
{
    public interface ILanguageClientManager
    {
        void SetSnykOptions(ISnykOptions options);
        Task StartServerAsync();
        Task RestartServerAsync();
        Task StopServerAsync();
        bool IsReady { get; set; }
        JsonRpc Rpc { get; set; }
        Task<object> InvokeWorkspaceScanAsync(CancellationToken cancellationToken);
    }
}
