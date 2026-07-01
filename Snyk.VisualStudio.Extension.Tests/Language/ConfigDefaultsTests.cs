// ABOUTME: Unit tests for ConfigDefaults covering UNIT-007, UNIT-012 from the IDE-2152 test plan.
using Snyk.VisualStudio.Extension.Authentication;
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
            var defaultValue = default(AuthenticationType).ToString().ToLowerInvariant();

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
                AuthenticationType.OAuth.ToString().ToLowerInvariant(),
                default(AuthenticationType).ToString().ToLowerInvariant());
        }

        // PR-REV-2b-1: IsDefault for AdditionalParameters with an empty string must return true.
        // The form sends AdditionalParameters as a space-joined text string (from a text input),
        // not as a collection. An empty string is the form's representation of "no parameters",
        // which must equal the default (empty list → space-joined → empty string).
        [Fact]
        public void IsDefault_AdditionalParameters_EmptyStringIsDefault()
        {
            Assert.True(ConfigDefaults.IsDefault(PflagKeys.AdditionalParameters, string.Empty),
                "An empty string for AdditionalParameters must be the default — " +
                "the form sends a space-joined string; empty string == empty list (the SnykSettings default)");
        }

        // PR-REV-2b-2: IsDefault for AdditionalParameters with a whitespace-only string must also
        // return true. A form field with only spaces should also be treated as "no parameters set".
        [Fact]
        public void IsDefault_AdditionalParameters_WhitespaceOnlyStringIsDefault()
        {
            Assert.True(ConfigDefaults.IsDefault(PflagKeys.AdditionalParameters, "   "),
                "A whitespace-only string for AdditionalParameters must be the default — " +
                "it is semantically equivalent to the empty list (splits to no tokens)");
        }

        // PR-REV-2b-3: IsDefault for AdditionalParameters with a non-empty string must return false.
        [Fact]
        public void IsDefault_AdditionalParameters_NonEmptyStringIsNotDefault()
        {
            Assert.False(ConfigDefaults.IsDefault(PflagKeys.AdditionalParameters, "--debug"),
                "A non-empty AdditionalParameters string must NOT be the default");
        }

        // PR-REV-3-1: ConfigDefaults boolean values for product-enablement must match
        // the canonical SnykSettings field initializers. This acts as a drift guard:
        // if SnykSettings changes a default, the test (which reads the SnykSettings field
        // via new SnykSettings()) will catch the mismatch.
        // Note: SnykSettings uses inline field initializers (not named constants), so we compare
        // by constructing a default instance and reading each field.
        [Fact]
        public void ConfigDefaults_BooleanValues_MatchSnykSettingsFieldInitializers()
        {
            var defaults = new Snyk.VisualStudio.Extension.Settings.SnykSettings();

            // Product enablement
            Assert.True((bool)ConfigDefaults.GetDefaultForTest(PflagKeys.SnykOssEnabled) == defaults.OssEnabled,
                "SnykOssEnabled default must match SnykSettings.OssEnabled initializer");
            Assert.True((bool)ConfigDefaults.GetDefaultForTest(PflagKeys.SnykCodeEnabled) == defaults.SnykCodeSecurityEnabled,
                "SnykCodeEnabled default must match SnykSettings.SnykCodeSecurityEnabled initializer");
            Assert.True((bool)ConfigDefaults.GetDefaultForTest(PflagKeys.SnykIacEnabled) == defaults.IacEnabled,
                "SnykIacEnabled default must match SnykSettings.IacEnabled initializer");
            Assert.True((bool)ConfigDefaults.GetDefaultForTest(PflagKeys.SnykSecretsEnabled) == defaults.SecretsEnabled,
                "SnykSecretsEnabled default must match SnykSettings.SecretsEnabled initializer");

            // Scan
            Assert.True((bool)ConfigDefaults.GetDefaultForTest(PflagKeys.ScanAutomatic) == defaults.AutoScan,
                "ScanAutomatic default must match SnykSettings.AutoScan initializer");
            Assert.True((bool)ConfigDefaults.GetDefaultForTest(PflagKeys.ScanNetNew) == defaults.EnableDeltaFindings,
                "ScanNetNew default must match SnykSettings.EnableDeltaFindings initializer");

            // Severity filters
            Assert.True((bool)ConfigDefaults.GetDefaultForTest(PflagKeys.SeverityFilterCritical) == defaults.FilterCritical,
                "SeverityFilterCritical default must match SnykSettings.FilterCritical initializer");
            Assert.True((bool)ConfigDefaults.GetDefaultForTest(PflagKeys.SeverityFilterHigh) == defaults.FilterHigh,
                "SeverityFilterHigh default must match SnykSettings.FilterHigh initializer");
            Assert.True((bool)ConfigDefaults.GetDefaultForTest(PflagKeys.SeverityFilterMedium) == defaults.FilterMedium,
                "SeverityFilterMedium default must match SnykSettings.FilterMedium initializer");
            Assert.True((bool)ConfigDefaults.GetDefaultForTest(PflagKeys.SeverityFilterLow) == defaults.FilterLow,
                "SeverityFilterLow default must match SnykSettings.FilterLow initializer");

            // Issue view
            Assert.True((bool)ConfigDefaults.GetDefaultForTest(PflagKeys.IssueViewOpenIssues) == defaults.OpenIssuesEnabled,
                "IssueViewOpenIssues default must match SnykSettings.OpenIssuesEnabled initializer");
            Assert.True((bool)ConfigDefaults.GetDefaultForTest(PflagKeys.IssueViewIgnoredIssues) == defaults.IgnoredIssuesEnabled,
                "IssueViewIgnoredIssues default must match SnykSettings.IgnoredIssuesEnabled initializer");

            // CLI / binary booleans
            Assert.True((bool)ConfigDefaults.GetDefaultForTest(PflagKeys.AutomaticDownload) == defaults.BinariesAutoUpdateEnabled,
                "AutomaticDownload default must match SnykSettings.BinariesAutoUpdateEnabled initializer");
            Assert.True((bool)ConfigDefaults.GetDefaultForTest(PflagKeys.ProxyInsecure) == defaults.IgnoreUnknownCa,
                "ProxyInsecure default must match SnykSettings.IgnoreUnknownCa initializer");
        }
    }
}
