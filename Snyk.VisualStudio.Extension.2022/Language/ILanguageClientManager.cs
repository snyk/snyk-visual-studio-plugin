using System.Threading;
using System.Threading.Tasks;
using Snyk.Common.Settings;
using StreamJsonRpc;

namespace Snyk.VisualStudio.Extension.Language
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
