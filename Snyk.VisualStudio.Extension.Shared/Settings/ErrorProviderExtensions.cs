using System.Data.SqlClient;
using System.Windows.Forms;

namespace Snyk.VisualStudio.Extension.Shared.Settings
{
    public static class ErrorProviderExtensions
    {
        public static void SetNoError(this ErrorProvider errorProvider, Control control) =>
            errorProvider.SetError(control, string.Empty);
    }
}