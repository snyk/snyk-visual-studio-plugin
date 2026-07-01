using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    [Collection(MockedVS.Collection)]
    public class LsSettingsV25Tests
    {
        private LsSettingsV25 cut;
        private readonly Mock<ISnykOptions> optionsMock;
        private readonly Mock<ISnykOptionsManager> optionsManagerMock;

        public LsSettingsV25Tests(GlobalServiceProvider sp)
        {
            sp.Reset();
            optionsMock = new Mock<ISnykOptions>();
            optionsManagerMock = new Mock<ISnykOptionsManager>();
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            serviceProviderMock.Setup(x => x.Options).Returns(optionsMock.Object);
            serviceProviderMock.Setup(x => x.SnykOptionsManager).Returns(optionsManagerMock.Object);
            cut = new LsSettingsV25(serviceProviderMock.Object);
        }

        // Builds a minimal IPersistableOptions at all-plugin-defaults for use with SeedFrom.
        // Used by tests that need a seeded real UserOverrideTracker with no keys marked changed.
        private static ISnykOptions BuildDefaultOptionsForSeed()
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
            o.SetupGet(x => x.ApiToken).Returns(
                new AuthenticationToken(AuthenticationType.OAuth, string.Empty));
            o.SetupGet(x => x.AuthenticationMethod).Returns(default(AuthenticationType));
            return o.Object;
        }

        private void SetupDefaults()
        {
            TestUtils.SetupOptionsMock(optionsMock);
            optionsMock.SetupGet(o => o.SecretsEnabled).Returns(false);
            optionsMock.SetupGet(o => o.FilterCritical).Returns(true);
            optionsMock.SetupGet(o => o.FilterHigh).Returns(true);
            optionsMock.SetupGet(o => o.FilterMedium).Returns(false);
            optionsMock.SetupGet(o => o.FilterLow).Returns(false);
            optionsMock.SetupGet(o => o.OpenIssuesEnabled).Returns(true);
            optionsMock.SetupGet(o => o.IgnoredIssuesEnabled).Returns(false);
            optionsMock.SetupGet(o => o.CliReleaseChannel).Returns(SnykCliDownloader.DefaultReleaseChannel);
            optionsMock.SetupGet(o => o.CliBaseDownloadURL).Returns(SnykCliDownloader.DefaultBaseDownloadUrl);
            optionsMock.SetupGet(o => o.AdditionalEnv).Returns(string.Empty);
            optionsMock.SetupGet(o => o.AdditionalParameters).Returns(new List<string>());
            optionsMock.SetupGet(o => o.RiskScoreThreshold).Returns((int?)null);
            optionsMock.SetupGet(o => o.InternalAutoScan).Returns(false);
        }

        [Fact]
        public void FolderConfigOverrides_RoundTripThroughBuildAndApply()
        {
            // Guards the outbound/inbound symmetry: BuildFolderConfigs forwards the per-folder
            // settings map and FolderConfigApplier.ToFolderConfig must round-trip every key back,
            // or a $/snyk.configuration echo silently drops the user's per-folder override. With the
            // opaque-map model this is verbatim, including reference_folder and local_branches — the
            // two keys the old cherry-pick model dropped.
            SetupDefaults();
            var original = new FolderConfig { FolderPath = "/repo" };
            original.Set(PflagKeys.SnykOssEnabled, true);
            original.Set(PflagKeys.SnykCodeEnabled, false);
            original.Set(PflagKeys.SnykIacEnabled, true);
            original.Set(PflagKeys.ScanAutomatic, true);
            original.Set(PflagKeys.SeverityFilterCritical, true);
            original.Set(PflagKeys.IssueViewOpenIssues, true);
            original.Set(PflagKeys.RiskScoreThreshold, 750);
            original.SetString(PflagKeys.BaseBranch, "main");
            original.SetString(PflagKeys.ReferenceFolder, @"C:\refs\main");
            original.Set(PflagKeys.LocalBranches, new List<string> { "main", "dev" });
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { original });

            // POCO -> BuildFolderConfigs (outbound) -> LspFolderConfig -> ToFolderConfig (inbound) -> POCO
            var lspFolderConfig = cut.GetInitializationOptions().FolderConfigs[0];
            var roundTripped = FolderConfigApplier.ToFolderConfig(lspFolderConfig);

            Assert.Equal(true, roundTripped.Settings[PflagKeys.SnykOssEnabled].Value);
            Assert.Equal(false, roundTripped.Settings[PflagKeys.SnykCodeEnabled].Value);
            Assert.Equal(true, roundTripped.Settings[PflagKeys.SnykIacEnabled].Value);
            Assert.Equal(true, roundTripped.Settings[PflagKeys.ScanAutomatic].Value);
            Assert.Equal(true, roundTripped.Settings[PflagKeys.SeverityFilterCritical].Value);
            Assert.Equal(true, roundTripped.Settings[PflagKeys.IssueViewOpenIssues].Value);
            Assert.Equal(750, roundTripped.Settings[PflagKeys.RiskScoreThreshold].Value);
            Assert.Equal("main", roundTripped.GetString(PflagKeys.BaseBranch));
            // The original gap: these two now round-trip instead of being dropped.
            Assert.Equal(@"C:\refs\main", roundTripped.GetString(PflagKeys.ReferenceFolder));
            Assert.Equal(new List<string> { "main", "dev" }, roundTripped.GetStringList(PflagKeys.LocalBranches));
        }

        [Fact]
        public void BuildFolderConfigs_RoundTripsNullValuedResetSettingsVerbatim()
        {
            // A reset is stored as a null-valued ConfigSetting directly in the opaque map (the bridge
            // Sets the key to null for a present-null form field). BuildFolderConfigs round-trips the
            // map verbatim, so each must go out as {value:null, changed:true} for snyk-ls to Unset the
            // user:folder: override.
            SetupDefaults();
            var fc = new FolderConfig { FolderPath = "/repo" };
            foreach (var key in new[]
            {
                PflagKeys.SnykCodeEnabled, PflagKeys.PreferredOrg, PflagKeys.RiskScoreThreshold,
                PflagKeys.AdditionalParameters, PflagKeys.AdditionalEnvironment, PflagKeys.ScanCommandConfig,
            })
            {
                fc.Set(key, null);
            }
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { fc });

            var settings = cut.GetInitializationOptions().FolderConfigs[0].Settings;

            AssertResetSetting(settings, PflagKeys.SnykCodeEnabled);
            AssertResetSetting(settings, PflagKeys.PreferredOrg);
            AssertResetSetting(settings, PflagKeys.RiskScoreThreshold);
            AssertResetSetting(settings, PflagKeys.AdditionalParameters);
            AssertResetSetting(settings, PflagKeys.AdditionalEnvironment);
            AssertResetSetting(settings, PflagKeys.ScanCommandConfig);
        }

        [Fact]
        public void BuildFolderConfigs_NullResetSettingSerializesValueNull()
        {
            // ConfigSetting.Value has no NullValueHandling.Ignore, so a reset serializes the explicit
            // "value": null the LS reset path expects.
            SetupDefaults();
            var fc = new FolderConfig { FolderPath = "/repo" };
            fc.Set(PflagKeys.SnykCodeEnabled, null);
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { fc });

            var lspFolderConfig = cut.GetInitializationOptions().FolderConfigs[0];
            var json = JObject.Parse(JsonConvert.SerializeObject(lspFolderConfig));
            var setting = json["settings"][PflagKeys.SnykCodeEnabled];

            Assert.Equal(JTokenType.Null, setting["value"].Type);
            Assert.Equal(true, setting["changed"].Value<bool>());
        }

        private static void AssertResetSetting(
            IDictionary<string, ConfigSetting> settings,
            string key)
        {
            Assert.True(settings.ContainsKey(key), $"{key} reset setting should be present");
            Assert.Null(settings[key].Value);
            Assert.True(settings[key].Changed, $"{key} reset changed should be true");
        }

        [Fact]
        public void GetInitializationOptions_ReturnsNonNull()
        {
            SetupDefaults();
            var result = cut.GetInitializationOptions();
            Assert.NotNull(result);
            Assert.NotNull(result.Settings);
        }

        [Fact]
        public void GetInitializationOptions_SetsRequiredProtocolVersion()
        {
            SetupDefaults();
            var result = cut.GetInitializationOptions();
            Assert.Equal("25", result.RequiredProtocolVersion);
        }

        [Fact]
        public void GetInitializationOptions_OsPlatformIsGoosStyle()
        {
            SetupDefaults();
            var result = cut.GetInitializationOptions();
            var valid = new[] { "windows", "linux", "darwin", "unknown" };
            Assert.Contains(result.OsPlatform, valid);
        }

        [Fact]
        public void GetInitializationOptions_OsArchIsGoarchStyle()
        {
            SetupDefaults();
            var result = cut.GetInitializationOptions();

            // Must be the GOARCH name for the current process architecture, never the .NET "X64"
            // spelling the Language Server doesn't understand.
            string expected;
            switch (RuntimeInformation.OSArchitecture)
            {
                case Architecture.X64: expected = "amd64"; break;
                case Architecture.Arm64: expected = "arm64"; break;
                case Architecture.X86: expected = "386"; break;
                default: expected = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(); break;
            }

            Assert.Equal(expected, result.OsArch);
        }

        [Fact]
        public void BuildSettingsMap_ContainsAllProductKeys()
        {
            SetupDefaults();
            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.True(map.ContainsKey(PflagKeys.SnykOssEnabled));
            Assert.True(map.ContainsKey(PflagKeys.SnykCodeEnabled));
            Assert.True(map.ContainsKey(PflagKeys.SnykIacEnabled));
            Assert.True(map.ContainsKey(PflagKeys.SnykSecretsEnabled));
        }

        [Fact]
        public void BuildSettingsMap_IndividualSeverityKeys()
        {
            SetupDefaults();
            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.True(map.ContainsKey(PflagKeys.SeverityFilterCritical));
            Assert.True(map.ContainsKey(PflagKeys.SeverityFilterHigh));
            Assert.True(map.ContainsKey(PflagKeys.SeverityFilterMedium));
            Assert.True(map.ContainsKey(PflagKeys.SeverityFilterLow));
            Assert.Equal(true, map[PflagKeys.SeverityFilterCritical].Value);
            Assert.Equal(true, map[PflagKeys.SeverityFilterHigh].Value);
            Assert.Equal(false, map[PflagKeys.SeverityFilterMedium].Value);
            Assert.Equal(false, map[PflagKeys.SeverityFilterLow].Value);
        }

        // ACC-001: A key the user has NOT overridden is sent with changed:false.
        [Fact]
        public void BuildSettingsMap_UntouchedKey_NotMarkedChanged()
        {
            SetupDefaults();
            // Tracker returns false for SnykOssEnabled (user never changed it).
            var trackerMock = new Mock<IUserOverrideTracker>();
            trackerMock.Setup(t => t.IsSeeded).Returns(true); // seeded so the gate is active
            trackerMock.Setup(t => t.IsChanged(It.IsAny<string>())).Returns(false);
            trackerMock.Setup(t => t.ConsumePendingResets()).Returns(new List<string>());
            optionsManagerMock.Setup(m => m.OverrideTracker).Returns(trackerMock.Object);

            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.False(map[PflagKeys.SnykOssEnabled].Changed,
                "An untouched key must be sent with changed:false so org defaults are not clobbered");
        }

        // ACC-002: A key the user HAS overridden is sent with changed:true.
        [Fact]
        public void BuildSettingsMap_UserChangedKey_MarkedChanged()
        {
            SetupDefaults();
            var trackerMock = new Mock<IUserOverrideTracker>();
            trackerMock.Setup(t => t.IsSeeded).Returns(true); // seeded so the gate is active
            trackerMock.Setup(t => t.IsChanged(PflagKeys.SnykOssEnabled)).Returns(true);
            trackerMock.Setup(t => t.IsChanged(It.Is<string>(k => k != PflagKeys.SnykOssEnabled))).Returns(false);
            trackerMock.Setup(t => t.ConsumePendingResets()).Returns(new List<string>());
            optionsManagerMock.Setup(m => m.OverrideTracker).Returns(trackerMock.Object);

            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.True(map[PflagKeys.SnykOssEnabled].Changed,
                "A user-overridden key must be sent with changed:true");
        }

        // ACC-005: trusted_folders is always marked changed regardless of tracker.
        // Uses a REAL seeded UserOverrideTracker (empty changed set) so the assertion exercises the
        // real PflagKeys.IsAlwaysChanged path — a mock that stubs IsChanged=true cannot detect
        // an AlwaysChanged regression (it would pass even if IsAlwaysChanged were removed).
        [Fact]
        public void BuildSettingsMap_AlwaysChangedKeys_AlwaysMarked()
        {
            SetupDefaults();
            // Real tracker seeded with all-defaults (empty changed set) — no key has been explicitly marked.
            var realTracker = new UserOverrideTracker();
            realTracker.SeedFrom(BuildDefaultOptionsForSeed());
            optionsManagerMock.Setup(m => m.OverrideTracker).Returns(realTracker);

            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.True(map[PflagKeys.TrustedFolders].Changed,
                "trusted_folders must always be sent with changed:true even when the tracker has no marks, " +
                "because PflagKeys.IsAlwaysChanged must return true for it");
        }

        // ACC-004: A key that was reset emits {value:null, changed:true} and is not re-emitted.
        [Fact]
        public void BuildSettingsMap_ResetKey_EmitsNullChangedTrue_AndUnmarks()
        {
            SetupDefaults();
            var trackerMock = new Mock<IUserOverrideTracker>();
            trackerMock.Setup(t => t.IsSeeded).Returns(true); // seeded so pending-reset loop runs
            trackerMock.Setup(t => t.IsChanged(It.IsAny<string>())).Returns(false);
            // ConsumePendingResets returns SnykOssEnabled as a key to reset.
            trackerMock.Setup(t => t.ConsumePendingResets())
                       .Returns(new List<string> { PflagKeys.SnykOssEnabled });
            optionsManagerMock.Setup(m => m.OverrideTracker).Returns(trackerMock.Object);

            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.True(map.ContainsKey(PflagKeys.SnykOssEnabled));
            Assert.Null(map[PflagKeys.SnykOssEnabled].Value);
            Assert.True(map[PflagKeys.SnykOssEnabled].Changed,
                "A reset key must be sent with value:null, changed:true");
        }

        // INT-003: Composition-root wiring — BuildSettingsMap uses the tracker injected via
        // ISnykOptionsManager.OverrideTracker; this test fails if the wiring is removed.
        [Fact]
        public void BuildSettingsMap_UsesInjectedTracker()
        {
            SetupDefaults();
            var trackerMock = new Mock<IUserOverrideTracker>();
            trackerMock.Setup(t => t.IsSeeded).Returns(true); // seeded so IsChanged is actually consulted
            var trackerCalled = false;
            trackerMock.Setup(t => t.IsChanged(It.IsAny<string>()))
                       .Callback<string>(_ => trackerCalled = true)
                       .Returns(false);
            trackerMock.Setup(t => t.ConsumePendingResets()).Returns(new List<string>());
            optionsManagerMock.Setup(m => m.OverrideTracker).Returns(trackerMock.Object);

            cut.BuildSettingsMap(optionsMock.Object);

            Assert.True(trackerCalled,
                "BuildSettingsMap must consult the tracker from ISnykOptionsManager.OverrideTracker. " +
                "This test fails if the wiring is removed from the production path.");
        }

        // R5-1e: An UNSEEDED real tracker causes Cs() to fall back to changed:true (safe pre-IDE-2152 behavior).
        // This proves that a startup race (BuildSettingsMap before Load()) cannot silently downgrade overrides.
        [Fact]
        public void BuildSettingsMap_UnseededTracker_FallsBackToChangedTrue()
        {
            SetupDefaults();
            // Real tracker with no SeedFrom call — IsSeeded is false.
            var unseededTracker = new UserOverrideTracker();
            Assert.False(unseededTracker.IsSeeded); // precondition
            optionsManagerMock.Setup(m => m.OverrideTracker).Returns(unseededTracker);

            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.True(map[PflagKeys.SnykOssEnabled].Changed,
                "An unseeded tracker must fall back to changed:true — " +
                "BuildSettingsMap may run before Load() at startup; silently sending changed:false " +
                "would let org defaults override the user's persisted settings");
        }

        // R5-1f: A SEEDED tracker with an empty changed set emits changed:false for normal (non-always-changed) keys.
        // Proves the seeded path still distinguishes between overridden and untouched keys.
        [Fact]
        public void BuildSettingsMap_SeededEmptyTracker_EmitsChangedFalseForNormalKeys()
        {
            SetupDefaults();
            // Real tracker seeded with all-default options — no key is marked changed.
            var seededTracker = new UserOverrideTracker();
            seededTracker.SeedFrom(BuildDefaultOptionsForSeed());
            Assert.True(seededTracker.IsSeeded); // precondition
            optionsManagerMock.Setup(m => m.OverrideTracker).Returns(seededTracker);

            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.False(map[PflagKeys.SnykOssEnabled].Changed,
                "A seeded tracker with an empty changed set must emit changed:false for normal keys — " +
                "the seeded path must distinguish overridden from untouched keys");
        }

        [Fact]
        public void BuildSettingsMap_ClientProtocolVersionPresent()
        {
            SetupDefaults();
            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.True(map.ContainsKey(PflagKeys.ClientProtocolVersion));
            Assert.Equal("25", map[PflagKeys.ClientProtocolVersion].Value);
        }

        // R3-3: DeviceId and ClientProtocolVersion are emitted via ConfigSetting.Of (always changed:true),
        // NOT via the tracker-gated Cs(). Assert changed:true directly so a regression switching them
        // to Cs() (which would make them changed:false for new users) is caught.
        [Fact]
        public void BuildSettingsMap_DeviceIdAndClientProtocolVersion_AlwaysChangedTrue()
        {
            SetupDefaults();
            // Wire a real seeded tracker with empty changed set — neither DeviceId nor ClientProtocolVersion
            // should be in AlwaysChanged, so they must be true via ConfigSetting.Of, not the tracker.
            // Seeding is required so the IsSeeded gate is active (unseeded fallback would trivially pass).
            var realTracker = new UserOverrideTracker();
            realTracker.SeedFrom(BuildDefaultOptionsForSeed());
            optionsManagerMock.Setup(m => m.OverrideTracker).Returns(realTracker);

            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.True(map[PflagKeys.DeviceId].Changed,
                "device_id must always be sent with changed:true (ConfigSetting.Of, not tracker-gated)");
            Assert.True(map[PflagKeys.ClientProtocolVersion].Changed,
                "client_protocol_version must always be sent with changed:true (ConfigSetting.Of, not tracker-gated)");
        }

        [Fact]
        public void Serialization_DictionaryKeysPreservedAsSnakeCase()
        {
            SetupDefaults();
            var param = new LspConfigurationParam
            {
                Settings = new Dictionary<string, ConfigSetting>
                {
                    [PflagKeys.SnykOssEnabled] = ConfigSetting.Of(true),
                    [PflagKeys.SeverityFilterCritical] = ConfigSetting.Of(true),
                }
            };

            var json = JsonConvert.SerializeObject(param);
            var obj = JObject.Parse(json);
            var settings = obj["settings"] as JObject;

            Assert.NotNull(settings);
            Assert.True(settings.ContainsKey("snyk_oss_enabled"), "Dictionary key must be snake_case, not camelCased");
            Assert.True(settings.ContainsKey("severity_filter_critical"), "Dictionary key must be snake_case");
            Assert.False(settings.ContainsKey("snykOssEnabled"), "camelCase key must not appear");
        }

        [Fact]
        public void Serialization_ConfigSettingPropertiesCamelCased()
        {
            var setting = ConfigSetting.Of(true);
            var json = JsonConvert.SerializeObject(setting);
            var obj = JObject.Parse(json);

            Assert.True(obj.ContainsKey("value"), "value should be camelCase");
            Assert.True(obj.ContainsKey("changed"), "changed should be camelCase");
            Assert.False(obj.ContainsKey("Value"), "PascalCase must not appear");
            Assert.False(obj.ContainsKey("isLocked"), "isLocked must be omitted when false (matches snyk-ls omitempty)");
        }

        [Fact]
        public void Serialization_InitializationOptionsV25TopLevelPropertiesCamelCased()
        {
            SetupDefaults();
            var init = cut.GetInitializationOptions();
            var json = JsonConvert.SerializeObject(init);
            var obj = JObject.Parse(json);

            Assert.True(obj.ContainsKey("requiredProtocolVersion"), "requiredProtocolVersion should be camelCase");
            Assert.True(obj.ContainsKey("integrationName"), "integrationName should be camelCase");
            Assert.False(obj.ContainsKey("RequiredProtocolVersion"), "PascalCase must not appear");
        }

        [Fact]
        public void BuildSettingsMap_ScanAutomatic_SourcesPersistedAutoScan_NotRuntimeGate()
        {
            // Regression: scan_automatic must carry the persisted user preference (AutoScan), not the
            // InternalAutoScan runtime gate. Sourcing the gate let the gate's post-first-scan `true`
            // overwrite a manual-mode choice on the next config round-trip, so manual mode never stuck.
            SetupDefaults();
            optionsMock.SetupGet(o => o.AutoScan).Returns(false);   // user picked manual
            optionsMock.SetupGet(o => o.InternalAutoScan).Returns(true); // gate already flipped after first scan

            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.Equal(false, map[PflagKeys.ScanAutomatic].Value);
        }

        [Fact]
        public void BuildSettingsMap_RiskScoreThreshold_NotIncludedWhenNull()
        {
            SetupDefaults();
            optionsMock.SetupGet(o => o.RiskScoreThreshold).Returns((int?)null);
            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.False(map.ContainsKey(PflagKeys.RiskScoreThreshold));
        }

        [Fact]
        public void BuildSettingsMap_RiskScoreThreshold_IncludedWhenSet()
        {
            SetupDefaults();
            optionsMock.SetupGet(o => o.RiskScoreThreshold).Returns(500);
            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.True(map.ContainsKey(PflagKeys.RiskScoreThreshold));
            Assert.Equal(500, map[PflagKeys.RiskScoreThreshold].Value);
        }

        [Fact]
        public void BuildSettingsMap_AdditionalParameters_InGlobalMap()
        {
            // additional_parameters is now sent in the global settings map (Project Defaults tab)
            SetupDefaults();
            var expectedParams = new List<string> { "--severity-threshold=high", "--debug" };
            optionsMock.SetupGet(o => o.AdditionalParameters).Returns(expectedParams);

            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.True(map.ContainsKey(PflagKeys.AdditionalParameters));
            // Sent as space-joined string — LS applyCliConfig reads via settingStr (string type-assert).
            var value = Assert.IsType<string>(map[PflagKeys.AdditionalParameters].Value);
            Assert.Equal("--severity-threshold=high --debug", value);
        }

        [Fact]
        public void BuildSettingsMap_AdditionalEnvironment_InGlobalMap()
        {
            // additional_environment is sent in the global settings map (Project Defaults tab)
            SetupDefaults();
            optionsMock.SetupGet(o => o.AdditionalEnv).Returns("VAR1=a;VAR2=b");

            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.True(map.ContainsKey(PflagKeys.AdditionalEnvironment));
            Assert.Equal("VAR1=a;VAR2=b", map[PflagKeys.AdditionalEnvironment].Value);
        }

        [Fact]
        public void BuildFolderConfigs_EmptyList_ReturnsEmptyList()
        {
            SetupDefaults();
            var result = cut.GetInitializationOptions();
            // FolderConfigs is empty list from SetupOptionsMock
            Assert.NotNull(result.FolderConfigs);
            Assert.Empty(result.FolderConfigs);
        }

        [Fact]
        public void BuildFolderConfigs_ForwardsSettingsMapVerbatim()
        {
            // Opaque-map model: every key in the folder's Settings map is forwarded as-is, no
            // cherry-pick. Replaces the old per-field MapsToSettings test.
            SetupDefaults();
            var folder = new FolderConfig { FolderPath = "/repo/myproject" };
            folder.SetString(PflagKeys.BaseBranch, "main");
            folder.SetString(PflagKeys.PreferredOrg, "my-org");
            folder.Set(PflagKeys.OrgSetByUser, true);
            folder.SetString(PflagKeys.AutoDeterminedOrg, "auto-org");
            folder.Set(PflagKeys.AdditionalParameters, new List<string> { "--debug", "--verbose" });
            folder.SetString(PflagKeys.AdditionalEnvironment, "FOO=bar");
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { folder });

            var result = cut.GetInitializationOptions();

            Assert.Single(result.FolderConfigs);
            var fc = result.FolderConfigs[0];
            Assert.Equal("/repo/myproject", fc.FolderPath);
            Assert.Equal("main", fc.Settings[PflagKeys.BaseBranch].Value);
            Assert.Equal("my-org", fc.Settings[PflagKeys.PreferredOrg].Value);
            Assert.Equal(true, fc.Settings[PflagKeys.OrgSetByUser].Value);
            Assert.Equal("auto-org", fc.Settings[PflagKeys.AutoDeterminedOrg].Value);
            var apValue = Assert.IsType<List<string>>(fc.Settings[PflagKeys.AdditionalParameters].Value);
            Assert.Equal(new List<string> { "--debug", "--verbose" }, apValue);
        }

        [Fact]
        public void BuildFolderConfigs_OnlyKeysInMapAreEmitted()
        {
            // Opaque-map model: keys absent from the map are not emitted (PATCH semantics). Unlike
            // the old typed model, OrgSetByUser is no longer always emitted — only when set.
            SetupDefaults();
            var folder = new FolderConfig { FolderPath = "/repo/myproject" };
            folder.Set(PflagKeys.SnykCodeEnabled, false);
            folder.Set(PflagKeys.SnykOssEnabled, true);
            folder.Set(PflagKeys.RiskScoreThreshold, 500);
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { folder });

            var result = cut.GetInitializationOptions();
            var fc = result.FolderConfigs[0];

            Assert.Equal(false, fc.Settings[PflagKeys.SnykCodeEnabled].Value);
            Assert.Equal(true, fc.Settings[PflagKeys.SnykOssEnabled].Value);
            Assert.Equal(500, fc.Settings[PflagKeys.RiskScoreThreshold].Value);
            Assert.False(fc.Settings.ContainsKey(PflagKeys.SnykIacEnabled));
            Assert.False(fc.Settings.ContainsKey(PflagKeys.SeverityFilterLow));
            Assert.False(fc.Settings.ContainsKey(PflagKeys.OrgSetByUser));
        }

        [Fact]
        public void BuildFolderConfigs_EmptyMap_EmitsEmptySettings()
        {
            SetupDefaults();
            var folder = new FolderConfig { FolderPath = "/repo/myproject" };
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { folder });

            var result = cut.GetInitializationOptions();
            var fc = result.FolderConfigs[0];

            Assert.Empty(fc.Settings);
        }

        [Fact]
        public void BuildFolderConfigs_AdditionalParameters_SerializesAsArray()
        {
            SetupDefaults();
            var folder = new FolderConfig { FolderPath = "/repo" };
            folder.Set(PflagKeys.AdditionalParameters, new List<string> { "--debug" });
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { folder });

            var result = cut.GetInitializationOptions();
            var json = JsonConvert.SerializeObject(result.FolderConfigs);
            var arr = JArray.Parse(json);
            var settings = arr[0]["settings"] as JObject;

            Assert.NotNull(settings);
            var apValue = settings[PflagKeys.AdditionalParameters]?["value"];
            Assert.Equal(JTokenType.Array, apValue?.Type);
        }

        [Fact]
        public void GetLspConfigurationParam_ReturnsNonNull()
        {
            SetupDefaults();
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig>());

            var result = cut.GetLspConfigurationParam();

            Assert.NotNull(result);
            Assert.NotNull(result.Settings);
        }

        [Fact]
        public void GetLspConfigurationParam_ContainsSameKeysAsInitSettings()
        {
            SetupDefaults();
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig>());

            var configParam = cut.GetLspConfigurationParam();
            var initOptions = cut.GetInitializationOptions();

            Assert.Equal(initOptions.Settings.Keys, configParam.Settings.Keys);
        }

        [Fact]
        public void GetLspConfigurationParam_EmptyFolderConfigs_WhenNullInput()
        {
            SetupDefaults();
            optionsMock.SetupGet(o => o.FolderConfigs).Returns((List<FolderConfig>)null);

            var result = cut.GetLspConfigurationParam();

            Assert.NotNull(result.FolderConfigs);
            Assert.Empty(result.FolderConfigs);
        }

        // RACC-001 (Acceptance / RED gate): Org pushes a non-default value, user saves WITHOUT
        // editing that key → the key must NOT be marked as a user override, so subsequent org
        // changes keep propagating. The correct path is ApplyUserEdits with empty editedKeys,
        // which leaves org-pushed values untouched in the tracker.
        [Fact]
        public void OrgPushedValue_UserSavesWithoutEditingIt_StillSentUnmarked_SoOrgChangesKeepPropagating()
        {
            SetupDefaults();
            // The org pushed OssEnabled=false (non-default) into Options.
            // The user opens settings and saves without changing OssEnabled (editedKeys is empty).
            // The tracker must NOT mark OssEnabled as a user override.

            // Use a real seeded tracker + real UserOverrideTracker after an ApplyUserEdits({}) call.
            var realTracker = new UserOverrideTracker();
            realTracker.SeedFrom(BuildDefaultOptionsForSeed()); // seeded, clean slate

            // Simulate: org pushed OssEnabled=false into Options (non-default).
            // Then the user saves with an empty edited-key set (didn't touch anything).
            var optionsWithOrgPush = new Mock<ISnykOptions>();
            optionsWithOrgPush.SetupGet(x => x.OssEnabled).Returns(false); // org-pushed non-default
            // (other keys are not queried by ApplyUserEdits when editedKeys is empty)

            realTracker.ApplyUserEdits(optionsWithOrgPush.Object, new List<string>()); // no edits

            optionsManagerMock.Setup(m => m.OverrideTracker).Returns(realTracker);

            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.False(map[PflagKeys.SnykOssEnabled].Changed,
                "An org-pushed value that the user did NOT edit must remain sent with changed:false. " +
                "Using ApplyUserEdits with empty editedKeys correctly leaves the key unmarked so " +
                "subsequent org changes keep propagating (the LS does not treat it as user-frozen).");
        }

        // RACC-002 (Acceptance): HTML bridge marks only edited keys, not untouched org values.
        // This tests the bridge-level edit-delta derivation: a key whose value was the same
        // before and after the apply step must NOT appear in the editedKeys passed to Save.
        [Fact]
        public void SaveIdeConfig_MarksOnlyEditedKeys_NotUntouchedOrgValues()
        {
            SetupDefaults();
            // Org pushed OssEnabled=false. User only edited IacEnabled (set to false).
            // Expected: IacEnabled marked (edited), OssEnabled NOT marked (untouched).

            // Real tracker, seeded clean.
            var realTracker = new UserOverrideTracker();
            realTracker.SeedFrom(BuildDefaultOptionsForSeed());

            // Simulate the pre-apply snapshot: OssEnabled=false (org-pushed), IacEnabled=true (default).
            // After apply: user changed IacEnabled to false; OssEnabled still false (untouched).
            // editedKeys derived by bridge: {IacEnabled} only.
            realTracker.ApplyUserEdits(
                // options after apply — OssEnabled=false (org), IacEnabled=false (user-edited)
                new Func<ISnykOptions>(() =>
                {
                    var o = new Mock<ISnykOptions>();
                    o.SetupGet(x => x.OssEnabled).Returns(false);  // org-pushed, not edited
                    o.SetupGet(x => x.IacEnabled).Returns(false);  // user-edited
                    o.SetupGet(x => x.SnykCodeSecurityEnabled).Returns(true);
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
                    o.SetupGet(x => x.CliReleaseChannel).Returns(Download.SnykCliDownloader.DefaultReleaseChannel);
                    o.SetupGet(x => x.CliBaseDownloadURL).Returns(Download.SnykCliDownloader.DefaultBaseDownloadUrl);
                    o.SetupGet(x => x.AdditionalEnv).Returns(string.Empty);
                    o.SetupGet(x => x.AdditionalParameters).Returns(new List<string>());
                    o.SetupGet(x => x.RiskScoreThreshold).Returns((int?)null);
                    o.SetupGet(x => x.TrustedFolders).Returns(new System.Collections.Generic.HashSet<string>());
                    o.SetupGet(x => x.ApiToken).Returns(
                        new AuthenticationToken(AuthenticationType.OAuth, string.Empty));
                    o.SetupGet(x => x.AuthenticationMethod).Returns(default(AuthenticationType));
                    return o.Object;
                })(),
                // editedKeys: only IacEnabled (the bridge derives this from the pre/post snapshot diff)
                new List<string> { PflagKeys.SnykIacEnabled });

            optionsManagerMock.Setup(m => m.OverrideTracker).Returns(realTracker);

            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.True(map[PflagKeys.SnykIacEnabled].Changed,
                "IacEnabled was in the edit delta (user changed it) → must be marked changed");
            Assert.False(map[PflagKeys.SnykOssEnabled].Changed,
                "OssEnabled was NOT in the edit delta (org-pushed, user didn't touch it) → must remain unmarked");
        }

        // RACC-003 (corrected semantics, PR #515): A key the user edits to its default value via the
        // form goes out as an explicit user-owned value ({value, changed:true}), NOT a reset — the
        // edit is an explicit choice. Untouched keys at their defaults never emit a reset either.
        // (Reset-to-default is no longer inferred inside ApplyUserEdits from value==default.)
        [Fact]
        public void UserEditsKeyToDefault_SendsExplicitValueChangedTrue_ButUntouchedDefaultsNeverEmitReset()
        {
            SetupDefaults();
            // User edits OssEnabled through the form, landing on the default value true.
            // IacEnabled is at its default (true) and was NOT edited.
            // Expected: OssEnabled sent {value:true, changed:true} (no reset); IacEnabled not reset.

            var realTracker = new UserOverrideTracker();
            realTracker.MarkSeeded(); // mark seeded so BuildSettingsMap's IsSeeded gate is active

            // User's edit: OssEnabled→true (its default). IacEnabled not touched (not in editedKeys).
            var options = new Mock<ISnykOptions>();
            options.SetupGet(x => x.OssEnabled).Returns(true); // user-applied value == default
            options.SetupGet(x => x.IacEnabled).Returns(true);
            options.SetupGet(x => x.SnykCodeSecurityEnabled).Returns(true);
            options.SetupGet(x => x.SecretsEnabled).Returns(false);
            options.SetupGet(x => x.AutoScan).Returns(true);
            options.SetupGet(x => x.EnableDeltaFindings).Returns(false);
            options.SetupGet(x => x.FilterCritical).Returns(true);
            options.SetupGet(x => x.FilterHigh).Returns(true);
            options.SetupGet(x => x.FilterMedium).Returns(true);
            options.SetupGet(x => x.FilterLow).Returns(true);
            options.SetupGet(x => x.OpenIssuesEnabled).Returns(true);
            options.SetupGet(x => x.IgnoredIssuesEnabled).Returns(false);
            options.SetupGet(x => x.CustomEndpoint).Returns((string)null);
            options.SetupGet(x => x.Organization).Returns((string)null);
            options.SetupGet(x => x.IgnoreUnknownCA).Returns(false);
            options.SetupGet(x => x.BinariesAutoUpdate).Returns(true);
            options.SetupGet(x => x.CliCustomPath).Returns(string.Empty);
            options.SetupGet(x => x.CliReleaseChannel).Returns(Download.SnykCliDownloader.DefaultReleaseChannel);
            options.SetupGet(x => x.CliBaseDownloadURL).Returns(Download.SnykCliDownloader.DefaultBaseDownloadUrl);
            options.SetupGet(x => x.AdditionalEnv).Returns(string.Empty);
            options.SetupGet(x => x.AdditionalParameters).Returns(new List<string>());
            options.SetupGet(x => x.RiskScoreThreshold).Returns((int?)null);
            options.SetupGet(x => x.TrustedFolders).Returns(new System.Collections.Generic.HashSet<string>());
            options.SetupGet(x => x.ApiToken).Returns(
                new AuthenticationToken(AuthenticationType.OAuth, string.Empty));
            options.SetupGet(x => x.AuthenticationMethod).Returns(default(AuthenticationType));

            realTracker.ApplyUserEdits(options.Object, new List<string> { PflagKeys.SnykOssEnabled });

            optionsManagerMock.Setup(m => m.OverrideTracker).Returns(realTracker);
            optionsMock.SetupGet(o => o.RiskScoreThreshold).Returns((int?)null);

            var map = cut.BuildSettingsMap(optionsMock.Object);

            // OssEnabled was in editedKeys → sent as an explicit user-owned value, NOT a reset.
            Assert.True(map.ContainsKey(PflagKeys.SnykOssEnabled),
                "OssEnabled must be in the map");
            Assert.NotNull(map[PflagKeys.SnykOssEnabled].Value); // explicit value, NOT a reset (value:null)
            Assert.True(map[PflagKeys.SnykOssEnabled].Changed,
                "An edited key must emit changed:true so the org default cannot override it");

            // IacEnabled was NOT in editedKeys → no reset emitted (its normal value entry is present).
            Assert.NotNull(map[PflagKeys.SnykIacEnabled].Value); // not edited → must not emit a reset
        }

        // RACC-004 (Acceptance / PR #515 regression): Enabling the global Snyk Code toggle must
        // persist as an explicit user-owned value, not be turned into a reset-to-default signal.
        //
        // Snyk Code's plugin default is `true` (ConfigDefaults). Under the OLD (buggy) semantics
        // ApplyUserEdits classified an edited key by value==plugin-default, so a user *enabling*
        // Code (posting true) hit the Unmark branch → BuildSettingsMap emitted a Reset
        // ({value:null, changed:true}) → the org/server default won → the enable appeared not to
        // persist. The fix: any key the form posted (present in editedKeys) is an explicit user
        // choice and must be Mark'd (changed:true) carrying the user's value, never a reset.
        [Fact]
        public void EnablingSnykCode_SendsExplicitEnabledValueChangedTrue_NotReset()
        {
            SetupDefaults(); // SnykCodeSecurityEnabled == true (the plugin default the user just enabled)

            var realTracker = new UserOverrideTracker();
            realTracker.SeedFrom(BuildDefaultOptionsForSeed()); // clean seeded slate, Code unmarked

            // The user enables Snyk Code in the settings form and applies. The form posts
            // snyk_code_enabled=true and includes it in the edit delta. Even though true equals the
            // plugin default, the user made an explicit choice — it must be recorded as an override.
            var optionsAfterSave = new Mock<ISnykOptions>();
            optionsAfterSave.SetupGet(x => x.SnykCodeSecurityEnabled).Returns(true); // user-enabled == plugin default

            realTracker.ApplyUserEdits(
                optionsAfterSave.Object,
                new List<string> { PflagKeys.SnykCodeEnabled });

            optionsManagerMock.Setup(m => m.OverrideTracker).Returns(realTracker);

            var map = cut.BuildSettingsMap(optionsMock.Object);

            // The user's explicit enable must go out as {value:true, changed:true} — NOT a reset.
            Assert.True(map.ContainsKey(PflagKeys.SnykCodeEnabled),
                "snyk_code_enabled must be present in the settings map");
            Assert.NotNull(map[PflagKeys.SnykCodeEnabled].Value); // must NOT be a reset (value:null)
            Assert.Equal(true, map[PflagKeys.SnykCodeEnabled].Value); // carries the user's enabled value
            Assert.True(map[PflagKeys.SnykCodeEnabled].Changed,
                "An explicitly enabled Snyk Code toggle must be marked changed so the org default " +
                "cannot silently override the user's choice");
        }

        // UNIT-008: ConfigSetting.Of(value, changed) overload sets Changed correctly;
        // ConfigSetting.Reset() sets value=null, changed=true.
        [Fact]
        public void ConfigSetting_Factories_SetChangedCorrectly()
        {
            // Of(value) — existing factory, always changed:true.
            var withChanged = ConfigSetting.Of("v");
            Assert.True(withChanged.Changed);
            Assert.Equal("v", withChanged.Value);

            // Of(value, changed:false) — new overload, not changed:
            var notChanged = ConfigSetting.Of("v", changed: false);
            Assert.False(notChanged.Changed);
            Assert.Equal("v", notChanged.Value);

            // Of(value, changed:true) — new overload, explicit true:
            var explicitChanged = ConfigSetting.Of("v", changed: true);
            Assert.True(explicitChanged.Changed);

            // Reset() — value=null, changed=true (used for un-marking a setting):
            var reset = ConfigSetting.Reset();
            Assert.Null(reset.Value);
            Assert.True(reset.Changed);
        }
    }
}
