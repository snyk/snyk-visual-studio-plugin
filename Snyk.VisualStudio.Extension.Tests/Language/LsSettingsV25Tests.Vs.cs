using Microsoft.VisualStudio.Sdk.TestFramework;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    /// <summary>
    /// Keeps <see cref="LsSettingsV25Tests"/> in the MockedVS collection when running under the
    /// Visual Studio SDK test framework, so it stays serialized alongside the other VS-dependent
    /// tests exactly as before. The tests themselves use only local mocks, which is why the rest of
    /// the class compiles in the cross-platform build (see docs/cross-platform-testing.md).
    /// </summary>
    [Collection(MockedVS.Collection)]
    public partial class LsSettingsV25Tests
    {
    }
}
