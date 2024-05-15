using StreamJsonRpc;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.Shared.Language
{
    public interface ILanguageClientManager
    {
        void SetOptions(string cliPath, string token);
        Task StartServerAsync();
        Task RestartServerAsync();
        Task StopServerAsync();
        bool IsReady { get; set; }
        JsonRpc Rpc { get; set; }
    }
}
