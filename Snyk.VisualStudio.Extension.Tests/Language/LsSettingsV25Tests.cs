using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Snyk.VisualStudio.Extension.Authentication;
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
            optionsMock.SetupGet(o => o.CliReleaseChannel).Returns("stable");
            optionsMock.SetupGet(o => o.CliBaseDownloadURL).Returns("https://downloads.snyk.io");
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
        public void BuildFolderConfigs_EmitsNullChangedTrueForResetKeys()
        {
            // A reset folder field must go out as {value:null, changed:true} so snyk-ls Unsets the
            // user:folder: override (fallback to org/LDX/default).
            SetupDefaults();
            var fc = new FolderConfig
            {
                FolderPath = "/repo",
                ResetKeys = new HashSet<string>
                {
                    PflagKeys.SnykCodeEnabled,
                    PflagKeys.PreferredOrg,
                    PflagKeys.RiskScoreThreshold,
                    PflagKeys.AdditionalParameters,
                    PflagKeys.AdditionalEnvironment,
                    PflagKeys.ScanCommandConfig,
                },
            };
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
        public void BuildFolderConfigs_ResetWinsForNonScalarFields()
        {
            // additional_parameters (List), additional_environment (string) and scan_command_config
            // (Dictionary) are non-scalar. A reset must emit {value:null, changed:true} even when a
            // typed value is stored, so snyk-ls Unsets the user:folder: override.
            SetupDefaults();
            var fc = new FolderConfig
            {
                FolderPath = "/repo",
                ResetKeys = new HashSet<string>
                {
                    PflagKeys.AdditionalParameters,
                    PflagKeys.AdditionalEnvironment,
                    PflagKeys.ScanCommandConfig,
                },
            };
            fc.Set(PflagKeys.AdditionalParameters, new List<string> { "--debug" });
            fc.Set(PflagKeys.AdditionalEnvironment, "FOO=bar");
            fc.Set(PflagKeys.ScanCommandConfig, new Dictionary<string, ScanCommandConfig>
            {
                ["oss"] = new ScanCommandConfig { PreScanCommand = "echo hi" },
            });
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { fc });

            var settings = cut.GetInitializationOptions().FolderConfigs[0].Settings;

            AssertResetSetting(settings, PflagKeys.AdditionalParameters);
            AssertResetSetting(settings, PflagKeys.AdditionalEnvironment);
            AssertResetSetting(settings, PflagKeys.ScanCommandConfig);
        }

        [Fact]
        public void BuildFolderConfigs_NonScalarResetWithNullStoredValue_StillEmitsNull()
        {
            // The ResetKeys-driven emit must win even when the stored non-scalar value is null/absent
            // (the guarded emits at the top of BuildFolderConfigs skip nulls; the reset must not be
            // dropped along with them).
            SetupDefaults();
            var fc = new FolderConfig
            {
                FolderPath = "/repo",
                ResetKeys = new HashSet<string>
                {
                    PflagKeys.AdditionalParameters,
                    PflagKeys.AdditionalEnvironment,
                    PflagKeys.ScanCommandConfig,
                },
            };
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { fc });

            var settings = cut.GetInitializationOptions().FolderConfigs[0].Settings;

            AssertResetSetting(settings, PflagKeys.AdditionalParameters);
            AssertResetSetting(settings, PflagKeys.AdditionalEnvironment);
            AssertResetSetting(settings, PflagKeys.ScanCommandConfig);
        }

        [Fact]
        public void BuildFolderConfigs_ResetKeyWinsOverStoredValue()
        {
            // If a key is both stored and reset, the reset (null) must win — the user cleared it.
            SetupDefaults();
            var fc = new FolderConfig
            {
                FolderPath = "/repo",
                ResetKeys = new HashSet<string> { PflagKeys.SnykCodeEnabled },
            };
            fc.Set(PflagKeys.SnykCodeEnabled, true);
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { fc });

            var settings = cut.GetInitializationOptions().FolderConfigs[0].Settings;

            AssertResetSetting(settings, PflagKeys.SnykCodeEnabled);
        }

        [Fact]
        public void BuildFolderConfigs_NullResetSettingSerializesValueNull()
        {
            // ConfigSetting.Value has no NullValueHandling.Ignore, so a reset serializes the explicit
            // "value": null the LS reset path expects.
            SetupDefaults();
            var fc = new FolderConfig
            {
                FolderPath = "/repo",
                ResetKeys = new HashSet<string> { PflagKeys.SnykCodeEnabled },
            };
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

        [Fact]
        public void BuildSettingsMap_AllEntriesHaveChangedTrue()
        {
            SetupDefaults();
            var map = cut.BuildSettingsMap(optionsMock.Object);

            foreach (var kv in map)
                Assert.True(kv.Value.Changed, $"Expected Changed=true for key '{kv.Key}'");
        }

        [Fact]
        public void BuildSettingsMap_ClientProtocolVersionPresent()
        {
            SetupDefaults();
            var map = cut.BuildSettingsMap(optionsMock.Object);

            Assert.True(map.ContainsKey(PflagKeys.ClientProtocolVersion));
            Assert.Equal("25", map[PflagKeys.ClientProtocolVersion].Value);
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
    }
}
