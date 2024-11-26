using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    [Collection(MockedVS.Collection)]
    public class LsConfigurationTest
    {
        private LsConfiguration cut;
        private readonly Mock<ISnykServiceProvider> serviceProviderMock;
        private readonly Mock<ISnykOptions> optionsMock;

        public LsConfigurationTest(GlobalServiceProvider sp)
        {
            sp.Reset();
            optionsMock = new Mock<ISnykOptions>();
            serviceProviderMock = new Mock<ISnykServiceProvider>();
            serviceProviderMock.Setup(x => x.Options).Returns(optionsMock.Object);
            cut = new LsConfiguration(serviceProviderMock.Object);
        }

        [Fact]
        public void GetInitializationOptions_ShouldReturnValidOptions_WhenServiceProviderIsNotNull()
        {
            // Arrange
            optionsMock.SetupGet(o => o.SnykCodeSecurityEnabled).Returns(true);
            optionsMock.SetupGet(o => o.SnykCodeQualityEnabled).Returns(true);
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
            optionsMock.Setup(o => o.GetAdditionalOptionsAsync()).ReturnsAsync("--debug");
            optionsMock.SetupProperty(o => o.DeviceId, "device-id-123");

            // Act
            var initOptions = cut.GetInitializationOptions();

            // Assert
            Assert.NotNull(initOptions);
            Assert.Equal("True", initOptions.ActivateSnykCode);
            Assert.Equal("True", initOptions.ActivateSnykCodeSecurity);
            Assert.Equal("True", initOptions.ActivateSnykCodeQuality);
            Assert.Equal("True", initOptions.ActivateSnykOpenSource);
            Assert.Equal("True", initOptions.ActivateSnykIac);
            Assert.Equal("true", initOptions.SendErrorReports);
            Assert.Equal("True", initOptions.ManageBinariesAutomatically);
            Assert.Equal("false", initOptions.EnableTrustedFoldersFeature);
            Assert.Contains("/path/to/trusted", initOptions.TrustedFolders);
            Assert.Equal("Visual Studio 2022@@VISUAL_STUDIO", initOptions.IntegrationName);
            Assert.Equal("2022@@1.0.0", initOptions.IntegrationVersion);
            Assert.Equal("auto", initOptions.ScanningMode);
            Assert.Equal("oauth", initOptions.AuthenticationMethod);
            Assert.Equal("/path/to/cli", initOptions.CliPath);
            Assert.Equal("test-org", initOptions.Organization);
            Assert.Equal("test-token", initOptions.Token);
            Assert.Equal("false", initOptions.AutomaticAuthentication);
            Assert.Equal("https://api.snyk.io", initOptions.Endpoint);
            Assert.Equal("False", initOptions.Insecure);
            Assert.Equal(LsConstants.ProtocolVersion, initOptions.RequiredProtocolVersion);
            Assert.Equal("plain", initOptions.OutputFormat);
            Assert.Equal("device-id-123", initOptions.DeviceId);
            Assert.Equal("True", initOptions.EnableDeltaFindings);
        }

    }
}
