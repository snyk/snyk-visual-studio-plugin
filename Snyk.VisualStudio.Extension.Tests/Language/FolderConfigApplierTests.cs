using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Utils;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    public class FolderConfigApplierTests
    {
        [Fact]
        public void Deserialize_LegacyTypedFolderConfigJson_DoesNotThrow_AndKeepsFolderPath()
        {
            // Persistence migration: settings.json written by a pre-opaque-map build stored typed
            // folder props (baseBranch, preferredOrg, ...). After the refactor those are unknown JSON
            // members; deserialization must tolerate them (no MissingMemberHandling.Error) and keep
            // FolderPath. The user-set keys live on the LS, which repopulates Settings on its next
            // $/snyk.configuration push — so a near-empty map on load is fine.
            const string legacyJson = @"{
                ""folderPath"": ""/repo"",
                ""baseBranch"": ""main"",
                ""referenceFolderPath"": ""C:\\refs\\main"",
                ""preferredOrg"": ""my-org"",
                ""orgSetByUser"": true,
                ""snykOssEnabled"": true,
                ""riskScoreThreshold"": 500
            }";

            var fc = Json.Deserialize<FolderConfig>(legacyJson);

            Assert.NotNull(fc);
            Assert.Equal("/repo", fc.FolderPath);
            Assert.NotNull(fc.Settings); // default-initialized, not null
        }

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
            var existing = new List<FolderConfig> { NewFolderConfig("/repo", PflagKeys.PreferredOrg, "old-org") };
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
            Assert.Equal("new-org", result[0].GetString(PflagKeys.PreferredOrg));
        }

        [Fact]
        public void Apply_PathMatchIsCaseInsensitive()
        {
            var existing = new List<FolderConfig> { NewFolderConfig("/Repo", PflagKeys.PreferredOrg, "old-org") };
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
            Assert.Equal("new-org", result[0].GetString(PflagKeys.PreferredOrg));
        }

        [Fact]
        public void Apply_PrunesStaleEntries_NotInIncoming()
        {
            // LS is source of truth: an existing entry for a path the LS no longer sends
            // (e.g. a previously-opened solution in the same VS session) must be dropped, so a
            // later FirstOrDefault can't pick it up by mistake.
            var existing = new List<FolderConfig> { NewFolderConfig("/old-solution", PflagKeys.PreferredOrg, "stale-org") };
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
        public void Apply_ReferenceFolder_ComesFromLsPayload()
        {
            // reference_folder now lives in the LS-sent settings map (the old extension-local
            // carry-over was removed). A config push must surface whatever the LS sent verbatim.
            var existing = new List<FolderConfig> { new FolderConfig { FolderPath = "/repo" } };
            var incoming = new List<LspFolderConfig>
            {
                new LspFolderConfig
                {
                    FolderPath = "/repo",
                    Settings = new Dictionary<string, ConfigSetting>
                    {
                        [PflagKeys.ReferenceFolder] = ConfigSetting.Of(@"C:\refs\main"),
                        [PflagKeys.PreferredOrg] = ConfigSetting.Of("new-org")
                    }
                }
            };

            var result = FolderConfigApplier.Apply(existing, incoming);

            Assert.Single(result);
            Assert.Equal(@"C:\refs\main", result[0].GetString(PflagKeys.ReferenceFolder));
            Assert.Equal("new-org", result[0].GetString(PflagKeys.PreferredOrg));
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

            Assert.Equal("my-org", fc.GetString(PflagKeys.PreferredOrg));
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

            Assert.Equal("auto-org", fc.GetString(PflagKeys.AutoDeterminedOrg));
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

            Assert.True(Convert.ToBoolean(fc.Settings[PflagKeys.OrgSetByUser].Value));
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

            Assert.Equal("main", fc.GetString(PflagKeys.BaseBranch));
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

            Assert.Equal(branches, fc.GetStringList(PflagKeys.LocalBranches));
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

            Assert.Equal(new List<string> { "-d", "--flag" }, fc.GetStringList(PflagKeys.AdditionalParameters));
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

            Assert.Equal("KEY=VALUE", fc.GetString(PflagKeys.AdditionalEnvironment));
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

            Assert.Null(fc.GetString(PflagKeys.PreferredOrg));
        }

        [Fact]
        public void ToFolderConfig_CopiesSettingsVerbatim_WithoutParsing()
        {
            // The applier no longer parses inbound values per-key — it copies the settings map
            // verbatim. Odd/mistyped values that the old switch would have skipped must now survive
            // untouched in the map, and the copy must not throw on them.
            var src = new LspFolderConfig
            {
                FolderPath = "/repo",
                Settings = new Dictionary<string, ConfigSetting>
                {
                    // A scalar where a string-list would be expected — copied raw, no parse.
                    [PflagKeys.AdditionalParameters] = new ConfigSetting { Value = JToken.Parse("42") },
                    // A well-formed key alongside it.
                    [PflagKeys.PreferredOrg] = ConfigSetting.Of("my-org"),
                }
            };

            var fc = FolderConfigApplier.ToFolderConfig(src);

            // The raw value survives verbatim in the map.
            Assert.Equal(42, ((JToken)fc.Settings[PflagKeys.AdditionalParameters].Value).Value<int>());
            Assert.Equal("my-org", fc.GetString(PflagKeys.PreferredOrg));
        }

        [Fact]
        public void ToFolderConfig_ReturnsEmptyConfig_WhenSettingsIsNull()
        {
            var src = new LspFolderConfig { FolderPath = "/repo", Settings = null };

            var fc = FolderConfigApplier.ToFolderConfig(src);

            Assert.Equal("/repo", fc.FolderPath);
            Assert.Empty(fc.Settings);
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

            Assert.Equal(branches, result[0].GetStringList(PflagKeys.LocalBranches));
        }

        private static FolderConfig NewFolderConfig(string folderPath, string key, string value)
        {
            var fc = new FolderConfig { FolderPath = folderPath };
            fc.SetString(key, value);
            return fc;
        }
    }
}
