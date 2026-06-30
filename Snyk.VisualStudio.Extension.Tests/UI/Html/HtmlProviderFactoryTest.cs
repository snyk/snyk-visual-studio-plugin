using System;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    /// <summary>
    /// Pins the provider-key → concrete provider mapping, including the "tree" case added with the
    /// HTML issue tree.
    /// </summary>
    public class HtmlProviderFactoryTest
    {
        [Theory]
        [InlineData("code", typeof(CodeHtmlProvider))]
        [InlineData("oss", typeof(OssHtmlProvider))]
        [InlineData("iac", typeof(IacHtmlProvider))]
        [InlineData("summary", typeof(SummaryHtmlProvider))]
        [InlineData("tree", typeof(TreeHtmlProvider))]
        [InlineData("static", typeof(StaticHtmlProvider))]
        public void GetHtmlProvider_ReturnsExpectedProvider_ForKnownKey(string key, Type expected)
        {
            var provider = HtmlProviderFactory.GetHtmlProvider(key);

            Assert.NotNull(provider);
            Assert.IsType(expected, provider);
        }

        [Theory]
        [InlineData("unknown")]
        [InlineData("")]
        [InlineData(null)]
        public void GetHtmlProvider_ReturnsNull_ForUnknownKey(string key)
        {
            Assert.Null(HtmlProviderFactory.GetHtmlProvider(key));
        }
    }
}
