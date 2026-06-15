using System.Collections.Generic;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Settings
{
    public class LegacySolutionSettingsMigratorTests
    {
        [Fact]
        public void ComputeFolderHash_MatchesLegacyKeyScheme()
        {
            // Must reproduce the original SnykOptionsManager.GetSolutionPathHashAsync key exactly,
            // otherwise a path -> hash lookup can never resolve a legacy entry.
            const string folder = @"C:\Repos\MySolution";

            Assert.Equal(folder.ToLower().GetHashCode(), LegacySolutionSettingsMigrator.ComputeFolderHash(folder));
        }

        [Fact]
        public void ToFolderConfig_MapsSupportedFields_AndSplitsParameters()
        {
            var legacy = new LegacySolutionSettings
            {
                AdditionalOptions = "-d --all-projects",
                AdditionalEnv = "KEY=VALUE",
                PreferredOrg = "my-org",
                AutoDeterminedOrg = "auto-org",
                OrgSetByUser = true,
                IsAllProjectsScanEnabled = true, // intentionally not migrated
            };

            var fc = LegacySolutionSettingsMigrator.ToFolderConfig(legacy, "/repo");

            Assert.Equal("/repo", fc.FolderPath);
            Assert.Equal(new List<string> { "-d", "--all-projects" }, fc.AdditionalParameters);
            Assert.Equal("KEY=VALUE", fc.AdditionalEnv);
            Assert.Equal("my-org", fc.PreferredOrg);
            Assert.Equal("auto-org", fc.AutoDeterminedOrg);
            Assert.True(fc.OrgSetByUser);
        }

        [Fact]
        public void ToFolderConfig_FallsBackToLegacyOrganization_AndMarksUserSet()
        {
            // Pre-org-split profile: only the single Organization field is set (a user override).
            var legacy = new LegacySolutionSettings
            {
                Organization = "legacy-org",
                PreferredOrg = null,
                OrgSetByUser = false,
            };

            var fc = LegacySolutionSettingsMigrator.ToFolderConfig(legacy, "/repo");

            Assert.Equal("legacy-org", fc.PreferredOrg);
            Assert.True(fc.OrgSetByUser);
        }

        [Fact]
        public void ToFolderConfig_EmptyOptions_LeavesFieldsUnset()
        {
            var fc = LegacySolutionSettingsMigrator.ToFolderConfig(new LegacySolutionSettings(), "/repo");

            Assert.Null(fc.AdditionalParameters);
            Assert.Null(fc.AdditionalEnv);
            Assert.Null(fc.PreferredOrg);
            Assert.Null(fc.AutoDeterminedOrg);
            Assert.False(fc.OrgSetByUser);
        }

        [Fact]
        public void Merge_AddsNewEntry_WhenNoMatchingPath()
        {
            var migrated = new FolderConfig { FolderPath = "/repo", PreferredOrg = "org" };

            var result = LegacySolutionSettingsMigrator.Merge(null, migrated);

            Assert.Single(result);
            Assert.Same(migrated, result[0]);
        }

        [Fact]
        public void Merge_FillsOnlyGaps_AndNeverClobbersExisting()
        {
            // Existing entry was provided by the LS (case-differing path); it already has an org.
            var existing = new List<FolderConfig>
            {
                new FolderConfig { FolderPath = "/Repo", PreferredOrg = "ls-org", OrgSetByUser = false },
            };
            var migrated = new FolderConfig
            {
                FolderPath = "/repo",
                PreferredOrg = "legacy-org",
                OrgSetByUser = true,
                AdditionalEnv = "KEY=VALUE",
                AdditionalParameters = new List<string> { "-d" },
            };

            var result = LegacySolutionSettingsMigrator.Merge(existing, migrated);

            Assert.Single(result); // matched case-insensitively, not appended
            var fc = result[0];
            Assert.Equal("ls-org", fc.PreferredOrg);   // not clobbered
            Assert.False(fc.OrgSetByUser);             // untouched because PreferredOrg was already set
            Assert.Equal("KEY=VALUE", fc.AdditionalEnv); // gap filled
            Assert.Equal(new List<string> { "-d" }, fc.AdditionalParameters); // gap filled
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void SplitArguments_BlankInput_ReturnsEmpty(string raw)
        {
            Assert.Empty(LegacySolutionSettingsMigrator.SplitArguments(raw));
        }

        [Fact]
        public void SplitArguments_RespectsDoubleQuotedSegments()
        {
            var result = LegacySolutionSettingsMigrator.SplitArguments("--org \"my org\" -d");

            Assert.Equal(new List<string> { "--org", "my org", "-d" }, result);
        }
    }
}
