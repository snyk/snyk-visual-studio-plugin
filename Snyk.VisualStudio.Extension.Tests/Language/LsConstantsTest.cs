using Snyk.VisualStudio.Extension.Language;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    public class LsConstantsTest
    {
        [Fact]
        public void ProtocolVersion_ShouldBe25()
        {
            Assert.Equal("25", LsConstants.ProtocolVersion);
        }

        [Fact]
        public void SnykConfiguration_ShouldHaveCorrectValue()
        {
            Assert.Equal("$/snyk.configuration", LsConstants.SnykConfiguration);
        }
    }
}
