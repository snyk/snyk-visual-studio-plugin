using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.Authentication
{
    /// <summary>
    /// Orchestrates the IDE-side auth flow: triggers the LS login, waits for the modal
    /// <c>AuthDialogWindow</c>, and surfaces success / failure to the rest of the extension.
    /// </summary>
    public interface IAuthenticationFlowService
    {
        /// <summary>
        /// Kicks off authentication. If the CLI is missing the caller receives a
        /// <see cref="System.IO.FileNotFoundException"/>; if the LS isn't ready the call
        /// is a no-op. If the user already has a valid token, optionally triggers a scan.
        /// </summary>
        void Authenticate();

        /// <summary>
        /// Called on LS-driven authentication success. Closes the modal auth dialog and
        /// refreshes the Snyk tool window. The visible HTML settings form is updated
        /// separately by <see cref="Snyk.VisualStudio.Extension.Settings.HtmlSettingsControl.UpdateAuthToken"/>.
        /// </summary>
        Task HandleAuthenticationSuccessAsync(string token, string apiUrl);

        /// <summary>
        /// Called on LS-driven authentication failure. Closes the modal auth dialog, fires
        /// an OSS error so the user sees the message in-place, and brings the tool window
        /// forward so the failed state is visible.
        /// </summary>
        Task HandleFailedAuthenticationAsync(string errorMessage);
    }
}
