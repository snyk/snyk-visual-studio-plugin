// ABOUTME: Interface for tracking which global LSP config keys the user has explicitly overridden
// ABOUTME: from their plugin defaults. Used by BuildSettingsMap to set the `changed` flag (IDE-2152).
using System.Collections.Generic;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Language
{
    /// <summary>
    /// Tracks which global pflag keys the user has explicitly set away from plugin defaults.
    /// Keys for which <see cref="PflagKeys.IsAlwaysChanged"/> returns true are always treated as
    /// changed regardless of user action.
    /// Thread-safety: implementations are single-threaded (UI thread / settings thread); no
    /// concurrent access guarantee is required.
    /// </summary>
    public interface IUserOverrideTracker
    {
        /// <summary>
        /// Returns true once the tracker has been hydrated from persistence (via
        /// <see cref="SeedFrom"/> or the explicit Mark-loop in
        /// <see cref="ISnykOptionsManager.Load"/>). False on a freshly constructed instance.
        /// <para>
        /// <see cref="Language.V25.LsSettingsV25.BuildSettingsMap"/> gates on this: an unseeded
        /// tracker falls back to <c>changed:true</c> for every key (the safe pre-IDE-2152
        /// behaviour) so a startup-ordering race cannot silently downgrade user overrides.
        /// </para>
        /// <para>
        /// Neither <see cref="ClearChanged"/> nor <see cref="Clear"/> resets this flag; once
        /// seeded, the tracker remains seeded across reload cycles.
        /// </para>
        /// </summary>
        bool IsSeeded { get; }

        /// <summary>
        /// Returns true when the key was explicitly overridden by the user, or
        /// <see cref="PflagKeys.IsAlwaysChanged"/> returns true for it.
        /// </summary>
        bool IsChanged(string key);

        /// <summary>
        /// Marks <paramref name="key"/> as explicitly overridden.
        /// </summary>
        void Mark(string key);

        /// <summary>
        /// Unmarks <paramref name="key"/> and enqueues it in the pending-reset set so the next
        /// <see cref="ILsSettingsV25.BuildSettingsMap"/> call can emit <c>{value:null, changed:true}</c>.
        /// No-op when the key was not previously marked.
        /// </summary>
        void Unmark(string key);

        /// <summary>
        /// Returns and clears the set of keys that need a reset signal (<c>{value:null, changed:true}</c>)
        /// on the next settings push. Idempotent: second call returns empty set.
        /// </summary>
        IReadOnlyCollection<string> ConsumePendingResets();

        /// <summary>
        /// Seeds the tracker from the persisted options on first load (upgrade path): marks any
        /// key whose current value differs from the plugin default.
        /// </summary>
        void SeedFrom(IPersistableOptions options);

        /// <summary>
        /// Called from <see cref="ISnykOptionsManager.Save"/> on the user-edit path: marks each
        /// key in <paramref name="editedKeys"/> whose <paramref name="options"/> value deviates
        /// from the plugin default, and unmarks + enqueues a reset for each edited key whose value
        /// equals the default. Keys NOT in <paramref name="editedKeys"/> are left completely
        /// untouched — so org-pushed values that merely persist in Options are never recorded as
        /// user overrides. Pass an empty set to mark nothing (safe default for callers that cannot
        /// enumerate their edited keys).
        /// </summary>
        void ApplyUserEdits(IPersistableOptions options, System.Collections.Generic.IReadOnlyCollection<string> editedKeys);

        /// <summary>
        /// Returns a snapshot of the currently-marked keys (for persisting to
        /// <see cref="SnykSettings.ChangedConfigKeys"/>). Returns a concrete HashSet so callers
        /// that need to assign to a <c>HashSet&lt;string&gt;</c> field (e.g. SnykSettings) do not
        /// require an explicit cast (ISet→HashSet has no implicit conversion in C#).
        /// </summary>
        System.Collections.Generic.HashSet<string> Snapshot();

        /// <summary>
        /// Marks the tracker as seeded. Called by <see cref="ISnykOptionsManager.Load"/> after
        /// the explicit persisted-keys Mark-loop completes, so that
        /// <see cref="Language.V25.LsSettingsV25.BuildSettingsMap"/>'s <c>IsSeeded</c> gate
        /// becomes active. (The <see cref="SeedFrom"/> path sets the flag internally; this method
        /// covers the persisted-keys branch in Load which calls Mark in a loop instead.)
        /// </summary>
        void MarkSeeded();

        /// <summary>
        /// Clears only the <c>changed</c> marks, preserving the pending-reset queue.
        /// Call at the start of <see cref="ISnykOptionsManager.Load"/> hydration so that a second
        /// Load on the same manager never unions stale marks, while any reset signals that were
        /// enqueued (via <see cref="ApplyUserEdits"/> or <see cref="Unmark"/>) between saves are
        /// not discarded before <see cref="ConsumePendingResets"/> picks them up in the next
        /// BuildSettingsMap call.
        /// </summary>
        void ClearChanged();

        /// <summary>
        /// Clears all marks AND pending resets. Use only when a full reset of tracker state is
        /// required (e.g. unit-test teardown or explicit full reinitialisation).
        /// </summary>
        void Clear();
    }
}
