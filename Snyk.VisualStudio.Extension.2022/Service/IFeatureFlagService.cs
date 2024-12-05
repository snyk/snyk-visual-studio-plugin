using System.Threading;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.Service;

public interface IFeatureFlagService
{
    Task RefreshAsync(CancellationToken cancellationToken);
}