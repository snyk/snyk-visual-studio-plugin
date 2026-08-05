using Microsoft.VisualStudio.Sdk.TestFramework;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    /// <summary>
    /// Keeps <see cref="SnykOptionsTest"/> in the MockedVS collection when running under the Visual
    /// Studio SDK test framework, so it stays serialized alongside the other VS-dependent tests
    /// exactly as before (see docs/cross-platform-testing.md).
    /// </summary>
    [Collection(MockedVS.Collection)]
    public partial class SnykOptionsTest
    {
    }
}
