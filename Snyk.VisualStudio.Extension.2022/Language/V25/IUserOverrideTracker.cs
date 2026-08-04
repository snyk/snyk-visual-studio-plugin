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
    /// Thread-safety: implementations MUST be safe for concurrent access. The tracker is mutated
    /// from the UI thread (settings Save) and from thread-pool continuations (the fire-and-forget
    /// config-send path commits resets after an awaited RPC), so every read and mutation must be
    /// internally synchronised and <see cref="PeekPendingResets"/>/<see cref="Snapshot"/> must
    /// return independent copies.
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
        /// Returns the set of keys that need a reset signal (<c>{value:null, changed:true}</c>) on the
        /// next settings push WITHOUT clearing them (IDE-2152 CP 2.2). Use this from
        /// <see cref="Language.V25.LsSettingsV25.BuildSettingsMap"/> so a transient RPC failure does
        /// not lose the reset intent — the queue is cleared only by <see cref="CommitPendingResets"/>
        /// after a confirmed successful config-update send.
        /// </summary>
        IReadOnlyCollection<string> PeekPendingResets();

        /// <summary>
        /// Removes only the named <paramref name="sentKeys"/> from the pending-reset queue, after they
        /// were confirmed delivered to the LS (IDE-2152 CP 2.2). Never a blanket clear: a newer reset
        /// for the same key enqueued between the peek and the commit is preserved. No-op on null/empty.
        /// </summary>
        void CommitPendingResets(IReadOnlyCollection<string> sentKeys);

        /// <summary>
        /// Applies an explicit user reset to each key in <paramref name="resetKeys"/> (IDE-2152): removes
        /// the local override mark (so the key drops out of <see cref="Snapshot"/>) AND enqueues a
        /// pending reset so the next <see cref="Language.V25.LsSettingsV25.BuildSettingsMap"/> emits
        /// <c>{value:null, changed:true}</c>. Unlike <see cref="Unmark"/>, the reset signal is ALWAYS
        /// enqueued — even when the key had no local mark — because a form reset must still tell the LS
        /// to Unset any <c>user:global</c> override (e.g. an org-pushed value the user wants cleared);
        /// the local un-mark is best-effort. No-op on null/empty. Reset and edit (<see cref="Mark"/>)
        /// are disjoint per save: a key is either edited-to-a-value or reset, never both.
        /// </summary>
        void ApplyUserResets(IReadOnlyCollection<string> resetKeys);

        /// <summary>
        /// Re-queues persisted-but-unconfirmed reset keys on <see cref="ISnykOptionsManager.Load"/>
        /// so a reset applied while the LS was not ready survives a restart and is re-delivered on the
        /// next configuration update (IDE-2152 critical fix #2). Unlike <see cref="ApplyUserResets"/>,
        /// this does NOT un-mark anything (Load hydrates <c>changed</c> separately) and skips any key
        /// that is currently a live override mark — a persisted override means the user re-applied the
        /// key after the reset was queued, so the override wins and no reset is re-emitted. No-op on
        /// null/empty. Preserves the invariant that a key is never in both <c>changed</c> and the
        /// pending-reset queue.
        /// </summary>
        void RehydratePendingResets(IReadOnlyCollection<string> resetKeys);

        /// <summary>
        /// Seeds the tracker from the persisted options on first load (upgrade path): marks any
        /// key whose current value differs from the plugin default.
        /// </summary>
        void SeedFrom(IPersistableOptions options);

        /// <summary>
        /// Called from <see cref="ISnykOptionsManager.Save"/> on the user-edit path: marks every
        /// key in <paramref name="editedKeys"/> as an explicit user override, regardless of whether
        /// its <paramref name="options"/> value equals the plugin default. Any key the settings form
        /// posted is an explicit user choice, so setting a key to a value that happens to equal the
        /// default (e.g. enabling Snyk Code, whose default is <c>true</c>) is still recorded as an
        /// override — reset-to-default is never inferred here from value==default. Keys NOT in
        /// <paramref name="editedKeys"/> are left completely untouched — so org-pushed values that
        /// merely persist in Options are never recorded as user overrides. Pass an empty set to mark
        /// nothing (safe default for callers that cannot enumerate their edited keys).
        /// <para>
        /// Reset-to-default is an explicit user action expressed via <see cref="Unmark"/>, not
        /// derived by this method from the edited value equalling the plugin default.
        /// </para>
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
        /// enqueued (via <see cref="ApplyUserResets"/> or <see cref="Unmark"/>) between saves are
        /// not discarded before <see cref="PeekPendingResets"/> folds them into the next
        /// BuildSettingsMap call (committed on confirmed send success).
        /// </summary>
        void ClearChanged();

        /// <summary>
        /// Clears all marks AND pending resets. Use only when a full reset of tracker state is
        /// required (e.g. unit-test teardown or explicit full reinitialisation).
        /// </summary>
        void Clear();
    }
}
