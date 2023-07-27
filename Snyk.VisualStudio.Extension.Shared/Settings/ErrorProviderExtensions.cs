using System.Data.SqlClient;

namespace Snyk.VisualStudio.Extension.Shared.Settings;

using System.Windows.Forms;

public static class ErrorProviderExtensions
{
    public static void SetNoError(this ErrorProvider errorProvider, Control control) =>
        errorProvider.SetError(control, string.Empty);
}