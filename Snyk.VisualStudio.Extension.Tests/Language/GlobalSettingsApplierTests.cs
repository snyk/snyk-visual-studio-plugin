using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using Newtonsoft.Json.Linq;
using Snyk.VisualStudio.Extension.Authentication;
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
            // An empty inbound value means "no opinion", not "cleared".
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
        public void Apply_ShouldApplyANonEmptyEcho_EvenWhenItMatchesWhatWeSent()
        {
            // The echo of our own resolved value is applied like any other: a no-op, or a normalisation
            // when what we hold is padded.
            var options = MakeOptions();
            options.CliBaseDownloadURL = "  https://mirror.corp  ";
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.BinaryBaseUrl] = ConfigSetting.Of("https://mirror.corp")
            };

            GlobalSettingsApplier.Apply(settings, options);

            Assert.Equal("https://mirror.corp", options.CliBaseDownloadURL);
        }

        // cli_path is IDE-owned: the IDE downloads the binary and tells the LS where it is, never the
        // other way round. Nothing can legitimately push one to us — snyk-ls keeps cli_path out of its
        // LDX-Sync key map and out of GlobalResettableSettings — so an inbound value is only ever our
        // own echoed back, or the LS's registered default. Adopting the latter repoints us at an empty
        // location and costs the user a second full CLI download.

        [Fact]
        public void Apply_ShouldIgnoreInboundCliPath_WhenItIsTheLanguageServerDefault()
        {
            // The regression: the LS's default is $XDG_DATA_HOME/snyk-ls/<exe>, which on Windows is
            // under %LOCALAPPDATA% but NOT our directory — non-empty, so an empty-guard misses it, and
            // it looks exactly like a user-chosen custom path.
            var options = MakeOptions();
            options.CliCustomPath = string.Empty;
            var lsDefault = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "snyk-ls",
                "snyk-win.exe");
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.CliPath] = ConfigSetting.Of(lsDefault)
            };

            GlobalSettingsApplier.Apply(settings, options);

            Assert.Equal(string.Empty, options.CliCustomPath);
        }

        [Theory]
        // Whatever CliCustomPath held, and whatever arrives, the value is left alone: empty must not
        // be taken as "cleared", and a non-empty one must not be taken as a user override.
        [InlineData("", @"C:\somewhere\else\snyk.exe")]
        [InlineData(@"C:\custom\snyk.exe", "")]
        [InlineData(@"C:\custom\snyk.exe", @"C:\somewhere\else\snyk.exe")]
        [InlineData(@"C:\custom\snyk.exe", null)]
        public void Apply_ShouldNeverChangeCliCustomPath(string priorPath, string inboundPath)
        {
            var options = MakeOptions();
            options.CliCustomPath = priorPath;
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.CliPath] = ConfigSetting.Of(inboundPath)
            };

            GlobalSettingsApplier.Apply(settings, options);

            Assert.Equal(priorPath, options.CliCustomPath);
        }

        [Fact]
        public void Apply_ShouldStillApplyOtherKeys_WhenCliPathIsPresent()
        {
            // Ignoring cli_path must not short-circuit the rest of the payload.
            var options = MakeOptions();
            options.CliCustomPath = string.Empty;
            var settings = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.CliPath] = ConfigSetting.Of(@"C:\somewhere\else\snyk.exe"),
                [PflagKeys.Organization] = ConfigSetting.Of("acme-org")
            };

            GlobalSettingsApplier.Apply(settings, options);

            Assert.Equal(string.Empty, options.CliCustomPath);
            Assert.Equal("acme-org", options.Organization);
        }
    }
}
