// ABOUTME: Thread-safe single-slot holder for the latest LS-issued auth token awaiting delivery to the settings page.
// ABOUTME: Extracted from HtmlSettingsControl so the take-once / last-write-wins semantics are unit-testable.

using System.Threading;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>The most recent LS-issued token/apiUrl pair queued for the settings page.</summary>
    public sealed class PendingAuthToken
    {
        public PendingAuthToken(string token, string apiUrl)
        {
            this.Token = token;
            this.ApiUrl = apiUrl;
        }

        public string Token { get; }

        public string ApiUrl { get; }
    }

    /// <summary>
    /// Single-slot, last-write-wins, take-once holder for the auth token that
    /// <see cref="HtmlSettingsControl"/> delivers to the settings page once it is ready. The auth
    /// callback can arrive when no page is open (or before its HTML loads), so the token is parked
    /// here and consumed by whichever control next becomes page-ready — exactly once. Access is via
    /// <see cref="Interlocked"/> because the LS callback thread sets while the UI thread takes.
    /// </summary>
    public sealed class PendingAuthTokenSlot
    {
        private PendingAuthToken pending;

        /// <summary>Stores the latest token, replacing any previously-queued one (last write wins).</summary>
        public void Set(string token, string apiUrl) =>
            Interlocked.Exchange(ref this.pending, new PendingAuthToken(token, apiUrl));

        /// <summary>Atomically removes and returns the queued token, or <c>null</c> if none is queued.</summary>
        public PendingAuthToken Take() => Interlocked.Exchange(ref this.pending, null);
    }
}
