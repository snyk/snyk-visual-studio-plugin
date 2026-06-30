// ABOUTME: Unit tests for UserOverrideTracker covering UNIT-001..010 from the IDE-2152 test plan.
using System.Collections.Generic;
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

        // UNIT-004: Unmark enqueues a pending reset; ConsumePendingResets returns it once, then empty.
        [Fact]
        public void Unmark_EnqueuesPendingReset_ConsumedOnce()
        {
            sut.Mark(PflagKeys.SnykOssEnabled);
            sut.Unmark(PflagKeys.SnykOssEnabled);

            var first = sut.ConsumePendingResets();
            Assert.Contains(PflagKeys.SnykOssEnabled, first);

            var second = sut.ConsumePendingResets();
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

            Assert.Empty(sut.Snapshot(),
                "SeedFrom with all-default options must clear prior marks — replace, not accumulate");
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
            var resets = sut.ConsumePendingResets();
            Assert.DoesNotContain(PflagKeys.SnykOssEnabled, resets,
                "SeedFrom must clear pendingResets (uses Clear(), not ClearChanged()). " +
                "A stale pending reset before SeedFrom must NOT survive into BuildSettingsMap.");
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

        // UNIT-006: ApplyUserEdits unmarks a key when the user resets it to its default,
        // and enqueues a pending reset for it so BuildSettingsMap can emit {value:null, changed:true}.
        [Fact]
        public void ApplyUserEdits_ReturnToDefault_UnmarksAndEnqueuesReset()
        {
            // Prime the tracker: OssEnabled was previously overridden.
            sut.Mark(PflagKeys.SnykOssEnabled);
            Assert.True(sut.IsChanged(PflagKeys.SnykOssEnabled));

            // User resets OssEnabled to the default (true). Provide it as an edited key.
            var options = DefaultOptions();
            options.SetupGet(o => o.OssEnabled).Returns(true); // back to default

            sut.ApplyUserEdits(options.Object, new List<string> { PflagKeys.SnykOssEnabled });

            Assert.False(sut.IsChanged(PflagKeys.SnykOssEnabled), "Key returned to default should be unmarked");
            var pending = sut.ConsumePendingResets();
            Assert.Contains(PflagKeys.SnykOssEnabled, pending);
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

        // UNIT-010 (finding 3): ApplyUserEdits with null RiskScoreThreshold after it was marked
        // should unmark it AND enqueue a pending reset so the LS can clear the value.
        [Fact]
        public void ApplyUserEdits_RiskScoreThresholdClearedToNull_UnmarksAndEnqueuesReset()
        {
            // Seed: threshold was set to 70 (non-default).
            sut.Mark(PflagKeys.RiskScoreThreshold);
            Assert.True(sut.IsChanged(PflagKeys.RiskScoreThreshold));

            // User cleared the threshold (returned to default null).
            var options = DefaultOptions();
            options.SetupGet(x => x.RiskScoreThreshold).Returns((int?)null);

            sut.ApplyUserEdits(options.Object, new List<string> { PflagKeys.RiskScoreThreshold });

            Assert.False(sut.IsChanged(PflagKeys.RiskScoreThreshold),
                "Cleared RiskScoreThreshold should be unmarked");
            var pending = sut.ConsumePendingResets();
            Assert.Contains(PflagKeys.RiskScoreThreshold, pending);
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
                new Authentication.AuthenticationToken(Authentication.AuthenticationType.Pat, "tok"));
            allNonDefault.SetupGet(x => x.AuthenticationMethod).Returns(Authentication.AuthenticationType.Pat);
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
                Assert.Contains(key, trackedKeys,
                    $"GetGlobalKeyValues must yield '{key}' — it is tracked by BuildSettingsMap via Cs() " +
                    "but was not found in the tracker snapshot after SeedFrom with all-non-default options");
            }

            // Conversely, no key should appear in the tracker that isn't in the BuildSettingsMap set.
            foreach (var key in trackedKeys)
            {
                Assert.Contains(key, buildSettingsMapTrackerKeys,
                    $"GetGlobalKeyValues yielded '{key}' which is NOT in BuildSettingsMap's Cs() key set — " +
                    "remove it from GetGlobalKeyValues or add it to BuildSettingsMap");
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
            var resets = sut.ConsumePendingResets();
            Assert.Contains(PflagKeys.SnykOssEnabled, resets,
                "ClearChanged() must NOT discard pending resets — they are still needed by BuildSettingsMap");
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
        // then sets false again (Mark). The next ConsumePendingResets() must NOT contain OssEnabled
        // because the user re-applied the override — sending Reset() would silently drop their change.
        [Fact]
        public void Mark_AfterUnmark_CancelsPendingReset()
        {
            // Arrange: Mark → Unmark → Mark cycle.
            sut.Mark(PflagKeys.SnykOssEnabled);
            sut.Unmark(PflagKeys.SnykOssEnabled);   // enqueues pending reset
            sut.Mark(PflagKeys.SnykOssEnabled);      // user re-applies the override

            // Assert: pending reset must be cancelled (not in ConsumePendingResets output).
            var resets = sut.ConsumePendingResets();
            Assert.DoesNotContain(PflagKeys.SnykOssEnabled, resets,
                "Mark() after Unmark() must cancel the pending reset — " +
                "otherwise BuildSettingsMap overwrites the active override with a Reset() signal");

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

        // RUNIT-002: ApplyUserEdits with an edited key whose new value equals the plugin default
        // must unmark it and enqueue a pending reset — not mark it.
        [Fact]
        public void ApplyUserEdits_EditedKeyEqualToDefault_UnmarksAndEnqueuesReset()
        {
            // Pre-condition: OssEnabled is marked (user previously set it to non-default).
            sut.Mark(PflagKeys.SnykOssEnabled);
            Assert.True(sut.IsChanged(PflagKeys.SnykOssEnabled));

            // Options: OssEnabled=true (its plugin default), meaning the user just reset it.
            var options = DefaultOptions();
            options.SetupGet(x => x.OssEnabled).Returns(true); // at default

            // The user's save included OssEnabled in the edit delta.
            var editedKeys = new List<string> { PflagKeys.SnykOssEnabled };

            sut.ApplyUserEdits(options.Object, editedKeys);

            // Must be unmarked.
            Assert.False(sut.IsChanged(PflagKeys.SnykOssEnabled),
                "An edited key whose new value equals the default must be unmarked");

            // Must have a pending reset so BuildSettingsMap can emit {value:null, changed:true}.
            var resets = sut.ConsumePendingResets();
            Assert.Contains(PflagKeys.SnykOssEnabled, resets,
                "An edited key reset to default must produce a pending reset signal");
        }
    }
}
