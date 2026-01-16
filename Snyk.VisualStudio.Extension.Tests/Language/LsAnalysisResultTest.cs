using System;
using Snyk.VisualStudio.Extension.Language;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    public class LsAnalysisResultTest
    {
        [Theory]
        [InlineData("critical", 4_000_000)]
        [InlineData("high", 3_000_000)]
        [InlineData("medium", 2_000_000)]
        [InlineData("low", 1_000_000)]
        [InlineData("unknown", 0)]
        public void Priority_ShouldMapSeverityToMillions(string severity, int expectedBase)
        {
            var issue = new Issue
            {
                Severity = severity,
                Product = Product.Oss,
                AdditionalData = new AdditionalData { RiskScore = 0 }
            };

            Assert.Equal(expectedBase, issue.Priority);
        }

        [Theory]
        [InlineData(Product.Oss, 500, 999, 3_000_500)]   // OSS uses RiskScore
        [InlineData(Product.Iac, 300, 999, 3_000_300)]   // IaC uses RiskScore
        [InlineData(Product.Code, 999, 750, 3_000_750)]  // Code uses PriorityScore
        public void Priority_ShouldUseCorrectScoreForProduct(string product, int riskScore, int priorityScore, int expected)
        {
            var issue = new Issue
            {
                Severity = "high",
                Product = product,
                AdditionalData = new AdditionalData { RiskScore = riskScore, PriorityScore = priorityScore }
            };

            Assert.Equal(expected, issue.Priority);
        }

        [Fact]
        public void Priority_ShouldThrow_WhenProductIsUnknown()
        {
            var issue = new Issue
            {
                Severity = "high",
                Product = "unknown_product",
                AdditionalData = new AdditionalData { RiskScore = 100 }
            };

            Assert.Throws<InvalidOperationException>(() => _ = issue.Priority);
        }
    }
}
