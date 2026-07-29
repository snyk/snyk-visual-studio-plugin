using System.Collections.Generic;
using Moq;
using Newtonsoft.Json.Linq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    public class GlobalSettingsApplierTests
    {
        private ISnykOptions MakeOptions()
        {
            var mock = new Mock<ISnykOptions>();
            mock.SetupAllProperties();
            return mock.Object;
        }

        [Fact]
        public void Apply_ShouldBeNoOp_WhenSettingsIsNull()
        {
            var options = MakeOptions();
            GlobalSettingsApplier.Apply(null, options);
            // no exception
        }

        [Fact]
        public void Apply_ShouldSetOssEnabled_WhenKeyPresent()
        {
            var options = MakeOptions();
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.SnykOssEnabled] = ConfigSetting.Of(true)
            };
            GlobalSettingsApplier.Apply(settings, options);
            Assert.True(options.OssEnabled);
        }

        [Fact]
        public void Apply_ShouldSetSecretsEnabled_WhenKeyPresent()
        {
            var options = MakeOptions();
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.SnykSecretsEnabled] = ConfigSetting.Of(false)
            };
            GlobalSettingsApplier.Apply(settings, options);
            Assert.False(options.SecretsEnabled);
        }

        [Fact]
        public void Apply_ShouldSetSeverityFilters()
        {
            var options = MakeOptions();
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.SeverityFilterCritical] = ConfigSetting.Of(true),
                [PflagKeys.SeverityFilterHigh] = ConfigSetting.Of(false),
                [PflagKeys.SeverityFilterMedium] = ConfigSetting.Of(true),
                [PflagKeys.SeverityFilterLow] = ConfigSetting.Of(false),
            };
            GlobalSettingsApplier.Apply(settings, options);
            Assert.True(options.FilterCritical);
            Assert.False(options.FilterHigh);
            Assert.True(options.FilterMedium);
            Assert.False(options.FilterLow);
        }

        [Fact]
        public void Apply_ShouldSetTrustedFolders_FromJArray()
        {
            var options = MakeOptions();
            var list = new List<string> { "/foo", "/bar" };
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.TrustedFolders] = ConfigSetting.Of(list)
            };
            // Simulate how ConfigSetting.Value comes back as JToken after deserialization
            settings[PflagKeys.TrustedFolders].Value = JToken.FromObject(list);
            GlobalSettingsApplier.Apply(settings, options);
            Assert.Contains("/foo", options.TrustedFolders);
            Assert.Contains("/bar", options.TrustedFolders);
        }

        [Fact]
        public void Apply_ShouldSetAuthMethod_OAuth()
        {
            var options = MakeOptions();
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.AuthenticationMethod] = ConfigSetting.Of("oauth")
            };
            GlobalSettingsApplier.Apply(settings, options);
            Assert.Equal(AuthenticationType.OAuth, options.AuthenticationMethod);
        }

        [Fact]
        public void Apply_ShouldSetAuthMethod_Token()
        {
            var options = MakeOptions();
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.AuthenticationMethod] = ConfigSetting.Of("token")
            };
            GlobalSettingsApplier.Apply(settings, options);
            Assert.Equal(AuthenticationType.Token, options.AuthenticationMethod);
        }

        [Fact]
        public void Apply_ShouldIgnoreUnknownKeys()
        {
            var options = MakeOptions();
            var settings = new Dictionary<string, ConfigSetting>
            {
                ["unknown_future_key"] = ConfigSetting.Of("some_value")
            };
            GlobalSettingsApplier.Apply(settings, options); // must not throw
        }

        [Fact]
        public void Apply_ShouldSkipEntry_WhenValueIsNull()
        {
            var options = MakeOptions();
            options.OssEnabled = true;
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.SnykOssEnabled] = new ConfigSetting { Value = null, Changed = false }
            };
            GlobalSettingsApplier.Apply(settings, options);
            Assert.True(options.OssEnabled); // unchanged
        }

        [Fact]
        public void Apply_ShouldSetRiskScoreThreshold()
        {
            var options = MakeOptions();
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.RiskScoreThreshold] = ConfigSetting.Of(42)
            };
            GlobalSettingsApplier.Apply(settings, options);
            Assert.Equal(42, options.RiskScoreThreshold);
        }

        [Fact]
        public void Apply_ShouldSetApiEndpoint()
        {
            var options = MakeOptions();
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.ApiEndpoint] = ConfigSetting.Of("https://api.example.com")
            };
            GlobalSettingsApplier.Apply(settings, options);
            Assert.Equal("https://api.example.com", options.CustomEndpoint);
        }

        [Fact]
        public void Apply_ShouldSetOssEnabled_FromDeserializedJson()
        {
            var options = MakeOptions();
            var json = @"{""snyk_oss_enabled"":{""value"":true,""changed"":true}}";
            var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, ConfigSetting>>(json);
            GlobalSettingsApplier.Apply(settings, options);
            Assert.True(options.OssEnabled);
        }

        [Fact]
        public void Apply_ShouldSkipKey_WhenValueHasWrongType()
        {
            var options = MakeOptions();
            options.OssEnabled = true;
            // Send a string where a bool is expected
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.SnykOssEnabled] = new ConfigSetting { Value = JToken.FromObject("not-a-bool"), Changed = true }
            };
            // Must not throw; OssEnabled must remain unchanged
            GlobalSettingsApplier.Apply(settings, options);
            Assert.True(options.OssEnabled);
        }

        [Fact]
        public void Apply_AdditionalParameters_SplitsSpaceJoinedString()
        {
            // LS sends additional_parameters as a space-joined string (strings.Join(..., " ")).
            var options = MakeOptions();
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.AdditionalParameters] = new ConfigSetting { Value = JToken.FromObject("--debug --severity-threshold=high"), Changed = true }
            };
            GlobalSettingsApplier.Apply(settings, options);
            Assert.Equal(new List<string> { "--debug", "--severity-threshold=high" }, options.AdditionalParameters);
        }

        [Fact]
        public void Apply_AdditionalParameters_HandlesEmptyString()
        {
            var options = MakeOptions();
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.AdditionalParameters] = new ConfigSetting { Value = JToken.FromObject(""), Changed = true }
            };
            GlobalSettingsApplier.Apply(settings, options);
            Assert.Empty(options.AdditionalParameters);
        }

        [Fact]
        public void Apply_AdditionalParameters_HandlesJsonArray()
        {
            // Future-proofing: if LS ever sends an array, it should still work.
            var options = MakeOptions();
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.AdditionalParameters] = new ConfigSetting { Value = JToken.FromObject(new[] { "--debug", "--verbose" }), Changed = true }
            };
            GlobalSettingsApplier.Apply(settings, options);
            Assert.Equal(new List<string> { "--debug", "--verbose" }, options.AdditionalParameters);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Apply_ShouldPreserveCliBaseDownloadUrl_WhenLsEchoesEmptyValue(string inbound)
        {
            // The LS registers binary_base_url with an empty default (register_configurations.go) and
            // buildGlobalSettingsMap echoes every machine-scope setting, so an empty inbound value means
            // "the LS has no opinion" — not "the user cleared it". Applying it wiped the IDE's canonical
            // default and left the download URL relative (/cli//ls-protocol-version-25), which resolved
            // to a nonexistent local file path.
            var options = MakeOptions();
            options.CliBaseDownloadURL = SnykCliDownloader.DefaultBaseDownloadUrl;
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.BinaryBaseUrl] = ConfigSetting.Of(inbound)
            };

            GlobalSettingsApplier.Apply(settings, options);

            Assert.Equal(SnykCliDownloader.DefaultBaseDownloadUrl, options.CliBaseDownloadURL);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Apply_ShouldPreserveCliReleaseChannel_WhenLsEchoesEmptyValue(string inbound)
        {
            var options = MakeOptions();
            options.CliReleaseChannel = SnykCliDownloader.DefaultReleaseChannel;
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.CliReleaseChannel] = ConfigSetting.Of(inbound)
            };

            GlobalSettingsApplier.Apply(settings, options);

            Assert.Equal(SnykCliDownloader.DefaultReleaseChannel, options.CliReleaseChannel);
        }

        [Fact]
        public void Apply_ShouldSetCliBaseDownloadUrlAndReleaseChannel_WhenLsSendsRealValues()
        {
            // The empty-value guard must not block genuine LS/LDX-Sync-pushed values.
            var options = MakeOptions();
            options.CliBaseDownloadURL = SnykCliDownloader.DefaultBaseDownloadUrl;
            options.CliReleaseChannel = SnykCliDownloader.DefaultReleaseChannel;
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.BinaryBaseUrl] = ConfigSetting.Of("https://downloads.snyk.io/fips"),
                [PflagKeys.CliReleaseChannel] = ConfigSetting.Of("rc"),
            };

            GlobalSettingsApplier.Apply(settings, options);

            Assert.Equal("https://downloads.snyk.io/fips", options.CliBaseDownloadURL);
            Assert.Equal("rc", options.CliReleaseChannel);
        }

        [Fact]
        public void Apply_ShouldClearCliPath_WhenInboundValueIsEmpty()
        {
            // cli_path is deliberately NOT covered by the empty-value guard: unlike the base URL and
            // release channel it has a meaningful cleared state, and an empty path resolves to the
            // default CLI location (SnykCli.GetCliFilePath).
            var options = MakeOptions();
            options.CliCustomPath = @"C:\custom\snyk.exe";
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.CliPath] = ConfigSetting.Of(string.Empty)
            };

            GlobalSettingsApplier.Apply(settings, options);

            Assert.Equal(string.Empty, options.CliCustomPath);
            Assert.Equal(SnykCli.GetSnykCliDefaultPath(), SnykCli.GetCliFilePath(options.CliCustomPath));
        }
    }
}
