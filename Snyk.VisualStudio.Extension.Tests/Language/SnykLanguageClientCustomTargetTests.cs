using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Newtonsoft.Json.Linq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    [Collection(MockedVS.Collection)]
    public class SnykLanguageClientCustomTargetTests : PackageBaseTest
    {
        private readonly Mock<ISnykTasksService> tasksServiceMock;
        private readonly Mock<ISnykOptions> optionsMock;
        private readonly Mock<IUserStorageSettingsService> userStorageSettingsServiceMock;
        private readonly Mock<ILanguageClientManager> languageClientManagerMock;
        private readonly Mock<ISnykGeneralOptionsDialogPage> generalSettingsPageMock;
        private readonly SnykLanguageClientCustomTarget cut;

        public SnykLanguageClientCustomTargetTests(GlobalServiceProvider gsp) : base(gsp)
        {
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            tasksServiceMock = new Mock<ISnykTasksService>();
            optionsMock = new Mock<ISnykOptions>();
            generalSettingsPageMock = new Mock<ISnykGeneralOptionsDialogPage>();
            userStorageSettingsServiceMock = new Mock<IUserStorageSettingsService>();
            languageClientManagerMock = new Mock<ILanguageClientManager>();

            var featureFlagServiceMock = new Mock<IFeatureFlagService>();
            
            featureFlagServiceMock.Setup(x => x.RefreshAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            serviceProviderMock.SetupGet(sp => sp.TasksService).Returns(tasksServiceMock.Object);
            serviceProviderMock.SetupGet(sp => sp.Options).Returns(optionsMock.Object);
            serviceProviderMock.SetupGet(sp => sp.GeneralOptionsDialogPage).Returns(generalSettingsPageMock.Object);
            serviceProviderMock.SetupGet(sp => sp.UserStorageSettingsService).Returns(userStorageSettingsServiceMock.Object);
            serviceProviderMock.SetupGet(sp => sp.FeatureFlagService).Returns(featureFlagServiceMock.Object);
            serviceProviderMock.SetupGet(sp => sp.LanguageClientManager).Returns(languageClientManagerMock.Object);

            cut = new SnykLanguageClientCustomTarget(serviceProviderMock.Object);
        }

        [Fact]
        public async Task OnPublishDiagnostics316_ShouldHandleNullUri()
        {
            // Arrange
            var arg = JObject.Parse("{\"diagnostics\":[]}");

            // Act
            await cut.OnPublishDiagnostics316(arg);

            // Assert
            // Expect no exceptions when uri is null
        }

        [Fact]
        public async Task OnPublishDiagnostics316_ShouldHandleEmptyDiagnostics()
        {
            // Arrange
            var arg = JObject.Parse("{\"uri\":\"file:///path/to/file\",\"diagnostics\":[]}");

            // Act
            await cut.OnPublishDiagnostics316(arg);

            // Assert
        }

        [Fact]
        public async Task OnPublishDiagnostics316_ShouldAddIssuesToDictionary()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                'uri': 'file:///path/to/file',
                'diagnostics': [
                    {
                        'source': 'Snyk Code',
                        'data': { 'id': 'issue1', 'isIgnored': false }
                    }
                ]
            }");

            optionsMock.SetupProperty(o => o.ConsistentIgnoresEnabled, false);

            // Act
            await cut.OnPublishDiagnostics316(arg);

            // Assert
            // Verify that ConsistentIgnoresEnabled remains false
            Assert.False(optionsMock.Object.ConsistentIgnoresEnabled);
        }

        [Fact]
        public async Task OnSnykScan_ShouldFireScanningCancelledEvent_WhenCancellationRequested()
        {
            // Arrange
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();
            tasksServiceMock.SetupGet(t => t.SnykScanTokenSource).Returns(tokenSource);

            // Act
            await cut.OnSnykScan(null);

            // Assert
            tasksServiceMock.Verify(t => t.FireScanningCancelledEvent(), Times.Once);
        }

        [Fact]
        public async Task OnHasAuthenticated_ShouldHandleFailedAuthentication_WhenTokenIsNull()
        {
            // Arrange
            var arg = JObject.Parse("{}");
            generalSettingsPageMock.Setup(o => o.HandleFailedAuthentication(It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            await cut.OnHasAuthenticated(arg);

            // Assert
            generalSettingsPageMock.Verify(o => o.HandleFailedAuthentication("Authentication failed"), Times.Once);
        }

        [Fact]
        public async Task OnHasAuthenticated_ShouldHandleAuthenticationSuccess_WhenTokenIsProvided()
        {
            // Arrange
            var arg = JObject.Parse("{'token':'test-token','apiUrl':'https://api.snyk.io'}");
            generalSettingsPageMock.Setup(o => o.HandleAuthenticationSuccess("test-token", "https://api.snyk.io")).Returns(Task.CompletedTask);
            optionsMock.SetupGet(o => o.AuthenticationMethod).Returns(AuthenticationType.OAuth);

            // Act
            await cut.OnHasAuthenticated(arg);

            // Assert
            optionsMock.VerifySet(o => o.CustomEndpoint = "https://api.snyk.io");
            optionsMock.VerifySet(o => o.ApiToken = It.Is<AuthenticationToken>(t => t.Type == AuthenticationType.OAuth && t.ToString() == "test-token"));
            generalSettingsPageMock.Verify(o => o.HandleAuthenticationSuccess("test-token", "https://api.snyk.io"), Times.Once);
        }

        [Fact]
        public async Task OnAddTrustedFolders_ShouldUpdateTrustedFolders()
        {
            // Arrange
            var arg = JObject.Parse("{'trustedFolders':['/folder1','/folder2']}");
            optionsMock.SetupProperty(o => o.TrustedFolders);

            // Act
            await cut.OnAddTrustedFolders(arg);

            // Assert
            Assert.NotNull(optionsMock.Object.TrustedFolders);
            Assert.Equal(2, optionsMock.Object.TrustedFolders.Count);
            Assert.Contains("/folder1", optionsMock.Object.TrustedFolders);
            Assert.Contains("/folder2", optionsMock.Object.TrustedFolders);
            userStorageSettingsServiceMock.Verify(s => s.SaveSettings(), Times.Once);
            languageClientManagerMock.Verify(s => s.DidChangeConfigurationAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task OnFolderConfig_ShouldUpdateFolderConfigs_WhenValidArgProvided()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                'folderConfigs': [
                        { 'folderPath': '/path/to/folder1', 'baseBranch': 'main' },
                        { 'folderPath': '/path/to/folder2', 'baseBranch': 'master' }
                        ]
                }");
            var expectedFolderConfigs = new List<FolderConfig>
            {
                new FolderConfig { FolderPath = "/path/to/folder1", BaseBranch = "main" },
                new FolderConfig { FolderPath = "/path/to/folder2", BaseBranch = "master" }
            };

            optionsMock.SetupProperty(o => o.FolderConfigs);

            // Act
            await cut.OnFolderConfig(arg);

            // Assert
            Assert.NotNull(optionsMock.Object.FolderConfigs);
            Assert.Equal(expectedFolderConfigs.Count, optionsMock.Object.FolderConfigs.Count);
            for (var i = 0; i < expectedFolderConfigs.Count; i++)
            {
                Assert.Equal(expectedFolderConfigs[i].FolderPath, optionsMock.Object.FolderConfigs[i].FolderPath);
                Assert.Equal(expectedFolderConfigs[i].BaseBranch, optionsMock.Object.FolderConfigs[i].BaseBranch);
            }
        }

        [Fact]
        public async Task OnFolderConfig_ShouldDoNothing_WhenFolderConfigsIsNull()
        {
            // Arrange
            var arg = JObject.Parse("{}");
            optionsMock.SetupProperty(o => o.FolderConfigs);

            // Act
            await cut.OnFolderConfig(arg);

            // Assert
            Assert.Null(optionsMock.Object.FolderConfigs);
        }
    }
}
