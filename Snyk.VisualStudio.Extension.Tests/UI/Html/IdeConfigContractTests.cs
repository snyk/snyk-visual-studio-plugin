using System.Linq;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    public class IdeConfigContractTests
    {
        // The full set of top-level keys the LS settings form posts to __saveIdeConfig__, taken from
        // snyk-ls' authoritative snapshot (js-tests/snapshots/form-payload.json). If snyk-ls adds a
        // key here (and this fixture is updated to match), or an IdeConfigData [JsonProperty] is
        // renamed, this test fails — surfacing the drift at build time instead of silently dropping
        // the setting at runtime.
        private const string AuthoritativePayload = @"{
            ""api_endpoint"": ""https://api.snyk.io"",
            ""authentication_method"": ""oauth"",
            ""automatic_download"": false,
            ""binary_base_url"": """",
            ""cli_path"": """",
            ""cli_release_channel"": ""stable"",
            ""folderConfigs"": [],
            ""issue_view_ignored_issues"": true,
            ""issue_view_open_issues"": true,
            ""organization"": ""org-uuid"",
            ""proxy_insecure"": false,
            ""risk_score_threshold"": null,
            ""scan_automatic"": true,
            ""scan_net_new"": false,
            ""severity_filter_critical"": true,
            ""severity_filter_high"": false,
            ""severity_filter_low"": true,
            ""severity_filter_medium"": true,
            ""snyk_code_enabled"": true,
            ""snyk_iac_enabled"": false,
            ""snyk_oss_enabled"": true,
            ""token"": """",
            ""trusted_folders"": []
        }";

        [Fact]
        public void Analyze_AuthoritativePayload_HasNoUnmappedKeys()
        {
            var result = IdeConfigContract.Analyze(AuthoritativePayload);

            Assert.Empty(result.UnmappedKeys);
            Assert.False(result.AllUnmapped);
        }

        [Fact]
        public void Analyze_FlagsOnlyTheUnrecognisedKey_OnPartialDrift()
        {
            // A new field added upstream that this build does not yet bind.
            var json = @"{ ""snyk_oss_enabled"": true, ""brand_new_setting"": 42 }";

            var result = IdeConfigContract.Analyze(json);

            Assert.Equal(new[] { "brand_new_setting" }, result.UnmappedKeys.ToArray());
            Assert.False(result.AllUnmapped); // partial — the save should still apply the known key
        }

        [Fact]
        public void Analyze_AllKeysUnrecognised_ReportsAllUnmapped()
        {
            // Wholesale rename: nothing maps, so applying would no-op while reporting success.
            var json = @"{ ""renamed_one"": true, ""renamed_two"": false }";

            var result = IdeConfigContract.Analyze(json);

            Assert.True(result.AllUnmapped);
            Assert.Equal(2, result.UnmappedKeys.Count);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("")]
        [InlineData("[1,2,3]")]
        [InlineData("not json")]
        [InlineData("{}")]
        public void Analyze_NonObjectOrEmptyPayload_IsNeverAllUnmapped(string json)
        {
            var result = IdeConfigContract.Analyze(json);

            Assert.Empty(result.UnmappedKeys);
            Assert.False(result.AllUnmapped);
        }
    }
}
