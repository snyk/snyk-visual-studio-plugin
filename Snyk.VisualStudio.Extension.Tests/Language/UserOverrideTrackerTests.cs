// ABOUTME: Unit tests for UserOverrideTracker covering UNIT-001..010 from the IDE-2152 test plan.
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    public class UserOverrideTrackerTests
    {
        private readonly UserOverrideTracker sut = new UserOverrideTracker();

        // Helper: build a mock options with all global keys at their plugin defaults.
        private static Mock<ISnykOptions> DefaultOptions()
        {
            var o = new Mock<ISnykOptions>();
            o.SetupGet(x => x.OssEnabled).Returns(true);
            o.SetupGet(x => x.SnykCodeSecurityEnabled).Returns(true);
            o.SetupGet(x => x.IacEnabled).Returns(true);
            o.SetupGet(x => x.SecretsEnabled).Returns(false);
            o.SetupGet(x => x.AutoScan).Returns(true);
            o.SetupGet(x => x.EnableDeltaFindings).Returns(false);
            o.SetupGet(x => x.FilterCritical).Returns(true);
            o.SetupGet(x => x.FilterHigh).Returns(true);
            o.SetupGet(x => x.FilterMedium).Returns(true);
            o.SetupGet(x => x.FilterLow).Returns(true);
            o.SetupGet(x => x.OpenIssuesEnabled).Returns(true);
            o.SetupGet(x => x.IgnoredIssuesEnabled).Returns(false);
            o.SetupGet(x => x.CustomEndpoint).Returns((string)null);
            o.SetupGet(x => x.Organization).Returns((string)null);
            o.SetupGet(x => x.IgnoreUnknownCA).Returns(false);
            o.SetupGet(x => x.BinariesAutoUpdate).Returns(true);
            o.SetupGet(x => x.CliCustomPath).Returns(string.Empty);
            o.SetupGet(x => x.CliReleaseChannel).Returns(SnykCliDownloader.DefaultReleaseChannel);
            o.SetupGet(x => x.CliBaseDownloadURL).Returns(SnykCliDownloader.DefaultBaseDownloadUrl);
            o.SetupGet(x => x.AdditionalEnv).Returns(string.Empty);
            o.SetupGet(x => x.AdditionalParameters).Returns(new List<string>());
            o.SetupGet(x => x.RiskScoreThreshold).Returns((int?)null);
            o.SetupGet(x => x.TrustedFolders).Returns(new System.Collections.Generic.HashSet<string>());
            // Token and AuthenticationMethod at their defaults.
            o.SetupGet(x => x.ApiToken).Returns(
                new AuthenticationToken(AuthenticationType.OAuth, string.Empty));
            o.SetupGet(x => x.AuthenticationMethod).Returns(default(AuthenticationType));
            return o;
        }

        // UNIT-001: IsChanged returns true for a key that is in the tracked (marked) set.
        [Fact]
        public void IsChanged_TrackedKey_True()
        {
            sut.Mark(PflagKeys.SnykOssEnabled);
            Assert.True(sut.IsChanged(PflagKeys.SnykOssEnabled));
        }

        // UNIT-002: IsChanged returns true for a key that is in AlwaysChanged even if never marked.
        [Fact]
        public void IsChanged_AlwaysChangedKey_True()
        {
            Assert.True(sut.IsChanged(PflagKeys.TrustedFolders));
        }

        // UNIT-003: IsChanged returns false for a key that is neither marked nor always-changed.
        [Fact]
        public void IsChanged_UntrackedKey_False()
        {
            Assert.False(sut.IsChanged(PflagKeys.SnykOssEnabled));
        }

        // UNIT-004: Unmark enqueues a pending reset; PeekPendingResets returns it, and after
        // CommitPendingResets confirms delivery the queue no longer contains it.
        [Fact]
        public void Unmark_EnqueuesPendingReset_ClearedAfterCommit()
        {
            sut.Mark(PflagKeys.SnykOssEnabled);
            sut.Unmark(PflagKeys.SnykOssEnabled);

            var first = sut.PeekPendingResets();
            Assert.Contains(PflagKeys.SnykOssEnabled, first);

            sut.CommitPendingResets(first);

            var second = sut.PeekPendingResets();
            Assert.Empty(second);
        }

        // R6-1: SeedFrom has replace-semantics — a prior Mark is cleared when SeedFrom is called
        // with all-default options, even without an explicit ClearChanged() before SeedFrom.
        // Confirms SeedFrom is self-consistent and a future direct caller cannot accumulate stale marks.
        [Fact]
        public void SeedFrom_ClearsPriorMarks_ReplaceSemantics()
        {
            // Prime with a non-default mark.
            sut.Mark(PflagKeys.SnykOssEnabled);
            Assert.True(sut.IsChanged(PflagKeys.SnykOssEnabled), "Precondition: key must be marked");

            // SeedFrom with all-default options — should clear the prior mark.
            sut.SeedFrom(DefaultOptions().Object);

            Assert.Empty(sut.Snapshot()); // SeedFrom with all-default options must clear prior marks
            Assert.False(sut.IsChanged(PflagKeys.SnykOssEnabled),
                "A key marked before SeedFrom(all-defaults) must no longer be considered changed");
        }

        // R7-2: SeedFrom clears pendingResets (via Clear(), not just ClearChanged()).
        // A pending reset enqueued before SeedFrom must NOT survive into the next BuildSettingsMap call;
        // emitting Reset() for a key the user never touched would send spurious {value:null, changed:true}
        // signals to the LS on the very first DidChangeConfiguration after startup.
        [Fact]
        public void SeedFrom_ClearsPendingResets_NotJustChangedSet()
        {
            // Enqueue a pending reset: Mark → Unmark leaves a reset in the queue.
            sut.Mark(PflagKeys.SnykOssEnabled);
            sut.Unmark(PflagKeys.SnykOssEnabled); // pendingResets now contains SnykOssEnabled

            // SeedFrom with all-default options (simulates upgrade / first-load path).
            sut.SeedFrom(DefaultOptions().Object);

            // The pending reset must be gone — Clear() at the top of SeedFrom drains it.
            var resets = sut.PeekPendingResets();
            Assert.DoesNotContain(PflagKeys.SnykOssEnabled, resets); // SeedFrom must drain pendingResets
        }

        // UNIT-005: SeedFrom marks only keys whose persisted value differs from the plugin default.
        [Fact]
        public void SeedFrom_MarksOnlyNonDefaultKeys()
        {
            // All at plugin defaults except OssEnabled which is set to false (non-default).
            var options = DefaultOptions();
            options.SetupGet(o => o.OssEnabled).Returns(false);

            sut.SeedFrom(options.Object);

            Assert.True(sut.IsChanged(PflagKeys.SnykOssEnabled), "Non-default OssEnabled should be marked");
            Assert.False(sut.IsChanged(PflagKeys.SnykCodeEnabled), "Default SnykCodeEnabled should NOT be marked");
        }

        // UNIT-006 (corrected semantics, PR #515): ApplyUserEdits marks every edited key — even one
        // whose value equals the plugin default — and never infers a reset from value==default.
        // Setting a key to its default value via the form is still an explicit user choice.
        [Fact]
        public void ApplyUserEdits_EditedKeyAtDefaultValue_StaysMarked_NoInferredReset()
        {
            // Prime the tracker: OssEnabled was previously overridden.
            sut.Mark(PflagKeys.SnykOssEnabled);
            Assert.True(sut.IsChanged(PflagKeys.SnykOssEnabled));

            // User re-applies OssEnabled at the default (true) through the form; it is in the delta.
            var options = DefaultOptions();
            options.SetupGet(o => o.OssEnabled).Returns(true); // equals default

            sut.ApplyUserEdits(options.Object, new List<string> { PflagKeys.SnykOssEnabled });

            // Edited key is an explicit user choice → remains marked; no reset inferred from default.
            Assert.True(sut.IsChanged(PflagKeys.SnykOssEnabled),
                "An edited key at the default value must remain marked — reset is not inferred here");
            var pending = sut.PeekPendingResets();
            Assert.DoesNotContain(PflagKeys.SnykOssEnabled, pending);
        }

        // UNIT-009 (finding 1): SeedFrom marks Token when ApiToken is non-empty,
        // and AuthenticationMethod when non-default. Does not mark when at defaults.
        [Fact]
        public void SeedFrom_NonDefaultTokenAndAuthMethod_AreBothMarked()
        {
            var options = DefaultOptions();
            options.SetupGet(x => x.ApiToken).Returns(
                new AuthenticationToken(AuthenticationType.Pat, "my-secret-token"));
            options.SetupGet(x => x.AuthenticationMethod).Returns(AuthenticationType.Pat);

            sut.SeedFrom(options.Object);

            Assert.True(sut.IsChanged(PflagKeys.Token),
                "Non-empty token should be marked as changed");
            Assert.True(sut.IsChanged(PflagKeys.AuthenticationMethod),
                "Non-default auth method should be marked as changed");
        }

        [Fact]
        public void SeedFrom_DefaultTokenAndAuthMethod_AreNotMarked()
        {
            var options = DefaultOptions();
            // ApiToken = empty string (default), AuthenticationMethod = OAuth (default)

            sut.SeedFrom(options.Object);

            Assert.False(sut.IsChanged(PflagKeys.Token),
                "Empty token (default) should NOT be marked as changed");
            Assert.False(sut.IsChanged(PflagKeys.AuthenticationMethod),
                "Default auth method (OAuth) should NOT be marked as changed");
        }

        // UNIT-010 (corrected semantics, PR #515): ApplyUserEdits with the RiskScoreThreshold key in
        // the edit delta marks it as an explicit user choice and does NOT infer a reset from
        // value==default (null). Reset-to-default is no longer derived inside ApplyUserEdits.
        [Fact]
        public void ApplyUserEdits_RiskScoreThresholdEdited_MarksChanged_NoInferredReset()
        {
            // User submits the risk-score field; it is in the edit delta with the default (null) value.
            var options = DefaultOptions();
            options.SetupGet(x => x.RiskScoreThreshold).Returns((int?)null);

            sut.ApplyUserEdits(options.Object, new List<string> { PflagKeys.RiskScoreThreshold });

            Assert.True(sut.IsChanged(PflagKeys.RiskScoreThreshold),
                "An edited RiskScoreThreshold key is an explicit user choice and must be marked");
            var pending = sut.PeekPendingResets();
            Assert.DoesNotContain(PflagKeys.RiskScoreThreshold, pending); // no inferred reset from value==default
        }

        // UNIT-011 (finding 4): Calling Clear() then re-seeding reflects only new data.
        [Fact]
        public void Clear_ThenReSeed_ReflectsOnlyNewData()
        {
            // First seed: OssEnabled non-default.
            sut.Mark(PflagKeys.SnykOssEnabled);
            Assert.True(sut.IsChanged(PflagKeys.SnykOssEnabled));

            // Clear and re-seed with all defaults.
            sut.Clear();
            var allDefaults = DefaultOptions();
            sut.SeedFrom(allDefaults.Object);

            // After clear + re-seed with defaults, OssEnabled should no longer be marked.
            Assert.False(sut.IsChanged(PflagKeys.SnykOssEnabled),
                "After Clear(), stale marks from first seed should be gone");
        }

        // S1: SeedFrom with null CliBaseDownloadURL / CliReleaseChannel must NOT mark those
        // keys changed — null should fall back to the canonical defaults, not to "" which
        // would differ from ConfigDefaults[BinaryBaseUrl/CliReleaseChannel].
        [Fact]
        public void SeedFrom_NullCliUrlAndChannel_NotMarkedChanged()
        {
            var options = DefaultOptions();
            // Simulate SnykOptions with no initializer-set value for these fields.
            options.SetupGet(x => x.CliBaseDownloadURL).Returns((string)null);
            options.SetupGet(x => x.CliReleaseChannel).Returns((string)null);

            sut.SeedFrom(options.Object);

            Assert.False(sut.IsChanged(PflagKeys.BinaryBaseUrl),
                "null CliBaseDownloadURL must fall back to canonical default, not be marked changed");
            Assert.False(sut.IsChanged(PflagKeys.CliReleaseChannel),
                "null CliReleaseChannel must fall back to canonical default, not be marked changed");
        }

        // S3: GetGlobalKeyValues must cover exactly the same Cs()-wrapped keys as BuildSettingsMap,
        // excluding the always-Of() keys (DeviceId, ClientProtocolVersion) which are never tracked.
        // This test fails if a key is added to one site but forgotten at the other.
        [Fact]
        public void GetGlobalKeyValues_CoversExactlySameKeysAsBuiltSettingsMapTrackerKeys()
        {
            // Keys BuildSettingsMap passes through Cs() — the tracker-gated keys.
            // DeviceId and ClientProtocolVersion are excluded (always sent via ConfigSetting.Of).
            var buildSettingsMapTrackerKeys = new System.Collections.Generic.HashSet<string>
            {
                PflagKeys.SnykOssEnabled,
                PflagKeys.SnykCodeEnabled,
                PflagKeys.SnykIacEnabled,
                PflagKeys.SnykSecretsEnabled,
                PflagKeys.ScanAutomatic,
                PflagKeys.ScanNetNew,
                PflagKeys.SeverityFilterCritical,
                PflagKeys.SeverityFilterHigh,
                PflagKeys.SeverityFilterMedium,
                PflagKeys.SeverityFilterLow,
                PflagKeys.IssueViewOpenIssues,
                PflagKeys.IssueViewIgnoredIssues,
                PflagKeys.ApiEndpoint,
                PflagKeys.Token,
                PflagKeys.Organization,
                PflagKeys.AuthenticationMethod,
                PflagKeys.ProxyInsecure,
                PflagKeys.AutomaticDownload,
                PflagKeys.CliPath,
                PflagKeys.BinaryBaseUrl,
                PflagKeys.CliReleaseChannel,
                PflagKeys.TrustedFolders,
                PflagKeys.AdditionalEnvironment,
                PflagKeys.AdditionalParameters,
                PflagKeys.RiskScoreThreshold,
            };

            // Keys yielded by GetGlobalKeyValues (via SeedFrom on a fresh tracker).
            // We drive it through SeedFrom to collect what GetGlobalKeyValues actually yields.
            // Use a custom ISnykOptions that records which keys were touched is complex —
            // instead call SeedFrom with all-defaults-plus-one-non-default-per-key and inspect
            // via reflection. Simpler: call SeedFrom with all non-defaults, then Snapshot gives
            // exactly the yielded keys (any key not yielded would not be in the snapshot).
            var trackerForKeys = new UserOverrideTracker();

            // Build options where every value is non-default so every key gets marked.
            var allNonDefault = new Mock<ISnykOptions>();
            allNonDefault.SetupGet(x => x.OssEnabled).Returns(false); // non-default
            allNonDefault.SetupGet(x => x.SnykCodeSecurityEnabled).Returns(false);
            allNonDefault.SetupGet(x => x.IacEnabled).Returns(false);
            allNonDefault.SetupGet(x => x.SecretsEnabled).Returns(true);
            allNonDefault.SetupGet(x => x.AutoScan).Returns(false);
            allNonDefault.SetupGet(x => x.EnableDeltaFindings).Returns(true);
            allNonDefault.SetupGet(x => x.FilterCritical).Returns(false);
            allNonDefault.SetupGet(x => x.FilterHigh).Returns(false);
            allNonDefault.SetupGet(x => x.FilterMedium).Returns(false);
            allNonDefault.SetupGet(x => x.FilterLow).Returns(false);
            allNonDefault.SetupGet(x => x.OpenIssuesEnabled).Returns(false);
            allNonDefault.SetupGet(x => x.IgnoredIssuesEnabled).Returns(true);
            allNonDefault.SetupGet(x => x.CustomEndpoint).Returns("https://custom.endpoint");
            allNonDefault.SetupGet(x => x.ApiToken).Returns(
                new AuthenticationToken(AuthenticationType.Pat, "tok"));
            allNonDefault.SetupGet(x => x.AuthenticationMethod).Returns(AuthenticationType.Pat);
            allNonDefault.SetupGet(x => x.Organization).Returns("my-org");
            allNonDefault.SetupGet(x => x.IgnoreUnknownCA).Returns(true);
            allNonDefault.SetupGet(x => x.BinariesAutoUpdate).Returns(false);
            allNonDefault.SetupGet(x => x.CliCustomPath).Returns("/custom/cli");
            allNonDefault.SetupGet(x => x.CliReleaseChannel).Returns("rc");
            allNonDefault.SetupGet(x => x.CliBaseDownloadURL).Returns("https://other.cdn");
            allNonDefault.SetupGet(x => x.AdditionalEnv).Returns("FOO=bar");
            allNonDefault.SetupGet(x => x.AdditionalParameters).Returns(new List<string> { "--flag" });
            allNonDefault.SetupGet(x => x.RiskScoreThreshold).Returns((int?)70);
            // TrustedFolders: non-empty so not equal to default empty-set.
            allNonDefault.SetupGet(x => x.TrustedFolders).Returns(
                new System.Collections.Generic.HashSet<string> { "/some/path" });

            trackerForKeys.SeedFrom(allNonDefault.Object);
            var trackedKeys = trackerForKeys.Snapshot();

            // Every key in BuildSettingsMap's Cs() set must be yielded by GetGlobalKeyValues.
            foreach (var key in buildSettingsMapTrackerKeys)
            {
                // AlwaysChanged keys are always IsChanged=true regardless of Snapshot,
                // so exclude them from the Snapshot check (they never appear in the changed set).
                if (PflagKeys.IsAlwaysChanged(key)) continue;
                Assert.Contains(key, trackedKeys); // GetGlobalKeyValues must yield this key
            }

            // Conversely, no key should appear in the tracker that isn't in the BuildSettingsMap set.
            foreach (var key in trackedKeys)
            {
                Assert.Contains(key, buildSettingsMapTrackerKeys); // key must be in BuildSettingsMap Cs() set
            }
        }

        // R3-1: ClearChanged() resets only the `changed` set, NOT the pendingResets queue.
        // A pending reset enqueued by ApplyUserEdits before Load() must survive the Load's ClearChanged call.
        [Fact]
        public void ClearChanged_PreservesPendingResets_ClearsOnlyChanged()
        {
            // Enqueue a pending reset: mark then Unmark directly (the same outcome as ApplyUserEdits
            // when the user resets a key to its default — Unmark is the primitive that enqueues).
            sut.Mark(PflagKeys.SnykOssEnabled);
            Assert.True(sut.IsChanged(PflagKeys.SnykOssEnabled));

            sut.Unmark(PflagKeys.SnykOssEnabled); // enqueues pending reset, removes from changed

            // Pending reset for SnykOssEnabled is queued; changed set has it removed.
            Assert.False(sut.IsChanged(PflagKeys.SnykOssEnabled));

            // Also mark a second key so ClearChanged has something to clear.
            sut.Mark(PflagKeys.SnykCodeEnabled);
            Assert.True(sut.IsChanged(PflagKeys.SnykCodeEnabled));

            // ClearChanged() — must clear changed but NOT pendingResets.
            sut.ClearChanged();

            // The stale mark for SnykCodeEnabled is gone.
            Assert.False(sut.IsChanged(PflagKeys.SnykCodeEnabled),
                "ClearChanged() must clear the changed set");

            // The pending reset for SnykOssEnabled is still consumable.
            var resets = sut.PeekPendingResets();
            Assert.Contains(PflagKeys.SnykOssEnabled, resets); // ClearChanged must NOT discard pending resets
        }

        // R3-2: Mark() for an AlwaysChanged key must be a no-op on the changed set.
        // IsChanged still returns true (via IsAlwaysChanged) but Snapshot() must be empty.
        [Fact]
        public void Mark_AlwaysChangedKey_NotAddedToChangedSet()
        {
            sut.Mark(PflagKeys.TrustedFolders);

            // Snapshot must not contain it (it's not in the changed set).
            Assert.Empty(sut.Snapshot());

            // IsChanged must still return true (via IsAlwaysChanged).
            Assert.True(sut.IsChanged(PflagKeys.TrustedFolders),
                "IsChanged must still be true for an always-changed key even when not in the changed set");
        }

        // R5-1a: IsSeeded is false on a fresh tracker instance.
        [Fact]
        public void IsSeeded_FreshTracker_IsFalse()
        {
            var fresh = new UserOverrideTracker();
            Assert.False(fresh.IsSeeded,
                "A freshly constructed tracker must not be seeded — it has no persisted data loaded");
        }

        // R5-1b: IsSeeded becomes true after SeedFrom is called (upgrade / first-load path).
        [Fact]
        public void IsSeeded_AfterSeedFrom_IsTrue()
        {
            var tracker = new UserOverrideTracker();
            Assert.False(tracker.IsSeeded);

            tracker.SeedFrom(DefaultOptions().Object);

            Assert.True(tracker.IsSeeded,
                "IsSeeded must be true after SeedFrom — tracker is now hydrated from persistence");
        }

        // R5-1c: ClearChanged does NOT reset IsSeeded (reload re-hydrates but tracker stays seeded).
        [Fact]
        public void IsSeeded_ClearChanged_DoesNotReset()
        {
            var tracker = new UserOverrideTracker();
            tracker.SeedFrom(DefaultOptions().Object);
            Assert.True(tracker.IsSeeded);

            tracker.ClearChanged();

            Assert.True(tracker.IsSeeded,
                "ClearChanged() must not reset IsSeeded — a reload re-hydrates without losing seeded status");
        }

        // R5-1d: Clear() does NOT reset IsSeeded either.
        [Fact]
        public void IsSeeded_Clear_DoesNotReset()
        {
            var tracker = new UserOverrideTracker();
            tracker.SeedFrom(DefaultOptions().Object);
            Assert.True(tracker.IsSeeded);

            tracker.Clear();

            Assert.True(tracker.IsSeeded,
                "Clear() must not reset IsSeeded — once seeded, the tracker is always considered seeded");
        }

        // R5-1e: MarkSeeded() sets IsSeeded to true (covers the Load() persisted-keys branch
        // which calls Mark in a loop then calls MarkSeeded() explicitly).
        [Fact]
        public void IsSeeded_AfterMarkSeeded_IsTrue()
        {
            var tracker = new UserOverrideTracker();
            Assert.False(tracker.IsSeeded);

            tracker.MarkSeeded();

            Assert.True(tracker.IsSeeded,
                "MarkSeeded() must set IsSeeded to true — it is called by Load() after the " +
                "persisted-keys Mark-loop to signal the tracker is hydrated");
        }

        // R4-1: Mark after Unmark (Mark→Unmark→Mark cycle) must cancel the pending reset.
        // Scenario: user sets OssEnabled=false (Mark), resets to default true (Unmark → pending reset),
        // then sets false again (Mark). The pending-reset queue must NOT contain OssEnabled
        // because the user re-applied the override — sending Reset() would silently drop their change.
        [Fact]
        public void Mark_AfterUnmark_CancelsPendingReset()
        {
            // Arrange: Mark → Unmark → Mark cycle.
            sut.Mark(PflagKeys.SnykOssEnabled);
            sut.Unmark(PflagKeys.SnykOssEnabled);   // enqueues pending reset
            sut.Mark(PflagKeys.SnykOssEnabled);      // user re-applies the override

            // Assert: pending reset must be cancelled (not in the pending-reset queue).
            var resets = sut.PeekPendingResets();
            Assert.DoesNotContain(PflagKeys.SnykOssEnabled, resets); // Mark after Unmark must cancel pending reset

            // Assert: IsChanged must be true because the key is back in the changed set.
            Assert.True(sut.IsChanged(PflagKeys.SnykOssEnabled),
                "IsChanged must be true after re-marking a key");
        }

        // RUNIT-001: ApplyUserEdits only marks / unmarks the explicitly edited keys.
        // Keys NOT in editedKeys must be completely untouched — whether previously marked or not.
        [Fact]
        public void ApplyUserEdits_OnlyEditedKeysAffected_UntouchedKeysUnchanged()
        {
            // Pre-condition: OssEnabled and IacEnabled are both marked (previously overridden).
            sut.Mark(PflagKeys.SnykOssEnabled);
            sut.Mark(PflagKeys.SnykIacEnabled);

            // Options: OssEnabled=false (non-default), IacEnabled=true (default).
            var options = DefaultOptions();
            options.SetupGet(x => x.OssEnabled).Returns(false);
            options.SetupGet(x => x.IacEnabled).Returns(true); // at default

            // editedKeys: only OssEnabled is in the edit delta (the user only touched OssEnabled).
            var editedKeys = new List<string> { PflagKeys.SnykOssEnabled };

            sut.ApplyUserEdits(options.Object, editedKeys);

            // OssEnabled was edited and its value (false) deviates from default (true) → must be marked.
            Assert.True(sut.IsChanged(PflagKeys.SnykOssEnabled),
                "An edited key with a non-default value must be marked");

            // IacEnabled was NOT in editedKeys → its prior mark must be preserved (not cleared).
            Assert.True(sut.IsChanged(PflagKeys.SnykIacEnabled),
                "A key NOT in editedKeys must not be touched — its prior mark must survive");

            // SnykCodeEnabled was not in editedKeys and was never marked → must remain unmarked.
            Assert.False(sut.IsChanged(PflagKeys.SnykCodeEnabled),
                "A key NOT in editedKeys and not previously marked must remain unmarked");
        }

        // RUNIT-002 (corrected semantics, PR #515): ApplyUserEdits with an edited key whose new
        // value equals the plugin default must MARK it (changed:true) and must NOT enqueue a reset.
        //
        // This is the core of the "enabling Snyk Code doesn't persist" fix: any key the form posted
        // (present in editedKeys) is an explicit user choice. Snyk Code's default is `true`, so a
        // user *enabling* it posts a value equal to the default — the OLD code inferred a reset from
        // value==default and let the org default win. Reset-to-default is no longer inferred here.
        [Fact]
        public void ApplyUserEdits_EditedKeyEqualToDefault_MarksChanged_NoReset()
        {
            // The user enables Snyk Code (posts true, which equals the plugin default true).
            var options = DefaultOptions();
            options.SetupGet(x => x.SnykCodeSecurityEnabled).Returns(true); // at default

            // The user's save included snyk_code_enabled in the edit delta.
            var editedKeys = new List<string> { PflagKeys.SnykCodeEnabled };

            sut.ApplyUserEdits(options.Object, editedKeys);

            // An edited key is an explicit user choice → must be marked, even at the default value.
            Assert.True(sut.IsChanged(PflagKeys.SnykCodeEnabled),
                "An edited key whose value equals the default is still an explicit user choice " +
                "and must be marked changed — reset is never inferred from value==default");

            // No reset must be enqueued via this path — value==default no longer implies a reset.
            var resets = sut.PeekPendingResets();
            Assert.DoesNotContain(PflagKeys.SnykCodeEnabled, resets); // no inferred reset from value==default
        }

        // PR-REV-001: Re-typing the same org-pushed value the user genuinely owns must still be
        // recorded as an override when the form sends the key.
        // OLD behaviour (snapshot/diff path): before==after equality caused the key to be DROPPED
        // from the edit-delta, so a user who re-typed a value that happened to equal the current
        // (org-pushed) Options value would lose their override mark.
        // NEW behaviour (form-driven): if the form sent the key (HasValue/!=null) it is in
        // editedKeys, so ApplyUserEdits sees it. Even when the current value is non-default,
        // the key is classified by IsDefault → Mark (not Unmark), and the override is preserved.
        [Fact]
        public void ApplyUserEdits_ReTypingSameOrgPushedValue_IsStillRecordedAsOverride()
        {
            // Simulate: org pushed OssEnabled=false (non-default). The tracker was NOT updated
            // (updateOverrideTracker:false on the LS-push path), so OssEnabled is NOT marked.
            var options = DefaultOptions();
            options.SetupGet(x => x.OssEnabled).Returns(false); // org-pushed non-default value

            // User re-types the same value (false) in the form — the form sends the key.
            // The form-driven approach records this as an edit because the form sent the key.
            sut.ApplyUserEdits(options.Object, new List<string> { PflagKeys.SnykOssEnabled });

            // The key must be marked as a user override (value is non-default).
            Assert.True(sut.IsChanged(PflagKeys.SnykOssEnabled),
                "Re-typing an org-pushed non-default value must be recorded as an override " +
                "because the form sent the key — the value is non-default so IsDefault→Mark");
        }

        // IDE-2152-UNIT-003a: Peek is non-destructive — the reset stays queued after a peek, so a
        // build-without-commit (e.g. a config send that fails) does not lose the reset intent.
        [Fact]
        public void PeekPendingResets_IsNonDestructive_QueueSurvives()
        {
            sut.ApplyUserResets(new List<string> { PflagKeys.SnykOssEnabled });

            var first = sut.PeekPendingResets();
            Assert.Contains(PflagKeys.SnykOssEnabled, first);

            // A second peek still sees it — nothing was consumed.
            var second = sut.PeekPendingResets();
            Assert.Contains(PflagKeys.SnykOssEnabled, second);
        }

        // IDE-2152-UNIT-003b: Commit clears only the named (delivered) keys, never the whole queue.
        [Fact]
        public void CommitPendingResets_ClearsOnlyNamedKeys()
        {
            sut.ApplyUserResets(new List<string> { PflagKeys.SnykOssEnabled, PflagKeys.SnykCodeEnabled });

            // Only OssEnabled was delivered.
            sut.CommitPendingResets(new List<string> { PflagKeys.SnykOssEnabled });

            var remaining = sut.PeekPendingResets();
            Assert.DoesNotContain(PflagKeys.SnykOssEnabled, remaining); // delivered → cleared
            Assert.Contains(PflagKeys.SnykCodeEnabled, remaining);      // not delivered → still queued
        }

        // IDE-2152-UNIT-003c: A reset for a DIFFERENT key enqueued between the peek (what the build
        // folded in and sent) and the commit must survive the commit — commit removes exactly the
        // sent set, never a blanket clear. This is the interleaving the re-delivery design must not
        // lose: the newer key was never in the confirmed message, so it must remain queued.
        [Fact]
        public void CommitPendingResets_InterleavedResetForDifferentKey_Survives()
        {
            sut.ApplyUserResets(new List<string> { PflagKeys.SnykOssEnabled });
            var peeked = sut.PeekPendingResets(); // what the build folded in and sent

            // Between the send and the commit, a reset for a different key is enqueued.
            sut.ApplyUserResets(new List<string> { PflagKeys.SnykCodeEnabled });

            // The send is confirmed only for the originally peeked keys; commit removes exactly those.
            sut.CommitPendingResets(peeked);

            var remaining = sut.PeekPendingResets();
            Assert.DoesNotContain(PflagKeys.SnykOssEnabled, remaining); // delivered → cleared
            Assert.Contains(PflagKeys.SnykCodeEnabled, remaining);      // enqueued after send → survives
        }

        // IDE-2152-UNIT-003d: CommitPendingResets is a safe no-op on null/empty.
        [Fact]
        public void CommitPendingResets_NullOrEmpty_IsNoOp()
        {
            sut.ApplyUserResets(new List<string> { PflagKeys.SnykOssEnabled });

            sut.CommitPendingResets(null);
            sut.CommitPendingResets(new List<string>());

            Assert.Contains(PflagKeys.SnykOssEnabled, sut.PeekPendingResets());
        }

        // IDE-2152-UNIT-002a: ApplyUserResets un-marks each reset key (drops out of Snapshot) and
        // enqueues a pending reset that BuildSettingsMap will consume as {value:null, changed:true}.
        [Fact]
        public void ApplyUserResets_UnmarksKey_AndEnqueuesPendingReset()
        {
            // The key was previously a user override.
            sut.Mark(PflagKeys.SnykOssEnabled);
            Assert.Contains(PflagKeys.SnykOssEnabled, sut.Snapshot());

            sut.ApplyUserResets(new List<string> { PflagKeys.SnykOssEnabled });

            // No longer a persisted override.
            Assert.DoesNotContain(PflagKeys.SnykOssEnabled, sut.Snapshot());
            Assert.False(sut.IsChanged(PflagKeys.SnykOssEnabled));

            // Reset signal is queued.
            var resets = sut.PeekPendingResets();
            Assert.Contains(PflagKeys.SnykOssEnabled, resets);
        }

        // IDE-2152-UNIT-002b: A form reset ALWAYS enqueues the LS reset signal — even when the plugin
        // had no local mark for the key (e.g. an org-pushed value the user wants cleared). Local
        // un-mark is best-effort; the LS still needs {value:null, changed:true} to Unset user:global.
        [Fact]
        public void ApplyUserResets_KeyNeverMarked_StillEnqueuesResetSignal()
        {
            Assert.DoesNotContain(PflagKeys.Organization, sut.Snapshot()); // precondition: no mark

            sut.ApplyUserResets(new List<string> { PflagKeys.Organization });

            var resets = sut.PeekPendingResets();
            Assert.Contains(PflagKeys.Organization, resets);
        }

        // IDE-2152-UNIT-002c: Re-enabling a key (Mark) after a reset was queued cancels that reset —
        // the user re-applied an override, so the next build must not clobber it with a reset signal.
        [Fact]
        public void ApplyUserResets_ThenMark_CancelsQueuedReset()
        {
            sut.ApplyUserResets(new List<string> { PflagKeys.SnykOssEnabled }); // reset queued
            sut.Mark(PflagKeys.SnykOssEnabled); // user re-applies the override

            var resets = sut.PeekPendingResets();
            Assert.DoesNotContain(PflagKeys.SnykOssEnabled, resets);
            Assert.True(sut.IsChanged(PflagKeys.SnykOssEnabled));
        }

        // IDE-2152-UNIT-002d: ApplyUserResets with a null/empty set is a harmless no-op.
        [Fact]
        public void ApplyUserResets_NullOrEmpty_IsNoOp()
        {
            sut.Mark(PflagKeys.SnykOssEnabled);

            sut.ApplyUserResets(null);
            sut.ApplyUserResets(new List<string>());

            Assert.Contains(PflagKeys.SnykOssEnabled, sut.Snapshot());
            Assert.Empty(sut.PeekPendingResets());
        }

        // IDE-2152-UNIT-P1 (fix #2): RehydratePendingResets re-queues persisted-but-unconfirmed resets
        // on Load without un-marking anything (Load hydrates `changed` separately). The rehydrated key
        // must appear in the pending-reset queue for re-delivery.
        [Fact]
        public void RehydratePendingResets_ReQueuesPersistedReset()
        {
            sut.RehydratePendingResets(new List<string> { PflagKeys.SnykOssEnabled });

            Assert.Contains(PflagKeys.SnykOssEnabled, sut.PeekPendingResets());
        }

        // IDE-2152-UNIT-P2 (fix #2): RehydratePendingResets must NOT re-queue a reset for a key that is
        // a live override mark — a persisted override means the user re-applied the key after the reset
        // was queued, so the override wins (invariant: never in both `changed` and pendingResets).
        [Fact]
        public void RehydratePendingResets_SkipsKeyThatIsLiveOverride()
        {
            sut.Mark(PflagKeys.SnykOssEnabled); // live override

            sut.RehydratePendingResets(new List<string> { PflagKeys.SnykOssEnabled });

            Assert.DoesNotContain(PflagKeys.SnykOssEnabled, sut.PeekPendingResets());
            Assert.True(sut.IsChanged(PflagKeys.SnykOssEnabled));
        }

        // IDE-2152-UNIT-P3 (fix #2): RehydratePendingResets with a null/empty set is a harmless no-op.
        [Fact]
        public void RehydratePendingResets_NullOrEmpty_IsNoOp()
        {
            sut.RehydratePendingResets(null);
            sut.RehydratePendingResets(new List<string>());

            Assert.Empty(sut.PeekPendingResets());
        }

        // PR-REV-002: ApplyUserEdits with AdditionalParameters key (List<string> in options,
        // but the tracker compares the space-joined string representation) must record an override
        // when the user types any non-empty value. Previously the snapshot/diff path used
        // ToString() on a List<string> ("System.Collections.Generic.List`1[System.String]") and
        // compared it to the string "System.Collections.Generic.List`1[System.String]" — which
        // always compared as equal, so editing AdditionalParameters never registered.
        [Fact]
        public void ApplyUserEdits_AdditionalParameters_RecordsOverrideWhenNonEmpty()
        {
            // Options: AdditionalParameters has a non-default value (non-empty list → space-joined non-empty string).
            var options = DefaultOptions();
            options.SetupGet(x => x.AdditionalParameters).Returns(new List<string> { "--debug" });

            // The form sent additional_parameters.
            sut.ApplyUserEdits(options.Object, new List<string> { PflagKeys.AdditionalParameters });

            // Must be marked as a user override.
            Assert.True(sut.IsChanged(PflagKeys.AdditionalParameters),
                "AdditionalParameters with a non-empty list must be marked as an override — " +
                "the tracker normalises it to a space-joined string before comparing to default");
        }

        // ─────────────────────────────────────────────────────────────────────
        // THREAD-SAFETY tests (IDE-2152 critical fix #1)
        // The tracker's `changed` / `pendingResets` are mutated from the UI thread (Save →
        // ApplyUserEdits/ApplyUserResets) AND from thread-pool continuations (the config-send
        // path commits after `await ...ConfigureAwait(false)`), from multiple fire-and-forget
        // call sites. Every read and mutation must be guarded by a single lock, and Peek/Snapshot
        // must return an independent copy taken under the lock.
        // ─────────────────────────────────────────────────────────────────────

        // TS-001: Snapshot returns an independent copy — mutating the tracker after taking a
        // snapshot must not change the already-returned snapshot, and vice versa.
        [Fact]
        public void Snapshot_ReturnsIndependentCopy()
        {
            sut.Mark(PflagKeys.SnykOssEnabled);

            var snap = sut.Snapshot();
            Assert.Contains(PflagKeys.SnykOssEnabled, snap);

            // Mutate the tracker after the snapshot was taken.
            sut.Mark(PflagKeys.SnykCodeEnabled);
            // The previously-returned snapshot must be unaffected.
            Assert.DoesNotContain(PflagKeys.SnykCodeEnabled, snap);

            // Mutating the returned copy must not affect the tracker.
            snap.Add(PflagKeys.SnykIacEnabled);
            Assert.False(sut.IsChanged(PflagKeys.SnykIacEnabled),
                "Mutating the returned Snapshot copy must not leak back into the tracker");
        }

        // TS-002: PeekPendingResets returns an independent copy — enqueuing a further reset after a
        // peek must not change the already-returned peek result.
        [Fact]
        public void PeekPendingResets_ReturnsIndependentCopy()
        {
            sut.ApplyUserResets(new List<string> { PflagKeys.SnykOssEnabled });

            var peek = sut.PeekPendingResets();
            Assert.Contains(PflagKeys.SnykOssEnabled, peek);

            sut.ApplyUserResets(new List<string> { PflagKeys.SnykCodeEnabled });
            Assert.DoesNotContain(PflagKeys.SnykCodeEnabled, peek);
        }

        // TS-003: Concurrent access from many threads must not throw or corrupt state, and the core
        // invariant — a key is never simultaneously in both `changed` and `pendingResets` — must
        // hold after the storm. With plain (unsynchronised) HashSets this test throws
        // InvalidOperationException ("collection was modified") or corrupts the sets; the single
        // lock makes every method atomic so it passes.
        [Fact]
        public void ConcurrentAccess_DoesNotThrowOrCorruptState()
        {
            var keys = new[]
            {
                PflagKeys.SnykOssEnabled, PflagKeys.SnykCodeEnabled, PflagKeys.SnykIacEnabled,
                PflagKeys.SnykSecretsEnabled, PflagKeys.ScanAutomatic, PflagKeys.ScanNetNew,
                PflagKeys.SeverityFilterCritical, PflagKeys.SeverityFilterHigh,
                PflagKeys.SeverityFilterMedium, PflagKeys.SeverityFilterLow,
            };

            const int threads = 8;
            const int iterations = 2000;
            var barrier = new Barrier(threads);
            var exceptions = new System.Collections.Concurrent.ConcurrentQueue<System.Exception>();

            var tasks = Enumerable.Range(0, threads).Select(t => Task.Run(() =>
            {
                barrier.SignalAndWait();
                try
                {
                    for (var i = 0; i < iterations; i++)
                    {
                        var key = keys[(t + i) % keys.Length];
                        switch (i % 6)
                        {
                            case 0: sut.Mark(key); break;
                            case 1: sut.ApplyUserResets(new List<string> { key }); break;
                            case 2: sut.CommitPendingResets(sut.PeekPendingResets()); break;
                            case 3: _ = sut.Snapshot(); break;
                            case 4: _ = sut.IsChanged(key); break;
                            case 5: sut.ApplyUserEdits(null, new List<string> { key }); break;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    exceptions.Enqueue(ex);
                }
            })).ToArray();

            Task.WaitAll(tasks);

            Assert.True(exceptions.IsEmpty,
                "Concurrent tracker access must not throw — the internal sets must be lock-guarded. " +
                "First exception: " + (exceptions.TryPeek(out var first) ? first.ToString() : "none"));

            // Invariant: no key is in both the changed set and the pending-reset queue.
            var changed = sut.Snapshot();
            var resets = sut.PeekPendingResets();
            Assert.Empty(changed.Intersect(resets));
        }
    }
}
