using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    [Collection(MockedVS.Collection)]
    public class LsSettingsTest
    {
        private LsSettings cut;
        private readonly Mock<ISnykOptions> optionsMock;

        public LsSettingsTest(GlobalServiceProvider sp)
        {
            sp.Reset();
            optionsMock = new Mock<ISnykOptions>();
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            serviceProviderMock.Setup(x => x.Options).Returns(optionsMock.Object);
            cut = new LsSettings(serviceProviderMock.Object);
        }

        [Fact]
        public void GetInitializationOptions_ShouldReturnValidOptions_WhenServiceProviderIsNotNull()
        {
            // Arrange
            TestUtils.SetupOptionsMock(optionsMock);

            // Act
            var initOptions = cut.GetInitializationOptions();

            // Assert
            Assert.NotNull(initOptions);
            Assert.Equal("true", initOptions.ActivateSnykCodeSecurity);
            Assert.Equal("true", initOptions.ActivateSnykCodeQuality);
            Assert.Equal("true", initOptions.ActivateSnykOpenSource);
            Assert.Equal("true", initOptions.ActivateSnykIac);
            Assert.Equal("true", initOptions.ManageBinariesAutomatically);
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
            Assert.Equal("false", initOptions.Insecure);
            Assert.Equal(LsConstants.ProtocolVersion, initOptions.RequiredProtocolVersion);
            Assert.Equal("plain", initOptions.OutputFormat);
            Assert.Equal("device-id-123", initOptions.DeviceId);
            Assert.Equal("true", initOptions.EnableDeltaFindings);
        }
    }
}
