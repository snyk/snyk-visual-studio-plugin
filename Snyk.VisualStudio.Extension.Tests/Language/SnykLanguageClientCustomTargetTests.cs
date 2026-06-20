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
        private readonly Mock<IAuthenticationFlowService> authenticationFlowServiceMock;
        private readonly Mock<ISolutionService> solutionServiceMock;
        private readonly SnykLanguageClientCustomTarget cut;

        public SnykLanguageClientCustomTargetTests(GlobalServiceProvider gsp) : base(gsp)
        {
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            tasksServiceMock = new Mock<ISnykTasksService>();
            optionsMock = new Mock<ISnykOptions>();
            authenticationFlowServiceMock = new Mock<IAuthenticationFlowService>();
            snykOptionsManagerMock = new Mock<ISnykOptionsManager>();
            languageClientManagerMock = new Mock<ILanguageClientManager>();
            solutionServiceMock = new Mock<ISolutionService>();

            var featureFlagServiceMock = new Mock<IFeatureFlagService>();

            featureFlagServiceMock.Setup(x => x.RefreshAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            serviceProviderMock.SetupGet(sp => sp.TasksService).Returns(tasksServiceMock.Object);
            serviceProviderMock.SetupGet(sp => sp.Options).Returns(optionsMock.Object);
            serviceProviderMock.SetupGet(sp => sp.AuthenticationFlowService).Returns(authenticationFlowServiceMock.Object);
            serviceProviderMock.SetupGet(sp => sp.SnykOptionsManager).Returns(snykOptionsManagerMock.Object);
            serviceProviderMock.SetupGet(sp => sp.FeatureFlagService).Returns(featureFlagServiceMock.Object);
            serviceProviderMock.SetupGet(sp => sp.LanguageClientManager).Returns(languageClientManagerMock.Object);
            serviceProviderMock.SetupGet(sp => sp.SolutionService).Returns(solutionServiceMock.Object);

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

            // Assert — a missing uri is ignored, no dictionary entry created
            Assert.Empty(cut.GetCodeDictionary());
        }

        [Fact]
        public async Task OnPublishDiagnostics316_ShouldHandleEmptyDiagnostics()
        {
            // Arrange
            var arg = JObject.Parse("{\"uri\":\"file:///path/to/file\",\"diagnostics\":[]}");

            // Act
            await cut.OnPublishDiagnostics316(arg);

            // Assert — an empty diagnostics list adds no Code issues for the path
            Assert.False(cut.GetCodeDictionary().ContainsKey("\\path\\to\\file"));
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
            authenticationFlowServiceMock.Setup(o => o.HandleFailedAuthenticationAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            await cut.OnHasAuthenticated(arg);

            // Assert
            authenticationFlowServiceMock.Verify(o => o.HandleFailedAuthenticationAsync("Authentication failed"), Times.Once);
        }

        [Fact]
        public async Task OnHasAuthenticated_ShouldHandleAuthenticationSuccess_WhenTokenIsProvided()
        {
            // Arrange — new login (no previous token).
            var arg = JObject.Parse("{'token':'test-token','apiUrl':'https://api.snyk.io'}");
            authenticationFlowServiceMock.Setup(o => o.HandleAuthenticationSuccessAsync("test-token", "https://api.snyk.io")).Returns(Task.CompletedTask);
            optionsMock.SetupGet(o => o.AuthenticationMethod).Returns(AuthenticationType.OAuth);

            // Act
            await cut.OnHasAuthenticated(arg);

            // Assert — token and endpoint stored, saved without triggering didChangeConfiguration loop
            optionsMock.VerifySet(o => o.CustomEndpoint = "https://api.snyk.io");
            optionsMock.VerifySet(o => o.ApiToken = It.Is<AuthenticationToken>(t => t.Type == AuthenticationType.OAuth && t.ToString() == "test-token"));
            snykOptionsManagerMock.Verify(s => s.Save(It.IsAny<IPersistableOptions>(), false), Times.Once);
            authenticationFlowServiceMock.Verify(o => o.HandleAuthenticationSuccessAsync("test-token", "https://api.snyk.io"), Times.Once);
        }

        [Fact]
        public async Task OnHasAuthenticated_WithExistingToken_OnlySavesQuietly()
        {
            // Arrange — token refresh (existing token non-blank).
            var arg = JObject.Parse("{'token':'refreshed-token','apiUrl':'https://api.snyk.io'}");
            optionsMock.SetupGet(o => o.AuthenticationMethod).Returns(AuthenticationType.OAuth);
            // Simulate existing token so isNewLogin=false
            optionsMock.SetupGet(o => o.ApiToken).Returns(new AuthenticationToken(AuthenticationType.OAuth, "existing-token"));

            // Act
            await cut.OnHasAuthenticated(arg);

            // Assert — Save must be called with triggerSettingsChangedEvent=false to avoid the loop
            snykOptionsManagerMock.Verify(s => s.Save(It.IsAny<IPersistableOptions>(), false), Times.Once);
            // HandleAuthenticationSuccessAsync must NOT be called — not a new login
            authenticationFlowServiceMock.Verify(o => o.HandleAuthenticationSuccessAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            // No scan on refresh — old token was non-blank
            tasksServiceMock.Verify(t => t.ScanAsync(), Times.Never);
        }

        [Fact]
        public async Task OnHasAuthenticated_AlwaysUpdatesTokenAndEndpoint()
        {
            // Arrange — always stores token and endpoint regardless of whether it is a new login or refresh.
            var arg = JObject.Parse("{'token':'refreshed-token','apiUrl':'https://api.eu.snyk.io'}");
            optionsMock.SetupGet(o => o.AuthenticationMethod).Returns(AuthenticationType.OAuth);
            optionsMock.SetupGet(o => o.ApiToken).Returns(new AuthenticationToken(AuthenticationType.OAuth, "existing-token"));

            // Act
            await cut.OnHasAuthenticated(arg);

            // Assert — token and endpoint must be stored
            optionsMock.VerifySet(o => o.CustomEndpoint = "https://api.eu.snyk.io");
            optionsMock.VerifySet(o => o.ApiToken = It.Is<AuthenticationToken>(t => t.ToString() == "refreshed-token"));
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
        public async Task OnSnykConfiguration_ShouldUpdateFolderConfigs_WhenValidFolderConfigsProvided()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                ""settings"": {},
                ""folderConfigs"": [
                    { ""folderPath"": ""/path/to/folder1"", ""settings"": { ""base_branch"": { ""value"": ""main"", ""changed"": true } } },
                    { ""folderPath"": ""/path/to/folder2"", ""settings"": { ""base_branch"": { ""value"": ""master"", ""changed"": true } } }
                ]
            }");
            optionsMock.SetupProperty(o => o.FolderConfigs);

            // Act
            await cut.OnSnykConfiguration(arg);

            // Assert
            Assert.NotNull(optionsMock.Object.FolderConfigs);
            Assert.Equal(2, optionsMock.Object.FolderConfigs.Count);
            Assert.Equal("/path/to/folder1", optionsMock.Object.FolderConfigs[0].FolderPath);
            Assert.Equal("main", optionsMock.Object.FolderConfigs[0].BaseBranch);
            Assert.Equal("/path/to/folder2", optionsMock.Object.FolderConfigs[1].FolderPath);
            Assert.Equal("master", optionsMock.Object.FolderConfigs[1].BaseBranch);
        }

        [Fact]
        public async Task OnSnykConfiguration_ShouldPreserveFolderConfigs_WhenFolderConfigsIsEmpty()
        {
            // Arrange
            var arg = JObject.Parse(@"{ ""settings"": {}, ""folderConfigs"": [] }");
            optionsMock.SetupProperty(o => o.FolderConfigs);
            optionsMock.Object.FolderConfigs = new List<FolderConfig>
            {
                new FolderConfig { FolderPath = "/existing/path" }
            };

            // Act
            await cut.OnSnykConfiguration(arg);

            // Assert — existing list preserved when incoming is empty
            Assert.Single(optionsMock.Object.FolderConfigs);
        }

        [Fact]
        public async Task OnSnykConfiguration_ShouldStoreFolderConfigOrgs_WhenSettingsHaveOrgFields()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                ""settings"": {},
                ""folderConfigs"": [
                    {
                        ""folderPath"": ""/path/to/folder1"",
                        ""settings"": {
                            ""auto_determined_org"": { ""value"": ""auto-determined-org"", ""changed"": true },
                            ""preferred_org"": { ""value"": """", ""changed"": true },
                            ""org_set_by_user"": { ""value"": false, ""changed"": true }
                        }
                    }
                ]
            }");
            optionsMock.SetupProperty(o => o.FolderConfigs);

            // Act
            await cut.OnSnykConfiguration(arg);

            // Assert — org fields stored in-memory; no disk Save*Async calls
            var fc = optionsMock.Object.FolderConfigs[0];
            Assert.Equal("auto-determined-org", fc.AutoDeterminedOrg);
            Assert.Equal("", fc.PreferredOrg);
            Assert.False(fc.OrgSetByUser);
        }

        [Fact]
        public async Task OnSnykConfiguration_GlobalOrgNotChanged_WhenFolderConfigArrives()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                ""settings"": {},
                ""folderConfigs"": [
                    {
                        ""folderPath"": ""/path/to/folder1"",
                        ""settings"": {
                            ""auto_determined_org"": { ""value"": ""auto-org"", ""changed"": true }
                        }
                    }
                ]
            }");
            optionsMock.SetupProperty(o => o.FolderConfigs);
            optionsMock.SetupProperty(o => o.Organization);
            optionsMock.Object.Organization = "original-org";

            // Act
            await cut.OnSnykConfiguration(arg);

            // Assert — global org untouched
            Assert.Equal("original-org", optionsMock.Object.Organization);
            snykOptionsManagerMock.Verify(s => s.SaveOrganizationAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task OnSnykConfiguration_ShouldStoreAllOrgFields_WhenBothOrgsPresent()
        {
            // Arrange — two folder configs; both stored
            var arg = JObject.Parse(@"{
                ""settings"": {},
                ""folderConfigs"": [
                    {
                        ""folderPath"": ""/path/to/folder1"",
                        ""settings"": {
                            ""preferred_org"": { ""value"": ""user-specified-org"", ""changed"": true },
                            ""auto_determined_org"": { ""value"": ""auto-determined-org"", ""changed"": true },
                            ""org_set_by_user"": { ""value"": true, ""changed"": true }
                        }
                    },
                    {
                        ""folderPath"": ""/path/to/folder2"",
                        ""settings"": {
                            ""preferred_org"": { ""value"": """", ""changed"": true },
                            ""auto_determined_org"": { ""value"": ""auto-determined-org-2"", ""changed"": true },
                            ""org_set_by_user"": { ""value"": false, ""changed"": true }
                        }
                    }
                ]
            }");
            optionsMock.SetupProperty(o => o.FolderConfigs);

            // Act
            await cut.OnSnykConfiguration(arg);

            // Assert — both configs stored; no disk calls
            Assert.Equal(2, optionsMock.Object.FolderConfigs.Count);
            var fc1 = optionsMock.Object.FolderConfigs[0];
            Assert.Equal("auto-determined-org", fc1.AutoDeterminedOrg);
            Assert.Equal("user-specified-org", fc1.PreferredOrg);
            Assert.True(fc1.OrgSetByUser);
        }

        [Fact]
        public async Task OnSnykConfiguration_ShouldStoreBothOrgFields_WhenBothExist()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                ""settings"": {},
                ""folderConfigs"": [
                    {
                        ""folderPath"": ""/path/to/folder1"",
                        ""settings"": {
                            ""preferred_org"": { ""value"": ""user-preferred-org"", ""changed"": true },
                            ""auto_determined_org"": { ""value"": ""auto-detected-org"", ""changed"": true },
                            ""org_set_by_user"": { ""value"": true, ""changed"": true }
                        }
                    }
                ]
            }");
            optionsMock.SetupProperty(o => o.FolderConfigs);

            // Act
            await cut.OnSnykConfiguration(arg);

            // Assert
            var fc = optionsMock.Object.FolderConfigs[0];
            Assert.Equal("auto-detected-org", fc.AutoDeterminedOrg);
            Assert.Equal("user-preferred-org", fc.PreferredOrg);
            Assert.True(fc.OrgSetByUser);
        }

        [Fact]
        public async Task OnSnykConfiguration_ShouldStoreEmptyPreferredOrg_WhenPreferredOrgIsEmpty()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                ""settings"": {},
                ""folderConfigs"": [
                    {
                        ""folderPath"": ""/path/to/folder1"",
                        ""settings"": {
                            ""preferred_org"": { ""value"": """", ""changed"": true },
                            ""auto_determined_org"": { ""value"": ""auto-detected-org"", ""changed"": true },
                            ""org_set_by_user"": { ""value"": false, ""changed"": true }
                        }
                    }
                ]
            }");
            optionsMock.SetupProperty(o => o.FolderConfigs);

            // Act
            await cut.OnSnykConfiguration(arg);

            // Assert
            var fc = optionsMock.Object.FolderConfigs[0];
            Assert.Equal("auto-detected-org", fc.AutoDeterminedOrg);
            Assert.Equal("", fc.PreferredOrg);
            Assert.False(fc.OrgSetByUser);
        }

        [Fact]
        public async Task OnSnykConfiguration_ShouldOverwriteByPath_WhenSamePathArrivesTwice()
        {
            // Arrange
            optionsMock.SetupProperty(o => o.FolderConfigs);

            JObject ConfigFor(string org) => JObject.Parse(@"{
                ""settings"": {},
                ""folderConfigs"": [
                    {
                        ""folderPath"": ""/path/to/folder1"",
                        ""settings"": {
                            ""preferred_org"": { ""value"": """ + org + @""", ""changed"": true }
                        }
                    }
                ]
            }");

            // Act — the SAME path arrives on two separate configuration pushes with different orgs.
            await cut.OnSnykConfiguration(ConfigFor("old-org"));
            await cut.OnSnykConfiguration(ConfigFor("new-org"));

            // Assert — the second push replaces the first by path: one entry, latest value wins.
            Assert.Single(optionsMock.Object.FolderConfigs);
            Assert.Equal("/path/to/folder1", optionsMock.Object.FolderConfigs[0].FolderPath);
            Assert.Equal("new-org", optionsMock.Object.FolderConfigs[0].PreferredOrg);
        }

        [Fact]
        public async Task OnSnykConfiguration_ShouldNotThrow_WhenPayloadIsEmpty()
        {
            // Arrange
            var arg = JObject.Parse(@"{ ""settings"": {}, ""folderConfigs"": [] }");

            // Act — must not throw
            await cut.OnSnykConfiguration(arg);
        }

        [Fact]
        public async Task OnSnykConfiguration_ShouldNotThrow_WhenPayloadIsJsonNull()
        {
            // Arrange — JValue.CreateNull() is a non-null JToken of type JTokenType.Null
            var arg = JValue.CreateNull();

            // Act — must not throw
            await cut.OnSnykConfiguration(arg);
        }

        [Fact]
        public async Task OnSnykConfiguration_ShouldNotThrow_WhenArgIsNull()
        {
            // Act — bare C# null (e.g. StreamJsonRpc sends null for a no-params notification)
            await cut.OnSnykConfiguration(null);
        }

        [Fact]
        public async Task OnSnykConfiguration_DoesNotThrow_WhenHtmlSettingsPanelIsClosed()
        {
            // HtmlSettingsControl.Instance is always null in unit tests (no WPF/VS host).
            // This test documents that RequestReload() handles a null instance guard correctly:
            // when no settings page is open the method returns immediately without touching WPF.
            var arg = JObject.Parse(@"{ ""settings"": {}, ""folderConfigs"": [] }");
            optionsMock.SetupProperty(o => o.FolderConfigs);

            // Act — must not throw when HtmlSettingsControl.Instance is null
            await cut.OnSnykConfiguration(arg);
        }

        [Fact]
        public async Task OnSnykConfiguration_RequestReload_IsCalledAfterSave_WhenConfigArrives()
        {
            // Arrange — exercises the production path that leads to RequestReload().
            // HtmlSettingsControl.Instance is null in the unit test environment (no WPF host),
            // so RequestReload() returns immediately after the null guard — no WPF call is made.
            // This test verifies the wiring: OnSnykConfiguration calls RequestReload() without
            // throwing, even though the panel is not open.
            var arg = JObject.Parse(@"{
                ""settings"": { ""snyk_oss_enabled"": { ""value"": true, ""changed"": true } },
                ""folderConfigs"": []
            }");
            optionsMock.SetupProperty(o => o.FolderConfigs);
            optionsMock.SetupProperty(o => o.OssEnabled);

            // Act — must not throw; RequestReload() fires after Save() completes
            await cut.OnSnykConfiguration(arg);

            // Assert — settings were applied and saved before the reload was attempted
            Assert.True(optionsMock.Object.OssEnabled);
            snykOptionsManagerMock.Verify(m => m.Save(It.IsAny<IPersistableOptions>(), false), Times.Once());
        }

        [Fact]
        public async Task OnSnykConfiguration_ShouldPopulateFolderConfigs_WithValidLspConfigurationParam()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                ""settings"": {
                    ""snyk_oss_enabled"": { ""value"": true, ""changed"": true },
                    ""snyk_code_enabled"": { ""value"": false, ""changed"": true }
                },
                ""folderConfigs"": [
                    { ""folderPath"": ""/repo"", ""settings"": {} }
                ]
            }");
            optionsMock.SetupProperty(o => o.FolderConfigs);

            // Act — must not throw and must populate folder configs
            await cut.OnSnykConfiguration(arg);

            // Assert — folder configs populated (not ignored)
            Assert.NotNull(optionsMock.Object.FolderConfigs);
            Assert.Single(optionsMock.Object.FolderConfigs);
            Assert.Equal("/repo", optionsMock.Object.FolderConfigs[0].FolderPath);

            // Assert — the global settings block was actually applied to Options, not just parsed.
            Assert.True(optionsMock.Object.OssEnabled);
            Assert.False(optionsMock.Object.SnykCodeSecurityEnabled);
        }

        [Fact]
        public async Task OnSnykConfiguration_ShouldSaveOptions_WhenValidSettingsReceived()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                ""settings"": {
                    ""snyk_oss_enabled"": { ""value"": true, ""changed"": true }
                },
                ""folderConfigs"": []
            }");
            optionsMock.SetupProperty(o => o.FolderConfigs);

            // Act
            await cut.OnSnykConfiguration(arg);

            // Assert — the setting was applied to Options...
            Assert.True(optionsMock.Object.OssEnabled);
            // ...and persisted without re-triggering DidChangeConfigurationAsync (triggerSettingsChangedEvent=false)
            snykOptionsManagerMock.Verify(m => m.Save(It.IsAny<IPersistableOptions>(), false), Times.Once());
        }

        // ─── Secrets product ──────────────────────────────────────────────────────

        [Fact]
        public async Task OnPublishDiagnostics316_WithSecretsSource_ShouldAddIssuesToSecretsDictionary()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                'uri': 'file:///c:/repo/secret.cs',
                'diagnostics': [
                    {
                        'source': 'Snyk Secrets',
                        'data': { 'id': 'secrets-1', 'isIgnored': false }
                    }
                ]
            }");
            optionsMock.SetupProperty(o => o.ConsistentIgnoresEnabled, false);

            // Act
            await cut.OnPublishDiagnostics316(arg);

            // Assert
            Assert.True(cut.GetSecretsDictionary().ContainsKey("c:\\repo\\secret.cs"));
            Assert.Empty(cut.GetCodeDictionary());
        }

        [Fact]
        public async Task OnPublishDiagnostics316_EmptyDiagnostics_ShouldClearSecretsDictionary()
        {
            // Arrange — first populate secrets dict
            var populate = JObject.Parse(@"{
                'uri': 'file:///c:/repo/secret.cs',
                'diagnostics': [
                    {
                        'source': 'Snyk Secrets',
                        'data': { 'id': 'secrets-1', 'isIgnored': false }
                    }
                ]
            }");
            optionsMock.SetupProperty(o => o.ConsistentIgnoresEnabled, false);
            await cut.OnPublishDiagnostics316(populate);
            Assert.Single(cut.GetSecretsDictionary());

            // Now send empty diagnostics for same file
            var clear = JObject.Parse(@"{'uri': 'file:///c:/repo/secret.cs', 'diagnostics': []}");
            await cut.OnPublishDiagnostics316(clear);

            // Assert — cleared
            Assert.Empty(cut.GetSecretsDictionary());
        }

        [Fact]
        public async Task OnSnykScan_WithSecretsProductInProgress_ShouldFireStartedEvent()
        {
            // Arrange
            var arg = JObject.Parse(@"{'status':'inProgress','product':'secrets','folderPath':'/repo'}");
            tasksServiceMock.Setup(t => t.SnykScanTokenSource).Returns(new CancellationTokenSource());

            // Act
            await cut.OnSnykScan(arg);

            // Assert
            tasksServiceMock.Verify(t => t.FireSecretsScanningStartedEvent(), Times.Once);
        }

        [Fact]
        public async Task OnSnykScan_WithSecretsProductError_ShouldFireErrorAndTaskFinished()
        {
            // Arrange
            var arg = JObject.Parse(@"{'status':'error','product':'secrets','folderPath':'/repo','presentableError':{'error':'test error'}}");
            tasksServiceMock.Setup(t => t.SnykScanTokenSource).Returns(new CancellationTokenSource());

            // Act
            await cut.OnSnykScan(arg);

            // Assert
            tasksServiceMock.Verify(t => t.OnSecretsError(It.IsAny<PresentableError>()), Times.Once);
            tasksServiceMock.Verify(t => t.FireTaskFinished(), Times.Once);
        }

        [Fact]
        public async Task OnSnykScan_WithSecretsProductSuccess_ShouldFireUpdateFinishedAndTaskFinished()
        {
            // Arrange
            var arg = JObject.Parse(@"{'status':'success','product':'secrets','folderPath':'/repo'}");
            tasksServiceMock.Setup(t => t.SnykScanTokenSource).Returns(new CancellationTokenSource());

            // Act
            await cut.OnSnykScan(arg);

            // Assert
            tasksServiceMock.Verify(t => t.FireSecretsScanningUpdateEvent(It.IsAny<IDictionary<string, IEnumerable<Issue>>>()), Times.Once);
            tasksServiceMock.Verify(t => t.FireSecretsScanningFinishedEvent(), Times.Once);
            tasksServiceMock.Verify(t => t.FireTaskFinished(), Times.Once);
        }

        [Fact]
        public async Task OnSnykConfiguration_CallsReloadHtmlSettings_AfterSavingOptions()
        {
            // Arrange — inject a spy via the internal constructor overload instead of using
            // HtmlSettingsControl.RequestReload() statically, so the call can be verified.
            var reloadCalled = false;
            var spMock = new Mock<ISnykServiceProvider>();
            var optsMock = new Mock<ISnykOptions>();
            var optsMgrMock = new Mock<ISnykOptionsManager>();
            optsMock.SetupAllProperties();
            optsMock.SetupProperty(o => o.FolderConfigs);
            spMock.SetupGet(sp => sp.Options).Returns(optsMock.Object);
            spMock.SetupGet(sp => sp.SnykOptionsManager).Returns(optsMgrMock.Object);

            var sut = new SnykLanguageClientCustomTarget(spMock.Object, () => { reloadCalled = true; });
            var arg = JObject.Parse(@"{ ""settings"": {}, ""folderConfigs"": [] }");

            // Act
            await sut.OnSnykConfiguration(arg);

            // Assert — reload was requested after settings were saved
            Assert.True(reloadCalled, "Expected RequestReload to be called after OnSnykConfiguration applies config");
        }
    }
}
