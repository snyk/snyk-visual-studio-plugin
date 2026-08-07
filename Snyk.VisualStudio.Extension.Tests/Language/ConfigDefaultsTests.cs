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

            // Snyk Code defaults to false to match the Language Server, which does not
            // default-enable it. An enabled-Code value is therefore a genuine override.
            Assert.True(ConfigDefaults.IsDefault(PflagKeys.SnykCodeEnabled, false));
            Assert.False(ConfigDefaults.IsDefault(PflagKeys.SnykCodeEnabled, true));

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

        // PR-REV-3-1: every tracked boolean default, PINNED to a literal expected value and checked
        // against the SnykSettings field that must agree with it.
        //
        // Pinned rather than derived, deliberately. The four product keys now reference
        // SnykSettings.Default* consts from ConfigDefaults, which is what stops those two sites
        // drifting apart — but it also makes a bare "map == SnykSettings" comparison tautological for
        // them: it compares a value to itself and can never fail. The literal below is therefore the
        // only thing that fails when a default changes, which keeps such a change a deliberate,
        // reviewable edit instead of a silent one.
        //
        // These values must also equal the Language Server's registered defaults (snyk-ls
        // internal/types/register_configurations.go). The LS owns the authoritative default; the IDE
        // holds a hard-coded copy because the override seed runs before the first LSP handshake and
        // must still work when the language server never starts, so no build-time check can enforce
        // the agreement — it is enforced by this test plus review. A copy that disagrees is exactly
        // how Snyk Code came to be silently disabled on upgrade: both VS sites agreed on `true` while
        // the LS said `false`, so the old derived-comparison guard passed while the value was wrong.
        [Fact]
        public void ConfigDefaults_BooleanValues_ArePinnedAndMatchSnykSettings()
        {
            var defaults = new Snyk.VisualStudio.Extension.Settings.SnykSettings();

            // (pflag key, pinned expected default, the SnykSettings value that must agree, its name)
            var cases = new (string Key, bool Expected, bool FromSettings, string Field)[]
            {
                // Product enablement. Code is false to match the LS, which does not default-enable
                // it; Secrets likewise. OSS and IaC the LS does default-enable.
                (PflagKeys.SnykOssEnabled,     true,  defaults.OssEnabled,              nameof(defaults.OssEnabled)),
                (PflagKeys.SnykCodeEnabled,    false, defaults.SnykCodeSecurityEnabled, nameof(defaults.SnykCodeSecurityEnabled)),
                (PflagKeys.SnykIacEnabled,     true,  defaults.IacEnabled,              nameof(defaults.IacEnabled)),
                (PflagKeys.SnykSecretsEnabled, false, defaults.SecretsEnabled,          nameof(defaults.SecretsEnabled)),

                // Scan
                (PflagKeys.ScanAutomatic, true,  defaults.AutoScan,             nameof(defaults.AutoScan)),
                (PflagKeys.ScanNetNew,    false, defaults.EnableDeltaFindings,  nameof(defaults.EnableDeltaFindings)),

                // Severity filters
                (PflagKeys.SeverityFilterCritical, true, defaults.FilterCritical, nameof(defaults.FilterCritical)),
                (PflagKeys.SeverityFilterHigh,     true, defaults.FilterHigh,     nameof(defaults.FilterHigh)),
                (PflagKeys.SeverityFilterMedium,   true, defaults.FilterMedium,   nameof(defaults.FilterMedium)),
                (PflagKeys.SeverityFilterLow,      true, defaults.FilterLow,      nameof(defaults.FilterLow)),

                // Issue view
                (PflagKeys.IssueViewOpenIssues,    true,  defaults.OpenIssuesEnabled,    nameof(defaults.OpenIssuesEnabled)),
                (PflagKeys.IssueViewIgnoredIssues, false, defaults.IgnoredIssuesEnabled, nameof(defaults.IgnoredIssuesEnabled)),

                // CLI / binary booleans
                (PflagKeys.AutomaticDownload, true,  defaults.BinariesAutoUpdateEnabled, nameof(defaults.BinariesAutoUpdateEnabled)),
                (PflagKeys.ProxyInsecure,     false, defaults.IgnoreUnknownCa,           nameof(defaults.IgnoreUnknownCa)),
            };

            foreach (var c in cases)
            {
                Assert.True((bool)ConfigDefaults.GetDefaultForTest(c.Key) == c.Expected,
                    $"ConfigDefaults[{c.Key}] must be {c.Expected}. If this default is changing " +
                    "deliberately, update the pinned value here and confirm it still equals the " +
                    "Language Server's registered default in snyk-ls register_configurations.go — " +
                    "a default that disagrees with the LS makes the override seed classify user " +
                    "preferences against the wrong baseline.");

                Assert.True(c.FromSettings == c.Expected,
                    $"SnykSettings.{c.Field} must be {c.Expected} so the persistence default and " +
                    $"ConfigDefaults[{c.Key}] agree.");
            }
        }
    }
}
