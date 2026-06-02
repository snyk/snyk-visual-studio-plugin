using System.Collections.Generic;
using Snyk.VisualStudio.Extension.Language;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    public class FolderConfigMatcherTests
    {
        [Theory]
        [InlineData(@"C:\repo\project", @"C:\repo\project", true)]   // exact
        [InlineData(@"C:\repo\PROJECT", @"c:\repo\project", true)]   // case-insensitive
        [InlineData(@"C:\repo\project\", @"C:\repo\project", true)]  // trailing separator on config path
        [InlineData(@"C:\repo", @"C:\repo\project", true)]           // solution sits inside config path (subfolder)
        [InlineData(@"C:\repo/", @"C:\repo/project", true)]          // forward-slash separator
        [InlineData(@"C:/repo/project", @"C:\repo\project", true)]   // mixed separators, same folder
        [InlineData(@"C:/repo", @"C:\repo\project", true)]           // mixed separators, subfolder
        [InlineData(@"C:\repo\other", @"C:\repo\project", false)]    // sibling folder
        [InlineData(@"C:\repo\projectX", @"C:\repo\project", false)] // prefix but not a path boundary
        [InlineData("", @"C:\repo\project", false)]                  // empty config path
        [InlineData(@"C:\repo\project", "", false)]                  // empty solution path
        public void Matches_AppliesExactAndSubfolderRules(string folderPath, string solutionPath, bool expected)
        {
            Assert.Equal(expected, FolderConfigMatcher.Matches(folderPath, solutionPath));
        }

        [Fact]
        public void FindMatching_ReturnsTheEntryForTheCurrentSolution()
        {
            var configs = new List<FolderConfig>
            {
                new FolderConfig { FolderPath = @"C:\repo\other" },
                new FolderConfig { FolderPath = @"C:\repo\project" },
            };

            var match = FolderConfigMatcher.FindMatching(configs, @"C:\repo\project");

            Assert.NotNull(match);
            Assert.Equal(@"C:\repo\project", match.FolderPath);
        }

        [Fact]
        public void FindMatching_ReturnsNull_WhenNoEntryMatches()
        {
            var configs = new List<FolderConfig>
            {
                new FolderConfig { FolderPath = @"C:\repo\other" },
            };

            Assert.Null(FolderConfigMatcher.FindMatching(configs, @"C:\repo\project"));
        }

        [Fact]
        public void FindMatching_HandlesNullListAndNullEntries()
        {
            Assert.Null(FolderConfigMatcher.FindMatching(null, @"C:\repo\project"));

            var configs = new List<FolderConfig> { null, new FolderConfig { FolderPath = @"C:\repo\project" } };
            Assert.NotNull(FolderConfigMatcher.FindMatching(configs, @"C:\repo\project"));
        }
    }
}
