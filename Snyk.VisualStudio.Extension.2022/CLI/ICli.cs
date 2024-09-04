using System.Security.Authentication;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.CLI
{
    /// <summary>
    /// Describe Snyk CLI interface common methods.
    /// </summary>
    public interface ICli
    {
        bool IsCliFileFound();
        string GetCliPath();
    }
}
