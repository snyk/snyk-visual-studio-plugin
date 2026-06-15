// ABOUTME: Abstraction over the modal auth dialog so AuthenticationFlowService can be tested
// ABOUTME: without constructing the WPF AuthDialogWindow singleton.

namespace Snyk.VisualStudio.Extension.Authentication
{
    /// <summary>
    /// The auth-flow's view of the modal authentication dialog. Implemented by
    /// <see cref="AuthDialogWindow"/>; injected into <see cref="AuthenticationFlowService"/> so the
    /// orchestration can be unit-tested with a fake. See <see cref="AuthDialogState"/> for the
    /// show/hide race-guard semantics these methods drive.
    /// </summary>
    public interface IAuthDialog
    {
        /// <summary>Resets the show/hide guard at the start of an authentication attempt.</summary>
        void ArmForShow();

        /// <summary>Shows the modal dialog unless the result already arrived or it is already visible.</summary>
        void ShowDialogForAuth();

        /// <summary>Hides the dialog because the auth result arrived (and suppresses a pending show).</summary>
        void HideForAuthResult();
    }
}
