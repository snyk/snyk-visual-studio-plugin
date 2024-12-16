using Snyk.VisualStudio.Extension.Service;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.Settings;

public interface ISnykGeneralOptionsDialogPage
{
    void Initialize(ISnykServiceProvider provider);
    Task HandleAuthenticationSuccess(string token, string apiUrl);
    Task HandleFailedAuthentication(string errorMessage);
    void Authenticate();
}