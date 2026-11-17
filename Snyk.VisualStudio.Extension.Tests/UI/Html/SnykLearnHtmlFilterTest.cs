using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    public class SnykLearnHtmlFilterTest
    {
        [Theory]
        [InlineData(
            @"<div class=""sn-learn""><div class=""lesson-icon""><svg></svg></div><a class=""lesson-link styled-link"" href=""https://learn.snyk.io/lesson/sql-injection/?loc=ide"">Learn about this issue type<svg></svg></a></div>")]
        [InlineData(
            @"<div class=""sn-learn"" id=""learn""><svg></svg><a class=""learn--link lesson-link"" id=""learn--link"" href=""https://learn.snyk.io/lesson/open-redirect/?loc=ide"">Learn about this issue type<svg></svg></a></div>")]
        public void RemoveSnykLearn_KnownIssueDescriptionComponent_RemovesWholeComponent(string learnComponent)
        {
            var html = "<section><h1>Issue title</h1>" + learnComponent + "</section><p>Issue details</p>";

            var result = SnykLearnHtmlFilter.RemoveSnykLearn(html);

            Assert.Equal("<section><h1>Issue title</h1></section><p>Issue details</p>", result);
        }

        [Fact]
        public void RemoveSnykLearn_UnwrappedLearnAnchor_RemovesAnchorAndItsLabel()
        {
            const string html =
                "<p>Before</p><a href='HTTPS://LEARN.SNYK.IO/lesson/x'>Snyk Learn</a><p>After</p>";

            var result = SnykLearnHtmlFilter.RemoveSnykLearn(html);

            Assert.Equal("<p>Before</p><p>After</p>", result);
        }

        [Fact]
        public void RemoveSnykLearn_PreservesIssueDetailsAndUnrelatedExternalLinks()
        {
            const string html =
                "<h2>Remediation</h2>" +
                "<p>Upgrade the package and review <code>example</code>.</p>" +
                "<a href=\"https://docs.snyk.io/fix\">Documentation</a>" +
                "<a href=\"https://cwe.mitre.org/data/definitions/79.html\">CWE-79</a>";

            Assert.Equal(html, SnykLearnHtmlFilter.RemoveSnykLearn(html));
        }

        [Fact]
        public void RemoveSnykLearn_SimilarButDifferentHost_PreservesLink()
        {
            const string html =
                "<a href=\"https://learn.snyk.io.example.com/lesson/x\">Unrelated host</a>";

            Assert.Equal(html, SnykLearnHtmlFilter.RemoveSnykLearn(html));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void RemoveSnykLearn_EmptyInput_ReturnsInput(string html)
        {
            Assert.Equal(html, SnykLearnHtmlFilter.RemoveSnykLearn(html));
        }
    }
}
