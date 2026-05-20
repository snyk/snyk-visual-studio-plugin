using Snyk.VisualStudio.Extension.Language;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    public class LsConstantsTest
    {
        [Fact]
        public void ProtocolVersion_ShouldBe24()
        {
            Assert.Equal("24", LsConstants.ProtocolVersion);
        }

        [Fact]
        public void SnykConfiguration_ShouldHaveCorrectValue()
        {
            Assert.Equal("$/snyk.configuration", LsConstants.SnykConfiguration);
        }
    }
}
