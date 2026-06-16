// ABOUTME: Pure show/hide race-guard state for the auth dialog, extracted from AuthDialogWindow.
// ABOUTME: WPF-free so the guard semantics can be unit-tested without constructing a window.

namespace Snyk.VisualStudio.Extension
{
    /// <summary>
    /// Guards the show/hide race in the auth flow: login runs fire-and-forget while the modal
    /// <c>ShowDialog()</c> is started separately, so a fast success/failure can record the result
    /// <em>before</em> the show runs. <see cref="Arm"/> resets the guard at the start of an attempt;
    /// <see cref="RecordResult"/> marks that the result arrived; <see cref="ShouldShow"/> decides
    /// whether the modal show should still proceed.
    /// <para>
    /// The flag is <c>volatile</c> because it is written from the (possibly non-UI) thread that
    /// starts the auth attempt and read on the UI thread that runs the show.
    /// </para>
    /// </summary>
    public sealed class AuthDialogState
    {
        private volatile bool resultArrived;

        /// <summary>True once <see cref="RecordResult"/> has been called since the last <see cref="Arm"/>.</summary>
        public bool ResultArrived => this.resultArrived;

        /// <summary>Resets the guard at the start of an authentication attempt.</summary>
        public void Arm() => this.resultArrived = false;

        /// <summary>Records that the auth result (success or failure) has arrived.</summary>
        public void RecordResult() => this.resultArrived = true;

        /// <summary>
        /// Whether the modal auth dialog should be shown: only when no result has arrived yet
        /// (otherwise the show would strand a dialog nothing will close) and it is not already
        /// visible (re-showing a visible modal throws).
        /// </summary>
        /// <param name="isVisible">Whether the dialog is currently visible.</param>
        public bool ShouldShow(bool isVisible) => !this.resultArrived && !isVisible;
    }
}
