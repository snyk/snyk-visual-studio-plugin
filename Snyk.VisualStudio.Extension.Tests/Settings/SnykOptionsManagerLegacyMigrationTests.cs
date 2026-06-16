using System.Collections.Generic;
using System.IO;
using System.Text;
using Moq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.Utils;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Settings
{
    public class SnykOptionsManagerLegacyMigrationTests
    {
        private const string SolutionFolder = "/test/solution";

        private readonly SnykOptionsManager cut;
        private readonly SnykOptions options;
        private readonly string testSettingsPath;

        public SnykOptionsManagerLegacyMigrationTests()
        {
            this.testSettingsPath = Path.GetTempFileName();

            // Seed settings.json with a legacy entry keyed by the same hash the old code used.
            var hash = LegacySolutionSettingsMigrator.ComputeFolderHash(SolutionFolder);
            var seed = new SnykSettings
            {
                SolutionSettingsDict = new Dictionary<int, LegacySolutionSettings>
                {
                    [hash] = new LegacySolutionSettings
                    {
                        AdditionalOptions = "-d --all-projects",
                        AdditionalEnv = "KEY=VALUE",
                        PreferredOrg = "seed-org",
                        OrgSetByUser = true,
                        IsAllProjectsScanEnabled = true,
                    },
                },
            };
            File.WriteAllText(this.testSettingsPath, Json.Serialize(seed), Encoding.UTF8);

            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            var solutionServiceMock = new Mock<ISolutionService>();
            solutionServiceMock.Setup(x => x.GetSolutionFolderAsync()).ReturnsAsync(SolutionFolder);
            serviceProviderMock.Setup(x => x.SolutionService).Returns(solutionServiceMock.Object);

            this.options = new SnykOptions
            {
                ApiToken = new AuthenticationToken(AuthenticationType.OAuth, "token"),
            };
            serviceProviderMock.Setup(x => x.Options).Returns(this.options);

            this.cut = new SnykOptionsManager(this.testSettingsPath, serviceProviderMock.Object);
        }

        [Fact]
        public void Migrate_SeedsFolderConfig_AndStripsLegacyEntry()
        {
            var migrated = this.cut.MigrateLegacySolutionSettings(SolutionFolder);

            Assert.True(migrated);

            var fc = Assert.Single(this.options.FolderConfigs);
            Assert.Equal(SolutionFolder, fc.FolderPath);
            Assert.Equal(new List<string> { "-d", "--all-projects" }, fc.AdditionalParameters);
            Assert.Equal("KEY=VALUE", fc.AdditionalEnv);
            Assert.Equal("seed-org", fc.PreferredOrg);
            Assert.True(fc.OrgSetByUser);

            // The migrated values and the now-empty legacy section are persisted.
            var reloaded = Json.Deserialize<SnykSettings>(File.ReadAllText(this.testSettingsPath, Encoding.UTF8));
            Assert.Null(reloaded.SolutionSettingsDict);
            Assert.Single(reloaded.FolderConfigs);
            Assert.Equal("seed-org", reloaded.FolderConfigs[0].PreferredOrg);
        }

        [Fact]
        public void Migrate_UnknownFolder_IsNoOp()
        {
            var migrated = this.cut.MigrateLegacySolutionSettings("/some/other/folder");

            Assert.False(migrated);
            Assert.Null(this.options.FolderConfigs);
        }

        [Fact]
        public void Migrate_IsIdempotent_SecondCallReturnsFalse()
        {
            Assert.True(this.cut.MigrateLegacySolutionSettings(SolutionFolder));
            Assert.False(this.cut.MigrateLegacySolutionSettings(SolutionFolder));
        }
    }
}
