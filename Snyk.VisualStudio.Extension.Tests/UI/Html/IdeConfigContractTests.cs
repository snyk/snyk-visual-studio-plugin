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
            ""additional_environment"": """",
            ""additional_parameters"": """",
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

        // A folder entry with the per-folder keys the form posts inside folderConfigs[], from the same
        // snyk-ls snapshot. Pins FolderConfigData's bindings the same way the global block pins
        // IdeConfigData's.
        private const string AuthoritativeFolderEntry = @"{
            ""additional_environment"": """",
            ""additional_parameters"": [],
            ""folderPath"": ""/workspace/project"",
            ""issue_view_ignored_issues"": false,
            ""issue_view_open_issues"": true,
            ""org_set_by_user"": true,
            ""preferred_org"": ""org-uuid"",
            ""risk_score_threshold"": 500,
            ""scan_automatic"": true,
            ""scan_command_config"": { ""Snyk Code"": { ""preScanCommand"": """", ""preScanOnlyReferenceFolder"": false, ""postScanCommand"": """", ""postScanOnlyReferenceFolder"": false } },
            ""scan_net_new"": false,
            ""severity_filter_critical"": true,
            ""severity_filter_high"": true,
            ""severity_filter_low"": false,
            ""severity_filter_medium"": false,
            ""snyk_code_enabled"": true,
            ""snyk_iac_enabled"": false,
            ""snyk_oss_enabled"": true
        }";

        [Fact]
        public void Analyze_AuthoritativePayload_HasNoUnmappedKeys()
        {
            var result = IdeConfigContract.Analyze(AuthoritativePayload);

            Assert.Empty(result.UnmappedKeys);
            Assert.Empty(result.UnmappedFolderKeys);
            Assert.False(result.AllUnmapped);
            Assert.False(result.HasUnmappedKeys);
        }

        [Fact]
        public void Analyze_AuthoritativeFolderEntry_HasNoUnmappedFolderKeys()
        {
            var json = @"{ ""folderConfigs"": [" + AuthoritativeFolderEntry + "] }";

            var result = IdeConfigContract.Analyze(json);

            Assert.Empty(result.UnmappedKeys);
            Assert.Empty(result.UnmappedFolderKeys);
        }

        [Fact]
        public void Analyze_FlagsUnrecognisedPerFolderKey()
        {
            // A new per-folder field added upstream that FolderConfigData does not yet bind.
            var json = @"{
                ""folderConfigs"": [
                    { ""folderPath"": ""/a"", ""brand_new_folder_setting"": 1 },
                    { ""folderPath"": ""/b"", ""brand_new_folder_setting"": 2 }
                ]
            }";

            var result = IdeConfigContract.Analyze(json);

            Assert.Empty(result.UnmappedKeys); // top-level "folderConfigs" is bound
            Assert.Equal(new[] { "brand_new_folder_setting" }, result.UnmappedFolderKeys.ToArray()); // deduped across entries
            Assert.True(result.HasUnmappedKeys);
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
            Assert.Empty(result.UnmappedFolderKeys);
            Assert.False(result.AllUnmapped);
            Assert.False(result.HasUnmappedKeys);
        }
    }
}
