using Snyk.VisualStudio.Extension.Language;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    public class IssueIgnoreDisplayTest
    {
        [Fact]
        public void GetDisplayTitleWithLineNumber_ShowsIgnoredBadge_WhenIssueIsIgnored()
        {
            // Arrange
            var issue = new Issue
            {
                IsIgnored = true,
                Product = Product.Secrets,
                Title = "Hardcoded API key",
                Range = new Range { End = new End { Line = 42 } },
            };

            // Act
            var title = issue.GetDisplayTitleWithLineNumber();

            // Assert
            Assert.StartsWith("[ Ignored ] ", title);
            Assert.Contains("line 42:", title);
        }

        [Fact]
        public void GetDisplayTitleWithLineNumber_OmitsIgnoredBadge_WhenIssueIsNotIgnored()
        {
            // Arrange
            var issue = new Issue
            {
                IsIgnored = false,
                Product = Product.Secrets,
                Title = "Hardcoded API key",
                Range = new Range { End = new End { Line = 7 } },
            };

            // Act
            var title = issue.GetDisplayTitleWithLineNumber();

            // Assert
            Assert.DoesNotContain("[ Ignored ]", title);
            Assert.Contains("line 7:", title);
        }
    }
}
