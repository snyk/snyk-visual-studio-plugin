using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Snyk.VisualStudio.Extension.Language;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    public class FolderConfigApplierTests
    {
        [Fact]
        public void Apply_AddsNewEntry_WhenPathNotInExisting()
        {
            var existing = new List<FolderConfig>();
            var incoming = new List<LspFolderConfig>
            {
                new LspFolderConfig { FolderPath = "/repo", Settings = new Dictionary<string, ConfigSetting>() }
            };

            var result = FolderConfigApplier.Apply(existing, incoming);

            Assert.Single(result);
            Assert.Equal("/repo", result[0].FolderPath);
        }

        [Fact]
        public void Apply_ReplacesExistingEntry_WhenPathMatches()
        {
            var existing = new List<FolderConfig>
            {
                new FolderConfig { FolderPath = "/repo", PreferredOrg = "old-org" }
            };
            var incoming = new List<LspFolderConfig>
            {
                new LspFolderConfig
                {
                    FolderPath = "/repo",
                    Settings = new Dictionary<string, ConfigSetting>
                    {
                        [PflagKeys.PreferredOrg] = ConfigSetting.Of("new-org")
                    }
                }
            };

            var result = FolderConfigApplier.Apply(existing, incoming);

            Assert.Single(result);
            Assert.Equal("new-org", result[0].PreferredOrg);
        }

        [Fact]
        public void Apply_PathMatchIsCaseInsensitive()
        {
            var existing = new List<FolderConfig>
            {
                new FolderConfig { FolderPath = "/Repo", PreferredOrg = "old-org" }
            };
            var incoming = new List<LspFolderConfig>
            {
                new LspFolderConfig
                {
                    FolderPath = "/repo",
                    Settings = new Dictionary<string, ConfigSetting>
                    {
                        [PflagKeys.PreferredOrg] = ConfigSetting.Of("new-org")
                    }
                }
            };

            var result = FolderConfigApplier.Apply(existing, incoming);

            Assert.Single(result);
            Assert.Equal("new-org", result[0].PreferredOrg);
        }

        [Fact]
        public void Apply_PrunesStaleEntries_NotInIncoming()
        {
            // LS is source of truth: an existing entry for a path the LS no longer sends
            // (e.g. a previously-opened solution in the same VS session) must be dropped, so a
            // later FirstOrDefault can't pick it up by mistake.
            var existing = new List<FolderConfig>
            {
                new FolderConfig { FolderPath = "/old-solution", PreferredOrg = "stale-org" }
            };
            var incoming = new List<LspFolderConfig>
            {
                new LspFolderConfig { FolderPath = "/current-solution", Settings = new Dictionary<string, ConfigSetting>() }
            };

            var result = FolderConfigApplier.Apply(existing, incoming);

            Assert.Single(result);
            Assert.Equal("/current-solution", result[0].FolderPath);
            Assert.DoesNotContain(result, fc => fc.FolderPath == "/old-solution");
        }

        [Fact]
        public void Apply_PreservesLocalReferenceFolderPath_AcrossSync()
        {
            // ReferenceFolderPath is extension-local (set via the Branch Selector, never sent by
            // the LS). A config push rebuilds the entry from the LS payload, so it must carry the
            // prior local value over rather than wiping it.
            var existing = new List<FolderConfig>
            {
                new FolderConfig { FolderPath = "/repo", ReferenceFolderPath = @"C:\refs\main" }
            };
            var incoming = new List<LspFolderConfig>
            {
                new LspFolderConfig
                {
                    FolderPath = "/repo",
                    Settings = new Dictionary<string, ConfigSetting>
                    {
                        [PflagKeys.PreferredOrg] = ConfigSetting.Of("new-org")
                    }
                }
            };

            var result = FolderConfigApplier.Apply(existing, incoming);

            Assert.Single(result);
            Assert.Equal(@"C:\refs\main", result[0].ReferenceFolderPath);
            Assert.Equal("new-org", result[0].PreferredOrg);
        }

        [Fact]
        public void Apply_ReturnsExisting_WhenIncomingIsEmpty()
        {
            var existing = new List<FolderConfig>
            {
                new FolderConfig { FolderPath = "/repo" }
            };

            var result = FolderConfigApplier.Apply(existing, new List<LspFolderConfig>());

            Assert.Single(result);
            Assert.Equal("/repo", result[0].FolderPath);
        }

        [Fact]
        public void Apply_ReturnsEmpty_WhenBothNull()
        {
            var result = FolderConfigApplier.Apply(null, null);

            Assert.Empty(result);
        }

        [Fact]
        public void Apply_SkipsEntries_WithNullOrEmptyFolderPath()
        {
            var incoming = new List<LspFolderConfig>
            {
                null,
                new LspFolderConfig { FolderPath = null },
                new LspFolderConfig { FolderPath = "" },
                new LspFolderConfig { FolderPath = "/valid" }
            };

            var result = FolderConfigApplier.Apply(new List<FolderConfig>(), incoming);

            Assert.Single(result);
            Assert.Equal("/valid", result[0].FolderPath);
        }

        [Fact]
        public void ToFolderConfig_MapsPreferredOrg()
        {
            var src = new LspFolderConfig
            {
                FolderPath = "/repo",
                Settings = new Dictionary<string, ConfigSetting>
                {
                    [PflagKeys.PreferredOrg] = ConfigSetting.Of("my-org")
                }
            };

            var fc = FolderConfigApplier.ToFolderConfig(src);

            Assert.Equal("my-org", fc.PreferredOrg);
        }

        [Fact]
        public void ToFolderConfig_MapsAutoDeterminedOrg()
        {
            var src = new LspFolderConfig
            {
                FolderPath = "/repo",
                Settings = new Dictionary<string, ConfigSetting>
                {
                    [PflagKeys.AutoDeterminedOrg] = ConfigSetting.Of("auto-org")
                }
            };

            var fc = FolderConfigApplier.ToFolderConfig(src);

            Assert.Equal("auto-org", fc.AutoDeterminedOrg);
        }

        [Fact]
        public void ToFolderConfig_MapsOrgSetByUser()
        {
            var src = new LspFolderConfig
            {
                FolderPath = "/repo",
                Settings = new Dictionary<string, ConfigSetting>
                {
                    [PflagKeys.OrgSetByUser] = ConfigSetting.Of(true)
                }
            };

            var fc = FolderConfigApplier.ToFolderConfig(src);

            Assert.True(fc.OrgSetByUser);
        }

        [Fact]
        public void ToFolderConfig_MapsBaseBranch()
        {
            var src = new LspFolderConfig
            {
                FolderPath = "/repo",
                Settings = new Dictionary<string, ConfigSetting>
                {
                    [PflagKeys.BaseBranch] = ConfigSetting.Of("main")
                }
            };

            var fc = FolderConfigApplier.ToFolderConfig(src);

            Assert.Equal("main", fc.BaseBranch);
        }

        [Fact]
        public void ToFolderConfig_MapsLocalBranches()
        {
            var branches = new List<string> { "main", "dev" };
            var src = new LspFolderConfig
            {
                FolderPath = "/repo",
                Settings = new Dictionary<string, ConfigSetting>
                {
                    [PflagKeys.LocalBranches] = ConfigSetting.Of(branches)
                }
            };

            var fc = FolderConfigApplier.ToFolderConfig(src);

            Assert.Equal(branches, fc.LocalBranches);
        }

        [Fact]
        public void ToFolderConfig_MapsAdditionalParameters()
        {
            var src = new LspFolderConfig
            {
                FolderPath = "/repo",
                Settings = new Dictionary<string, ConfigSetting>
                {
                    [PflagKeys.AdditionalParameters] = ConfigSetting.Of(new List<string> { "-d", "--flag" })
                }
            };

            var fc = FolderConfigApplier.ToFolderConfig(src);

            Assert.Equal(new List<string> { "-d", "--flag" }, fc.AdditionalParameters);
        }

        [Fact]
        public void ToFolderConfig_MapsAdditionalEnvironment()
        {
            var src = new LspFolderConfig
            {
                FolderPath = "/repo",
                Settings = new Dictionary<string, ConfigSetting>
                {
                    [PflagKeys.AdditionalEnvironment] = ConfigSetting.Of("KEY=VALUE")
                }
            };

            var fc = FolderConfigApplier.ToFolderConfig(src);

            Assert.Equal("KEY=VALUE", fc.AdditionalEnv);
        }

        [Fact]
        public void ToFolderConfig_SkipsNullValueEntry()
        {
            var src = new LspFolderConfig
            {
                FolderPath = "/repo",
                Settings = new Dictionary<string, ConfigSetting>
                {
                    [PflagKeys.PreferredOrg] = new ConfigSetting { Value = null }
                }
            };

            var fc = FolderConfigApplier.ToFolderConfig(src);

            Assert.Null(fc.PreferredOrg);
        }

        [Fact]
        public void ToFolderConfig_SkipsBadValueWithoutThrowing()
        {
            var src = new LspFolderConfig
            {
                FolderPath = "/repo",
                Settings = new Dictionary<string, ConfigSetting>
                {
                    // org_set_by_user expects bool; send a non-parseable object
                    [PflagKeys.OrgSetByUser] = new ConfigSetting { Value = JToken.Parse(@"{ ""not"": ""a bool"" }") }
                }
            };

            var fc = FolderConfigApplier.ToFolderConfig(src);

            // Default false — key skipped, no exception
            Assert.False(fc.OrgSetByUser);
        }

        [Fact]
        public void ToFolderConfig_SkipsBadCollectionValuesWithoutThrowing()
        {
            // The list/dict-typed keys go through ToObject<T>, which throws on a mistyped JToken.
            // A malformed LS payload must be skipped per-key, not abort the whole conversion.
            var src = new LspFolderConfig
            {
                FolderPath = "/repo",
                Settings = new Dictionary<string, ConfigSetting>
                {
                    // AdditionalParameters expects an array of strings — send a scalar.
                    [PflagKeys.AdditionalParameters] = new ConfigSetting { Value = JToken.Parse("42") },
                    // LocalBranches expects an array — send a string scalar.
                    [PflagKeys.LocalBranches] = new ConfigSetting { Value = JToken.Parse(@"""main""") },
                    // ScanCommandConfig expects an object map — send an array.
                    [PflagKeys.ScanCommandConfig] = new ConfigSetting { Value = JToken.Parse(@"[1, 2, 3]") },
                    // A well-formed key alongside the bad ones must still be applied.
                    [PflagKeys.PreferredOrg] = ConfigSetting.Of("my-org"),
                }
            };

            var fc = FolderConfigApplier.ToFolderConfig(src);

            Assert.Null(fc.AdditionalParameters);
            Assert.Null(fc.LocalBranches);
            Assert.Null(fc.ScanCommandConfig);
            Assert.Equal("my-org", fc.PreferredOrg);
        }

        [Fact]
        public void ToFolderConfig_ReturnsEmptyConfig_WhenSettingsIsNull()
        {
            var src = new LspFolderConfig { FolderPath = "/repo", Settings = null };

            var fc = FolderConfigApplier.ToFolderConfig(src);

            Assert.Equal("/repo", fc.FolderPath);
            Assert.Null(fc.PreferredOrg);
        }

        [Fact]
        public void LocalBranches_RoundTrip_ThroughApply()
        {
            var branches = new List<string> { "main", "feature/x" };
            var incoming = new List<LspFolderConfig>
            {
                new LspFolderConfig
                {
                    FolderPath = "/repo",
                    Settings = new Dictionary<string, ConfigSetting>
                    {
                        [PflagKeys.LocalBranches] = ConfigSetting.Of(branches)
                    }
                }
            };

            var result = FolderConfigApplier.Apply(new List<FolderConfig>(), incoming);

            Assert.Equal(branches, result[0].LocalBranches);
        }
    }
}
