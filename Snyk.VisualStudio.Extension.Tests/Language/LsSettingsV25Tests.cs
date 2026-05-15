using System.Collections.Generic;
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
            TestUtils.SetupOptionsManagerMock(optionsManagerMock);
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
            Assert.Equal(LsConstants.ProtocolVersion, result.RequiredProtocolVersion);
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
            Assert.Equal(LsConstants.ProtocolVersion, map[PflagKeys.ClientProtocolVersion].Value);
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
    }
}
