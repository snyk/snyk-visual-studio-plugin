// ABOUTME: Unit tests for ConfigDefaults covering UNIT-007, UNIT-012 from the IDE-2152 test plan.
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    public class ConfigDefaultsTests
    {
        // UNIT-007: ConfigDefaults.IsDefault returns true for each global pflag key at its SnykSettings
        // default value, and false for a non-default value.
        [Fact]
        public void IsDefault_MatchesSnykSettingsDefaults()
        {
            Assert.True(ConfigDefaults.IsDefault(PflagKeys.SnykOssEnabled, true));
            Assert.False(ConfigDefaults.IsDefault(PflagKeys.SnykOssEnabled, false));

            Assert.True(ConfigDefaults.IsDefault(PflagKeys.SnykCodeEnabled, true));
            Assert.False(ConfigDefaults.IsDefault(PflagKeys.SnykCodeEnabled, false));

            Assert.True(ConfigDefaults.IsDefault(PflagKeys.SnykIacEnabled, true));
            Assert.False(ConfigDefaults.IsDefault(PflagKeys.SnykIacEnabled, false));

            Assert.True(ConfigDefaults.IsDefault(PflagKeys.SnykSecretsEnabled, false));
            Assert.False(ConfigDefaults.IsDefault(PflagKeys.SnykSecretsEnabled, true));

            Assert.True(ConfigDefaults.IsDefault(PflagKeys.ScanAutomatic, true));
            Assert.False(ConfigDefaults.IsDefault(PflagKeys.ScanAutomatic, false));

            Assert.True(ConfigDefaults.IsDefault(PflagKeys.ScanNetNew, false));
            Assert.False(ConfigDefaults.IsDefault(PflagKeys.ScanNetNew, true));

            Assert.True(ConfigDefaults.IsDefault(PflagKeys.SeverityFilterCritical, true));
            Assert.True(ConfigDefaults.IsDefault(PflagKeys.SeverityFilterHigh, true));
            Assert.True(ConfigDefaults.IsDefault(PflagKeys.SeverityFilterMedium, true));
            Assert.True(ConfigDefaults.IsDefault(PflagKeys.SeverityFilterLow, true));

            Assert.True(ConfigDefaults.IsDefault(PflagKeys.IssueViewOpenIssues, true));
            Assert.False(ConfigDefaults.IsDefault(PflagKeys.IssueViewOpenIssues, false));

            Assert.True(ConfigDefaults.IsDefault(PflagKeys.IssueViewIgnoredIssues, false));
            Assert.False(ConfigDefaults.IsDefault(PflagKeys.IssueViewIgnoredIssues, true));

            Assert.True(ConfigDefaults.IsDefault(PflagKeys.AutomaticDownload, true));
            Assert.False(ConfigDefaults.IsDefault(PflagKeys.AutomaticDownload, false));

            Assert.True(ConfigDefaults.IsDefault(PflagKeys.CliReleaseChannel, SnykCliDownloader.DefaultReleaseChannel));
            Assert.False(ConfigDefaults.IsDefault(PflagKeys.CliReleaseChannel, "rc-preview"));

            // Unknown key: treat as "no default known" → not-default regardless of value.
            Assert.False(ConfigDefaults.IsDefault("unknown_key", "anything"));
        }

        // UNIT-012 (finding 2): BinaryBaseUrl default must match SnykCliDownloader.DefaultBaseDownloadUrl
        // ("https://downloads.snyk.io", no /cli suffix). A user who never changed this URL must NOT be
        // marked as overriding it.
        [Fact]
        public void IsDefault_BinaryBaseUrl_MatchesCanonicalDownloaderConstant()
        {
            // The canonical constant — must match exactly (no trailing /cli).
            Assert.True(ConfigDefaults.IsDefault(PflagKeys.BinaryBaseUrl, SnykCliDownloader.DefaultBaseDownloadUrl),
                $"IsDefault should return true for '{SnykCliDownloader.DefaultBaseDownloadUrl}' (SnykCliDownloader.DefaultBaseDownloadUrl)");

            // A non-default value must return false.
            Assert.False(ConfigDefaults.IsDefault(PflagKeys.BinaryBaseUrl, "https://example.com"),
                "IsDefault should return false for a non-default URL");
        }

        // UNIT-013 (finding 1): AuthenticationMethod default is default(AuthenticationType).ToString().ToLowerInvariant().
        // A user who never changed auth method must NOT be marked as overriding it.
        [Fact]
        public void IsDefault_AuthenticationMethod_MatchesEnumDefault()
        {
            var defaultValue = default(Authentication.AuthenticationType).ToString().ToLowerInvariant();

            Assert.True(ConfigDefaults.IsDefault(PflagKeys.AuthenticationMethod, defaultValue),
                $"IsDefault should return true for default auth method '{defaultValue}'");

            // A non-default auth method.
            Assert.False(ConfigDefaults.IsDefault(PflagKeys.AuthenticationMethod, "pat"),
                "IsDefault should return false for a non-default auth method");
        }

        // UNIT-014 (finding 3): RiskScoreThreshold null == default.
        [Fact]
        public void IsDefault_RiskScoreThreshold_NullIsDefault()
        {
            Assert.True(ConfigDefaults.IsDefault(PflagKeys.RiskScoreThreshold, null),
                "null RiskScoreThreshold should be the default");
            Assert.False(ConfigDefaults.IsDefault(PflagKeys.RiskScoreThreshold, 70),
                "Non-null RiskScoreThreshold should not be the default");
        }

        // R3-4: Pin the enum-default assumption. ConfigDefaults bakes the authentication_method
        // default from default(AuthenticationType).ToString().ToLowerInvariant(). If the enum is
        // ever reordered so OAuth is no longer the zero value, this test fails immediately.
        [Fact]
        public void AuthenticationType_OAuthIsZeroValue_MatchesDefaultEnumAssumption()
        {
            // OAuth must be the zero value so default(AuthenticationType) == OAuth.
            Assert.Equal(
                Authentication.AuthenticationType.OAuth.ToString().ToLowerInvariant(),
                default(Authentication.AuthenticationType).ToString().ToLowerInvariant(),
                "AuthenticationType.OAuth must be the enum zero value. If this fails, ConfigDefaults " +
                "authentication_method default is wrong and must be corrected to match the new zero value.");
        }
    }
}
