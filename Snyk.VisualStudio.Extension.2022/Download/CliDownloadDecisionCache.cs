// ABOUTME: Short-lived cache for the "does the CLI need downloading?" decision.
// ABOUTME: Startup asks the same question several times; each answer costs a network call.
using System;

namespace Snyk.VisualStudio.Extension.Download
{
    /// <summary>
    /// Remembers the most recent CLI-download decision so a burst of callers shares one answer.
    /// <para>
    /// Startup asks four times within a few seconds — the package gate, <c>OnLoadedAsync</c> (which
    /// Visual Studio raises more than once), and the download task itself — and every answer costs a
    /// request to the release endpoint plus a launch of the CLI to read its protocol version.
    /// </para>
    /// <para>
    /// A new CLI release must still be picked up, so the entry is deliberately fragile: it is keyed on
    /// everything the decision depends on (so editing the path, mirror or channel discards it, as does
    /// recording a newly installed version) and it expires after <see cref="Ttl"/>. Anything longer
    /// would trade correctness for a saving that only matters during startup.
    /// </para>
    /// </summary>
    internal sealed class CliDownloadDecisionCache
    {
        /// <summary>
        /// Long enough to cover the startup burst, short enough that a release published while Visual
        /// Studio is open is still noticed by the next check.
        /// </summary>
        internal static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

        private readonly Func<DateTime> utcNow;
        private readonly object gate = new object();

        private string cachedKey;
        private bool cachedDecision;
        private DateTime cachedAt;

        public CliDownloadDecisionCache()
            : this(() => DateTime.UtcNow)
        {
        }

        // Clock injected so the expiry can be tested without sleeping.
        internal CliDownloadDecisionCache(Func<DateTime> utcNow)
        {
            this.utcNow = utcNow;
        }

        /// <summary>
        /// Builds the cache key. Any change to a value the decision reads must produce a different key,
        /// otherwise a stale answer outlives the state it was computed from — recording a freshly
        /// installed version is what retires the "download needed" answer that triggered it.
        /// </summary>
        public static string BuildKey(string resolvedCliPath, string baseDownloadUrl, string releaseChannel, string currentCliVersion) =>
            // Unit separator, written as an escape so it stays visible in source: it cannot occur
            // in a Windows path, a URL or a version string, so adjacent fields can never run
            // together and yield a single key for two different states.
            string.Join(
                "\u001f",
                resolvedCliPath ?? string.Empty,
                baseDownloadUrl ?? string.Empty,
                releaseChannel ?? string.Empty,
                currentCliVersion ?? string.Empty);

        /// <summary>
        /// The remembered decision for <paramref name="key"/>, if it has not expired.
        /// </summary>
        public bool TryGet(string key, out bool downloadNeeded)
        {
            lock (this.gate)
            {
                if (this.cachedKey != key)
                {
                    downloadNeeded = false;

                    return false;
                }

                if (this.utcNow() - this.cachedAt >= Ttl)
                {
                    downloadNeeded = false;

                    return false;
                }

                downloadNeeded = this.cachedDecision;

                return true;
            }
        }

        public void Set(string key, bool downloadNeeded)
        {
            lock (this.gate)
            {
                this.cachedKey = key;
                this.cachedDecision = downloadNeeded;
                this.cachedAt = this.utcNow();
            }
        }

        /// <summary>
        /// Drops the entry. Used when something happened that the key cannot see — a completed install,
        /// for instance, replaces the binary the protocol probe was run against.
        /// </summary>
        public void Invalidate()
        {
            lock (this.gate)
            {
                this.cachedKey = null;
            }
        }
    }
}
