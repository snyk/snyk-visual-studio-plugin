// ABOUTME: Acceptance + integration tests for global reset-to-default propagation (IDE-2152 CP 2.1).
// ABOUTME: Drives the real HtmlSettingsScriptingBridge → real SnykOptionsManager → real UserOverrideTracker
// ABOUTME: → real LsSettingsV25, so the reset flows through the production composition, not a fake at the seam.
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Newtonsoft.Json;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Settings
{
    [Collection(MockedVS.Collection)]
    public class GlobalResetPropagationTests
    {
        public GlobalResetPropagationTests(GlobalServiceProvider sp)
        {
            sp.Reset();
        }

        // Wire a REAL SnykOptionsManager (temp-file backed) + real UserOverrideTracker into a real
        // service provider, plus a real bridge and a real LsSettingsV25 reading the same manager.
        // This is the production composition: the reset must survive every hand-off.
        private sealed class Harness
        {
            public HtmlSettingsScriptingBridge Bridge;
            public SnykOptionsManager Manager;
            public LsSettingsV25 LsSettings;
            public Mock<ISnykOptions> Options;
            public string Path;
        }

        private static Harness BuildHarness()
        {
            var path = System.IO.Path.GetTempFileName();

            var optMock = new Mock<ISnykOptions>();
            optMock.SetupAllProperties();
            // Plugin defaults so an empty save is a no-op and Save() does not null-ref.
            optMock.Object.OssEnabled = true;
            optMock.Object.SnykCodeSecurityEnabled = true;
            optMock.Object.IacEnabled = true;
            optMock.Object.SecretsEnabled = false;
            optMock.Object.AutoScan = true;
            optMock.Object.EnableDeltaFindings = false;
            optMock.Object.FilterCritical = true;
            optMock.Object.FilterHigh = true;
            optMock.Object.FilterMedium = true;
            optMock.Object.FilterLow = true;
            optMock.Object.OpenIssuesEnabled = true;
            optMock.Object.IgnoredIssuesEnabled = false;
            optMock.Object.IgnoreUnknownCA = false;
            optMock.Object.BinariesAutoUpdate = true;
            optMock.Object.CliCustomPath = string.Empty;
            optMock.Object.CliReleaseChannel = SnykCliDownloader.DefaultReleaseChannel;
            optMock.Object.CliBaseDownloadURL = SnykCliDownloader.DefaultBaseDownloadUrl;
            optMock.Object.AdditionalEnv = string.Empty;
            optMock.Object.AdditionalParameters = new List<string>();
            optMock.Object.RiskScoreThreshold = null;
            optMock.Object.TrustedFolders = new HashSet<string>();
            optMock.Object.DeviceId = "test-device";
            optMock.Object.Organization = "acme-user-org"; // a user override to be reset
            optMock.Object.IntegrationEnvironment = "Visual Studio 2022";
            optMock.Object.IntegrationName = "VISUAL_STUDIO";
            optMock.Object.IntegrationEnvironmentVersion = "2022";
            optMock.Object.IntegrationVersion = "1.0.0";
            optMock.Object.ApiToken = new AuthenticationToken(AuthenticationType.OAuth, string.Empty);
            optMock.Object.AuthenticationMethod = AuthenticationType.OAuth;
            optMock.Object.FolderConfigs = new List<FolderConfig>();

            var spMock = new Mock<ISnykServiceProvider>();
            spMock.Setup(x => x.Options).Returns(optMock.Object);
            spMock.Setup(x => x.LanguageClientManager).Returns((ILanguageClientManager)null);

            var manager = new SnykOptionsManager(path, spMock.Object);
            // Wire the real manager into the provider so both the bridge and LsSettingsV25 use the
            // same tracker instance (the production composition root behaviour).
            spMock.Setup(x => x.SnykOptionsManager).Returns(manager);

            var bridge = new HtmlSettingsScriptingBridge(spMock.Object, onModified: () => { });
            var lsSettings = new LsSettingsV25(spMock.Object);

            return new Harness
            {
                Bridge = bridge,
                Manager = manager,
                LsSettings = lsSettings,
                Options = optMock,
                Path = path,
            };
        }

        // IDE-2152-ACCEPT-001: When a user resets global settings to default (a form save payload with
        // those keys posted as explicit JSON null), the settings the plugin sends to the LS clear those
        // keys as {value:null, changed:true} so the org/LDX-sync default takes effect again.
        // Covers a bool key (snyk_code_enabled), a string key (organization) and the unset int key
        // (risk_score_threshold, which the value path never emits).
        [Fact]
        public void ResetPayload_ClearsKeysInLsBoundSettingsMap()
        {
            var h = BuildHarness();
            try
            {
                h.Manager.Load(); // seed the real tracker

                // The "Reset overrides" form save: the reset-eligible keys are posted as explicit null.
                var payload = "{" +
                    "\"snyk_code_enabled\":null," +
                    "\"organization\":null," +
                    "\"risk_score_threshold\":null" +
                    "}";

                h.Bridge.__saveIdeConfig__(payload);
                Assert.True(h.Bridge.SaveCompletion.IsCompleted);
                Assert.True(h.Bridge.SaveCompletion.Result, "the reset save must succeed");

                var map = h.LsSettings.BuildSettingsMap(h.Options.Object);

                AssertReset(map, PflagKeys.SnykCodeEnabled);
                AssertReset(map, PflagKeys.Organization);
                AssertReset(map, PflagKeys.RiskScoreThreshold);
            }
            finally
            {
                File.Delete(h.Path);
            }
        }

        // IDE-2152-INTEG-001: A reset save un-marks the key so it no longer appears in the persisted
        // override set and stays cleared across a reload (fresh manager over the same file).
        [Fact]
        public void ResetSave_UnmarksKey_StaysClearedAcrossReload()
        {
            var h = BuildHarness();
            try
            {
                h.Manager.Load();

                // First: the user overrides snyk_code_enabled (edit to non-default false).
                h.Options.Object.SnykCodeSecurityEnabled = false;
                h.Manager.Save(h.Options.Object, triggerSettingsChangedEvent: false,
                    editedKeys: new List<string> { PflagKeys.SnykCodeEnabled });
                Assert.Contains(PflagKeys.SnykCodeEnabled, h.Manager.OverrideTracker.Snapshot());

                // Then: the user resets it. Reset payload posts snyk_code_enabled as JSON null.
                h.Bridge.__saveIdeConfig__("{\"snyk_code_enabled\":null}");
                Assert.True(h.Bridge.SaveCompletion.Result);

                // The key must be gone from the tracker's persisted set.
                Assert.DoesNotContain(PflagKeys.SnykCodeEnabled, h.Manager.OverrideTracker.Snapshot());

                // And it must stay cleared across a restart (fresh manager over the same file).
                var spMock2 = new Mock<ISnykServiceProvider>();
                spMock2.Setup(x => x.Options).Returns(h.Options.Object);
                var manager2 = new SnykOptionsManager(h.Path, spMock2.Object);
                spMock2.Setup(x => x.SnykOptionsManager).Returns(manager2);
                var loaded = manager2.Load();

                Assert.DoesNotContain(PflagKeys.SnykCodeEnabled,
                    loaded.ChangedConfigKeys ?? new HashSet<string>());
            }
            finally
            {
                File.Delete(h.Path);
            }
        }

        // IDE-2152-INTEG-002: A reset for organization (string) and risk_score_threshold (unset int)
        // both reach the LS as reset signals through the production bridge → manager → LsSettings path.
        [Fact]
        public void Reset_OrganizationAndRiskScoreThreshold_BothReachLs()
        {
            var h = BuildHarness();
            try
            {
                h.Manager.Load();

                h.Bridge.__saveIdeConfig__("{\"organization\":null,\"risk_score_threshold\":null}");
                Assert.True(h.Bridge.SaveCompletion.Result);

                var map = h.LsSettings.BuildSettingsMap(h.Options.Object);

                AssertReset(map, PflagKeys.Organization);
                AssertReset(map, PflagKeys.RiskScoreThreshold);
            }
            finally
            {
                File.Delete(h.Path);
            }
        }

        // IDE-2152 critical fix #2: A reset applied while the LS was not ready (so the reset signal
        // was never confirmed-delivered) must SURVIVE a restart and be re-emitted by BuildSettingsMap
        // after a Save→Load round-trip through a fresh manager over the same settings file. Before the
        // fix, the reset lived only in the in-memory pendingResets queue, so it was lost on restart and
        // the LS override was never cleared.
        [Fact]
        public void PendingReset_SurvivesRestart_ReEmittedByBuildSettingsMapAfterReload()
        {
            var h = BuildHarness();
            try
            {
                h.Manager.Load(); // seed the real tracker

                // The user resets a global setting. The reset is queued but (simulating "LS not ready")
                // it is never committed via a confirmed config send — it stays pending.
                h.Bridge.__saveIdeConfig__("{\"snyk_code_enabled\":null,\"organization\":null}");
                Assert.True(h.Bridge.SaveCompletion.Result, "the reset save must succeed");
                // Precondition: the reset is queued in the (unconfirmed) pending set.
                Assert.Contains(PflagKeys.SnykCodeEnabled, h.Manager.OverrideTracker.PeekPendingResets());

                // Simulate an IDE restart: a FRESH manager + FRESH LsSettings over the SAME file.
                var spMock2 = new Mock<ISnykServiceProvider>();
                spMock2.Setup(x => x.Options).Returns(h.Options.Object);
                var manager2 = new SnykOptionsManager(h.Path, spMock2.Object);
                spMock2.Setup(x => x.SnykOptionsManager).Returns(manager2);
                var lsSettings2 = new LsSettingsV25(spMock2.Object);

                manager2.Load(); // rehydrates the persisted pending resets

                // The un-confirmed reset must have survived the restart and be re-emitted to the LS.
                var map = lsSettings2.BuildSettingsMap(h.Options.Object);
                AssertReset(map, PflagKeys.SnykCodeEnabled);
                AssertReset(map, PflagKeys.Organization);
            }
            finally
            {
                File.Delete(h.Path);
            }
        }

        // IDE-2152 critical fix #2: A reset that WAS confirmed-delivered (committed via the manager's
        // CommitPendingResets) must be removed from persistence too, so it is NOT re-sent after a
        // restart. Otherwise every delivered reset would be re-delivered forever.
        [Fact]
        public void CommittedReset_RemovedFromPersistence_NotReEmittedAfterRestart()
        {
            var h = BuildHarness();
            try
            {
                h.Manager.Load();

                h.Bridge.__saveIdeConfig__("{\"snyk_code_enabled\":null}");
                Assert.True(h.Bridge.SaveCompletion.Result);
                Assert.Contains(PflagKeys.SnykCodeEnabled, h.Manager.OverrideTracker.PeekPendingResets());

                // The config send is confirmed successful → commit through the manager (which must also
                // update persistence, not just the in-memory queue).
                h.Manager.CommitPendingResets(new List<string> { PflagKeys.SnykCodeEnabled });
                Assert.DoesNotContain(PflagKeys.SnykCodeEnabled, h.Manager.OverrideTracker.PeekPendingResets());

                // Restart: fresh manager + LsSettings over the same file.
                var spMock2 = new Mock<ISnykServiceProvider>();
                spMock2.Setup(x => x.Options).Returns(h.Options.Object);
                var manager2 = new SnykOptionsManager(h.Path, spMock2.Object);
                spMock2.Setup(x => x.SnykOptionsManager).Returns(manager2);
                var lsSettings2 = new LsSettingsV25(spMock2.Object);
                manager2.Load();

                // The committed reset must NOT come back as a reset signal — its real value is sent.
                var map = lsSettings2.BuildSettingsMap(h.Options.Object);
                Assert.NotNull(map[PflagKeys.SnykCodeEnabled].Value); // not a reset (value:null)
                Assert.Empty(manager2.OverrideTracker.PeekPendingResets());
            }
            finally
            {
                File.Delete(h.Path);
            }
        }

        private static void AssertReset(IDictionary<string, ConfigSetting> map, string key)
        {
            Assert.True(map.ContainsKey(key), $"{key} must be present as a reset signal");
            Assert.Null(map[key].Value);
            Assert.True(map[key].Changed, $"{key} reset must be changed:true");
        }
    }
}
