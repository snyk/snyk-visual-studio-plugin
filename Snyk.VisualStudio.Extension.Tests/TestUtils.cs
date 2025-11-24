using System.Collections.Generic;
using Moq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class TestUtils
    {
        public static void SetupOptionsManagerMock(Mock<ISnykOptionsManager> snykOptionsManager)
        {
            snykOptionsManager.Setup(o => o.GetAdditionalOptionsAsync()).ReturnsAsync("--debug");
            snykOptionsManager.Setup(o => o.GetEffectiveOrganizationAsync()).ReturnsAsync("test-org");
        }

        public static void SetupOptionsMock(Mock<ISnykOptions> optionsMock)
        {
            optionsMock.SetupGet(o => o.SnykCodeSecurityEnabled).Returns(true);
            optionsMock.SetupGet(o => o.OssEnabled).Returns(true);
            optionsMock.SetupGet(o => o.IacEnabled).Returns(true);
            optionsMock.SetupGet(o => o.BinariesAutoUpdate).Returns(true);
            optionsMock.SetupGet(o => o.TrustedFolders).Returns(new HashSet<string> { "/path/to/trusted" });
            optionsMock.SetupGet(o => o.IntegrationEnvironment).Returns("Visual Studio 2022");
            optionsMock.SetupGet(o => o.IntegrationName).Returns("VISUAL_STUDIO");
            optionsMock.SetupGet(o => o.IntegrationEnvironmentVersion).Returns("2022");
            optionsMock.SetupGet(o => o.IntegrationVersion).Returns("1.0.0");
            optionsMock.SetupGet(o => o.AutoScan).Returns(true);
            optionsMock.SetupGet(o => o.AuthenticationMethod).Returns(AuthenticationType.OAuth);
            optionsMock.SetupGet(o => o.CliCustomPath).Returns("/path/to/cli");
            optionsMock.SetupGet(o => o.Organization).Returns("test-org");
            optionsMock.SetupGet(o => o.ApiToken).Returns(new AuthenticationToken(AuthenticationType.OAuth, "test-token"));
            optionsMock.SetupGet(o => o.CustomEndpoint).Returns("https://api.snyk.io");
            optionsMock.SetupGet(o => o.IgnoreUnknownCA).Returns(false);
            optionsMock.SetupGet(o => o.EnableDeltaFindings).Returns(true);
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig>());
            optionsMock.SetupProperty(o => o.DeviceId, "device-id-123");
        }
    }
}