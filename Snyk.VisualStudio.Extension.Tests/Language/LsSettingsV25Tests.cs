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
            optionsMock.SetupGet(o => o.RiskScoreThreshold).Returns((int?)null);
            optionsMock.SetupGet(o => o.InternalAutoScan).Returns(false);
        }

        [Fact]
        public void FolderConfigOverrides_RoundTripThroughBuildAndApply()
        {
            // Guards the outbound/inbound symmetry: BuildFolderConfigs emits the per-folder
            // overrides and FolderConfigApplier.ToFolderConfig must read every one of them back,
            // or a $/snyk.configuration echo silently drops the user's per-folder override.
            SetupDefaults();
            var original = new FolderConfig
            {
                FolderPath = "/repo",
                SnykOssEnabled = true,
                SnykCodeEnabled = false,
                SnykIacEnabled = true,
                SnykSecretsEnabled = false,
                ScanAutomatic = true,
                ScanNetNew = false,
                SeverityFilterCritical = true,
                SeverityFilterHigh = false,
                SeverityFilterMedium = true,
                SeverityFilterLow = false,
                IssueViewOpenIssues = true,
                IssueViewIgnoredIssues = false,
                RiskScoreThreshold = 750,
            };
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { original });

            // POCO -> BuildFolderConfigs (outbound) -> LspFolderConfig -> ToFolderConfig (inbound) -> POCO
            var lspFolderConfig = cut.GetInitializationOptions().FolderConfigs[0];
            var roundTripped = FolderConfigApplier.ToFolderConfig(lspFolderConfig);

            Assert.Equal(true, roundTripped.SnykOssEnabled);
            Assert.Equal(false, roundTripped.SnykCodeEnabled);
            Assert.Equal(true, roundTripped.SnykIacEnabled);
            Assert.Equal(false, roundTripped.SnykSecretsEnabled);
            Assert.Equal(true, roundTripped.ScanAutomatic);
            Assert.Equal(false, roundTripped.ScanNetNew);
            Assert.Equal(true, roundTripped.SeverityFilterCritical);
            Assert.Equal(false, roundTripped.SeverityFilterHigh);
            Assert.Equal(true, roundTripped.SeverityFilterMedium);
            Assert.Equal(false, roundTripped.SeverityFilterLow);
            Assert.Equal(true, roundTripped.IssueViewOpenIssues);
            Assert.Equal(false, roundTripped.IssueViewIgnoredIssues);
            Assert.Equal(750, roundTripped.RiskScoreThreshold);
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
                },
            };
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { fc });

            var settings = cut.GetInitializationOptions().FolderConfigs[0].Settings;

            AssertResetSetting(settings, PflagKeys.SnykCodeEnabled);
            AssertResetSetting(settings, PflagKeys.PreferredOrg);
            AssertResetSetting(settings, PflagKeys.RiskScoreThreshold);
        }

        [Fact]
        public void BuildFolderConfigs_ResetKeyWinsOverStoredValue()
        {
            // If a key is both stored and reset, the reset (null) must win — the user cleared it.
            SetupDefaults();
            var fc = new FolderConfig
            {
                FolderPath = "/repo",
                SnykCodeEnabled = true,
                ResetKeys = new HashSet<string> { PflagKeys.SnykCodeEnabled },
            };
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
        public void BuildSettingsMap_AdditionalParameters_NotInGlobalMap()
        {
            // additional_parameters is folder-scoped in v25; must not appear in global settings
            SetupDefaults();
            var map = cut.BuildSettingsMap(optionsMock.Object);
            Assert.False(map.ContainsKey(PflagKeys.AdditionalParameters));
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
        public void BuildFolderConfigs_FolderWithAllFields_MapsToSettings()
        {
            SetupDefaults();
            var folder = new FolderConfig
            {
                FolderPath = "/repo/myproject",
                BaseBranch = "main",
                PreferredOrg = "my-org",
                OrgSetByUser = true,
                AutoDeterminedOrg = "auto-org",
                AdditionalParameters = new List<string> { "--debug", "--verbose" },
                AdditionalEnv = "FOO=bar",
            };
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { folder });

            var result = cut.GetInitializationOptions();

            Assert.Single(result.FolderConfigs);
            var fc = result.FolderConfigs[0];
            Assert.Equal("/repo/myproject", fc.FolderPath);
            Assert.True(fc.Settings.ContainsKey(PflagKeys.BaseBranch));
            Assert.Equal("main", fc.Settings[PflagKeys.BaseBranch].Value);
            Assert.True(fc.Settings.ContainsKey(PflagKeys.PreferredOrg));
            Assert.True(fc.Settings.ContainsKey(PflagKeys.OrgSetByUser));
            Assert.Equal(true, fc.Settings[PflagKeys.OrgSetByUser].Value);
            Assert.True(fc.Settings.ContainsKey(PflagKeys.AdditionalParameters));
            var apValue = Assert.IsType<List<string>>(fc.Settings[PflagKeys.AdditionalParameters].Value);
            Assert.Equal(new List<string> { "--debug", "--verbose" }, apValue);
        }

        [Fact]
        public void BuildFolderConfigs_PerFolderOverrides_MappedWhenSet()
        {
            SetupDefaults();
            var folder = new FolderConfig
            {
                FolderPath = "/repo/myproject",
                OrgSetByUser = false,
                SnykCodeEnabled = false,
                SnykOssEnabled = true,
                SeverityFilterHigh = false,
                ScanAutomatic = true,
                IssueViewIgnoredIssues = false,
                RiskScoreThreshold = 500,
            };
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { folder });

            var result = cut.GetInitializationOptions();
            var fc = result.FolderConfigs[0];

            Assert.Equal(false, fc.Settings[PflagKeys.SnykCodeEnabled].Value);
            Assert.Equal(true, fc.Settings[PflagKeys.SnykOssEnabled].Value);
            Assert.Equal(false, fc.Settings[PflagKeys.SeverityFilterHigh].Value);
            Assert.Equal(true, fc.Settings[PflagKeys.ScanAutomatic].Value);
            Assert.Equal(false, fc.Settings[PflagKeys.IssueViewIgnoredIssues].Value);
            Assert.Equal(500, fc.Settings[PflagKeys.RiskScoreThreshold].Value);
            // Overrides not set on this folder must be omitted (PATCH semantics)
            Assert.False(fc.Settings.ContainsKey(PflagKeys.SnykIacEnabled));
            Assert.False(fc.Settings.ContainsKey(PflagKeys.SeverityFilterLow));
        }

        [Fact]
        public void BuildFolderConfigs_PerFolderOverrides_OmittedWhenNull()
        {
            SetupDefaults();
            var folder = new FolderConfig
            {
                FolderPath = "/repo/myproject",
                OrgSetByUser = false,
                // all org-scope override fields left null
            };
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { folder });

            var result = cut.GetInitializationOptions();
            var fc = result.FolderConfigs[0];

            Assert.False(fc.Settings.ContainsKey(PflagKeys.SnykOssEnabled));
            Assert.False(fc.Settings.ContainsKey(PflagKeys.SnykCodeEnabled));
            Assert.False(fc.Settings.ContainsKey(PflagKeys.SeverityFilterCritical));
            Assert.False(fc.Settings.ContainsKey(PflagKeys.ScanAutomatic));
            Assert.False(fc.Settings.ContainsKey(PflagKeys.IssueViewOpenIssues));
            Assert.False(fc.Settings.ContainsKey(PflagKeys.RiskScoreThreshold));
        }

        [Fact]
        public void BuildFolderConfigs_NullOptionalFields_OmitsThoseKeys()
        {
            SetupDefaults();
            var folder = new FolderConfig
            {
                FolderPath = "/repo/myproject",
                AdditionalParameters = null,
                AdditionalEnv = null,
                PreferredOrg = null,
                BaseBranch = null,
                ScanCommandConfig = null,
                AutoDeterminedOrg = null,
                OrgSetByUser = false,
            };
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { folder });

            var result = cut.GetInitializationOptions();
            var fc = result.FolderConfigs[0];

            Assert.False(fc.Settings.ContainsKey(PflagKeys.AdditionalParameters));
            Assert.False(fc.Settings.ContainsKey(PflagKeys.AdditionalEnvironment));
            Assert.False(fc.Settings.ContainsKey(PflagKeys.PreferredOrg));
            Assert.False(fc.Settings.ContainsKey(PflagKeys.BaseBranch));
            Assert.False(fc.Settings.ContainsKey(PflagKeys.ScanCommandConfig));
            // OrgSetByUser is always included (bool, not nullable)
            Assert.True(fc.Settings.ContainsKey(PflagKeys.OrgSetByUser));
        }

        [Fact]
        public void BuildFolderConfigs_AdditionalParameters_SerializesAsArray()
        {
            SetupDefaults();
            var folder = new FolderConfig
            {
                FolderPath = "/repo",
                AdditionalParameters = new List<string> { "--debug" },
                OrgSetByUser = false,
            };
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
