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
        private readonly Mock<ISnykOptionsManager> snykOptionsManagerMock;
        private readonly Mock<ILanguageClientManager> languageClientManagerMock;
        private readonly Mock<ISnykGeneralOptionsDialogPage> generalSettingsPageMock;
        private readonly Mock<ISolutionService> solutionServiceMock;
        private readonly SnykLanguageClientCustomTarget cut;

        public SnykLanguageClientCustomTargetTests(GlobalServiceProvider gsp) : base(gsp)
        {
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            tasksServiceMock = new Mock<ISnykTasksService>();
            optionsMock = new Mock<ISnykOptions>();
            generalSettingsPageMock = new Mock<ISnykGeneralOptionsDialogPage>();
            snykOptionsManagerMock = new Mock<ISnykOptionsManager>();
            languageClientManagerMock = new Mock<ILanguageClientManager>();
            solutionServiceMock = new Mock<ISolutionService>();

            var featureFlagServiceMock = new Mock<IFeatureFlagService>();
            
            featureFlagServiceMock.Setup(x => x.RefreshAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            solutionServiceMock.Setup(s => s.GetSolutionFolderAsync())
                .ReturnsAsync("/path/to/folder1");

            serviceProviderMock.SetupGet(sp => sp.TasksService).Returns(tasksServiceMock.Object);
            serviceProviderMock.SetupGet(sp => sp.Options).Returns(optionsMock.Object);
            serviceProviderMock.SetupGet(sp => sp.GeneralOptionsDialogPage).Returns(generalSettingsPageMock.Object);
            serviceProviderMock.SetupGet(sp => sp.SnykOptionsManager).Returns(snykOptionsManagerMock.Object);
            serviceProviderMock.SetupGet(sp => sp.FeatureFlagService).Returns(featureFlagServiceMock.Object);
            serviceProviderMock.SetupGet(sp => sp.LanguageClientManager).Returns(languageClientManagerMock.Object);
            serviceProviderMock.SetupGet(sp => sp.SolutionService).Returns(solutionServiceMock.Object);
            
            // Setup GetEffectiveOrganizationAsync mock
            snykOptionsManagerMock.Setup(s => s.GetEffectiveOrganizationAsync()).ReturnsAsync("auto-determined-org");
            optionsMock.SetupAllProperties();
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
        public async Task OnPublishDiagnostics316_ShouldHandleNonAsciiPath()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                'uri': 'file:///c:/users/user/dir - with - space üaöä中文/file.cs',
                'diagnostics': [
                    {
                        'source': 'Snyk Code',
                        'data': { 'id': 'issue1', 'isIgnored': false }
                    }
                ]
            }");

            // Act
            await cut.OnPublishDiagnostics316(arg);
            
            // Assert
            Assert.True(cut.GetCodeDictionary().ContainsKey("c:\\users\\user\\dir - with - space üaöä中文\\file.cs"));
        }

        [Fact]
        public async Task OnPublishDiagnostics316_ShouldHandlePath()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                'uri': 'file:///c:/users/user/dir/file.cs',
                'diagnostics': [
                    {
                        'source': 'Snyk Code',
                        'data': { 'id': 'issue1', 'isIgnored': false }
                    }
                ]
            }");

            // Act
            await cut.OnPublishDiagnostics316(arg);

            // Assert
            Assert.True(cut.GetCodeDictionary().ContainsKey("c:\\users\\user\\dir\\file.cs"));
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
            snykOptionsManagerMock.Verify(s => s.Save(It.IsAny<IPersistableOptions>(), false), Times.Once);
            // Note: DidChangeConfigurationAsync is intentionally not called to avoid infinite loop
            languageClientManagerMock.Verify(s => s.DidChangeConfigurationAsync(It.IsAny<CancellationToken>()), Times.Never);
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

        [Fact]
        public async Task OnFolderConfig_ShouldUpdateOrganization_WhenAutoDeterminedOrgExists()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                'folderConfigs': [
                    {
                        'folderPath': '/path/to/folder1',
                        'baseBranch': 'main',
                        'autoDeterminedOrg': 'auto-determined-org',
                        'preferredOrg': '',
                        'orgSetByUser': false,
                        'orgMigratedFromGlobalConfig': false
                    }
                ]
            }");
            optionsMock.SetupProperty(o => o.FolderConfigs);
            optionsMock.SetupProperty(o => o.Organization);

            // Act
            await cut.OnFolderConfig(arg);

            // Assert
            // Global organization is NOT updated when receiving folder configs - Language Server handles fallback
            // Only the auto-determined org is saved for solution-specific settings
            snykOptionsManagerMock.Verify(s => s.Save(It.IsAny<IPersistableOptions>(), It.IsAny<bool>()), Times.Once);
            snykOptionsManagerMock.Verify(s => s.SaveAutoDeterminedOrgAsync("auto-determined-org"), Times.Once);
            snykOptionsManagerMock.Verify(s => s.SavePreferredOrgAsync(""), Times.Once);
            snykOptionsManagerMock.Verify(s => s.SaveOrgSetByUserAsync(false), Times.Once);
            // SaveOrganizationAsync should NOT be called - global org is not updated from folder configs
            snykOptionsManagerMock.Verify(s => s.SaveOrganizationAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task OnFolderConfig_ShouldNotUpdateOrganization_WhenNoAutoDeterminedOrg()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                'folderConfigs': [
                    {
                        'folderPath': '/path/to/folder1',
                        'baseBranch': 'main'
                    }
                ]
            }");
            optionsMock.SetupProperty(o => o.FolderConfigs);
            optionsMock.SetupProperty(o => o.Organization);
            var originalOrg = "original-org";
            optionsMock.Object.Organization = originalOrg;

            // Act
            await cut.OnFolderConfig(arg);

            // Assert
            // Global organization should remain unchanged when no auto-determined org exists
            Assert.Equal(originalOrg, optionsMock.Object.Organization);
            snykOptionsManagerMock.Verify(s => s.Save(It.IsAny<IPersistableOptions>(), It.IsAny<bool>()), Times.Once);
            // SaveOrganizationAsync should NOT be called - global org is not updated from folder configs
            snykOptionsManagerMock.Verify(s => s.SaveOrganizationAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task OnFolderConfig_ShouldNotUpdateOrganization_WhenAutoDeterminedOrgIsNull()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                'folderConfigs': [
                    {
                        'folderPath': '/path/to/folder1',
                        'baseBranch': 'main',
                        'autoDeterminedOrg': null
                    }
                ]
            }");
            optionsMock.SetupProperty(o => o.FolderConfigs);
            optionsMock.SetupProperty(o => o.Organization);
            var originalOrg = "original-org";
            optionsMock.Object.Organization = originalOrg;

            // Act
            await cut.OnFolderConfig(arg);

            // Assert
            // Global organization should remain unchanged when auto-determined org is null
            Assert.Equal(originalOrg, optionsMock.Object.Organization);
            snykOptionsManagerMock.Verify(s => s.Save(It.IsAny<IPersistableOptions>(), It.IsAny<bool>()), Times.Once);
            // SaveOrganizationAsync should NOT be called - global org is not updated from folder configs
            snykOptionsManagerMock.Verify(s => s.SaveOrganizationAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task OnFolderConfig_ShouldUpdateOrganization_WithBothOrgFields()
        {
            // Arrange
            // Note: Visual Studio processes only ONE folder config - the one matching the current solution path.
            // The second folder config (/path/to/folder2) is included to verify that non-matching configs are ignored.
            // Solution path is set to '/path/to/folder1' in test setup, so only the first config will be processed.
            var arg = JObject.Parse(@"{
                'folderConfigs': [
                    {
                        'folderPath': '/path/to/folder1',
                        'baseBranch': 'main',
                        'preferredOrg': 'user-specified-org',
                        'autoDeterminedOrg': 'auto-determined-org',
                        'orgSetByUser': true,
                        'orgMigratedFromGlobalConfig': false
                    },
                    {
                        'folderPath': '/path/to/folder2',
                        'baseBranch': 'master',
                        'preferredOrg': '',
                        'autoDeterminedOrg': 'auto-determined-org-2',
                        'orgSetByUser': false,
                        'orgMigratedFromGlobalConfig': true
                    }
                ]
            }");
            optionsMock.SetupProperty(o => o.FolderConfigs);
            optionsMock.SetupProperty(o => o.Organization);

            // Act
            await cut.OnFolderConfig(arg);

            // Assert
            // Only the matching folder config (/path/to/folder1) is processed, so save methods are called once
            snykOptionsManagerMock.Verify(s => s.Save(It.IsAny<IPersistableOptions>(), It.IsAny<bool>()), Times.Once);
            snykOptionsManagerMock.Verify(s => s.SaveAutoDeterminedOrgAsync("auto-determined-org"), Times.Once);
            snykOptionsManagerMock.Verify(s => s.SavePreferredOrgAsync("user-specified-org"), Times.Once);
            snykOptionsManagerMock.Verify(s => s.SaveOrgSetByUserAsync(true), Times.Once);
            // GetEffectiveOrganizationAsync is not called - Language Server handles fallback logic
            snykOptionsManagerMock.Verify(s => s.GetEffectiveOrganizationAsync(), Times.Never);
        }

        [Fact]
        public async Task OnFolderConfig_ShouldSaveBothOrgFields_WhenBothExist()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                'folderConfigs': [
                    {
                        'folderPath': '/path/to/folder1',
                        'preferredOrg': 'user-preferred-org',
                        'autoDeterminedOrg': 'auto-detected-org',
                        'orgSetByUser': true,
                        'orgMigratedFromGlobalConfig': false
                    }
                ]
            }");

            optionsMock.SetupProperty(o => o.FolderConfigs);
            optionsMock.SetupProperty(o => o.Organization);

            // Act
            await cut.OnFolderConfig(arg);

            // Assert
            snykOptionsManagerMock.Verify(s => s.SaveAutoDeterminedOrgAsync("auto-detected-org"), Times.Once);
            snykOptionsManagerMock.Verify(s => s.SavePreferredOrgAsync("user-preferred-org"), Times.Once);
            snykOptionsManagerMock.Verify(s => s.SaveOrgSetByUserAsync(true), Times.Once);
            // GetEffectiveOrganizationAsync is not called - Language Server handles fallback logic
            snykOptionsManagerMock.Verify(s => s.GetEffectiveOrganizationAsync(), Times.Never);
        }

        [Fact]
        public async Task OnFolderConfig_ShouldSaveEmptyPreferredOrg_WhenPreferredOrgIsEmpty()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                'folderConfigs': [
                    {
                        'folderPath': '/path/to/folder1',
                        'preferredOrg': '',
                        'autoDeterminedOrg': 'auto-detected-org',
                        'orgSetByUser': false,
                        'orgMigratedFromGlobalConfig': false
                    }
                ]
            }");

            optionsMock.SetupProperty(o => o.FolderConfigs);
            optionsMock.SetupProperty(o => o.Organization);

            // Act
            await cut.OnFolderConfig(arg);

            // Assert
            snykOptionsManagerMock.Verify(s => s.SaveAutoDeterminedOrgAsync("auto-detected-org"), Times.Once);
            snykOptionsManagerMock.Verify(s => s.SavePreferredOrgAsync(""), Times.Once);
            snykOptionsManagerMock.Verify(s => s.SaveOrgSetByUserAsync(false), Times.Once);
        }
    }
}
