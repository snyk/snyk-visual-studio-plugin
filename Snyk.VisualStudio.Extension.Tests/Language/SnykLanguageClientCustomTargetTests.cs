using System;
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
using Snyk.VisualStudio.Extension.UI.Toolwindow;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    [Collection(MockedVS.Collection)]
    public class SnykLanguageClientCustomTargetTests : PackageBaseTest
    {
        private readonly Mock<ISnykServiceProvider> serviceProviderMock;
        private readonly Mock<ISnykTasksService> tasksServiceMock;
        private readonly Mock<ISnykOptions> optionsMock;
        private readonly Mock<ISnykOptionsManager> snykOptionsManagerMock;
        private readonly Mock<ILanguageClientManager> languageClientManagerMock;
        private readonly Mock<IAuthenticationFlowService> authenticationFlowServiceMock;
        private readonly Mock<ISolutionService> solutionServiceMock;
        private readonly SnykLanguageClientCustomTarget cut;

        public SnykLanguageClientCustomTargetTests(GlobalServiceProvider gsp) : base(gsp)
        {
            serviceProviderMock = new Mock<ISnykServiceProvider>();
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
        public async Task OnPublishDiagnostics316_ShouldNotThrow_WhenUriMissing()
        {
            // Arrange — a missing uri is ignored. Rendering is driven by the LS HTML tree now, so
            // the handler only inspects diagnostics for the Consistent Ignores flag.
            var arg = JObject.Parse("{\"diagnostics\":[]}");

            // Act — must not throw
            await cut.OnPublishDiagnostics316(arg);

            // Assert — no ignored issue seen, flag left alone
            Assert.False(optionsMock.Object.ConsistentIgnoresEnabled);
        }

        [Fact]
        public async Task OnPublishDiagnostics316_ShouldNotThrow_WhenDiagnosticsEmpty()
        {
            // Arrange
            var arg = JObject.Parse("{\"uri\":\"file:///path/to/file\",\"diagnostics\":[]}");

            // Act — must not throw
            await cut.OnPublishDiagnostics316(arg);

            // Assert
            Assert.False(optionsMock.Object.ConsistentIgnoresEnabled);
        }

        [Fact]
        public async Task OnPublishDiagnostics316_ShouldNotEnableConsistentIgnores_WhenNoIssueIgnored()
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

            // Assert — ConsistentIgnoresEnabled stays false when no issue is ignored
            Assert.False(optionsMock.Object.ConsistentIgnoresEnabled);
        }

        [Fact]
        public async Task OnPublishDiagnostics316_ShouldEnableConsistentIgnores_WhenAnyIssueIgnored()
        {
            // Arrange
            var arg = JObject.Parse(@"{
                'uri': 'file:///path/to/file',
                'diagnostics': [
                    {
                        'source': 'Snyk Code',
                        'data': { 'id': 'issue1', 'isIgnored': false }
                    },
                    {
                        'source': 'Snyk Code',
                        'data': { 'id': 'issue2', 'isIgnored': true }
                    }
                ]
            }");

            optionsMock.SetupProperty(o => o.ConsistentIgnoresEnabled, false);

            // Act
            await cut.OnPublishDiagnostics316(arg);

            // Assert — an ignored issue flips the Consistent Ignores flag on
            Assert.True(optionsMock.Object.ConsistentIgnoresEnabled);
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
        public async Task OnTreeView_ShouldNotThrow_WhenHtmlMissing()
        {
            // Arrange — payload without treeViewHtml is ignored (no ToolWindow access).
            var arg = JObject.Parse("{'totalIssues':0}");

            // Act — must not throw
            await cut.OnTreeView(arg);
        }

        [Fact]
        public async Task OnTreeView_ShouldNotThrow_WhenToolWindowUnavailable()
        {
            // Arrange — valid payload, but ToolWindow is not set up on the mock (returns null),
            // so the handler must guard and return without throwing.
            var arg = JObject.Parse("{'treeViewHtml':'<html></html>','totalIssues':3}");

            // Act — must not throw
            await cut.OnTreeView(arg);
        }

        [Fact]
        public async Task OnTreeView_ShouldNotThrow_WhenArgIsNull()
        {
            // Act — bare C# null (StreamJsonRpc may send null for a no-params notification)
            await cut.OnTreeView(null);
        }

        [Fact]
        public async Task OnTreeView_ShouldRenderHtmlAndCountTogether_WhenPayloadValid()
        {
            // Arrange — valid payload + a wired tool window/tree panel.
            var treePanelMock = new Mock<ITreeHtmlPanel>();
            var toolWindowMock = new Mock<ISnykToolWindow>();
            toolWindowMock.SetupGet(t => t.TreeHtmlPanel).Returns(treePanelMock.Object);
            serviceProviderMock.SetupGet(sp => sp.ToolWindow).Returns(toolWindowMock.Object);

            var arg = JObject.Parse("{'treeViewHtml':'<html>tree</html>','totalIssues':7}");

            // Act
            await cut.OnTreeView(arg);

            // Assert — HTML and count are applied together in a single SetContent call.
            treePanelMock.Verify(p => p.SetContent("<html>tree</html>", 7), Times.Once);
        }

        [Fact]
        public async Task OnShowDocument_ShouldSelectIssue_WhenDetailPanelRequestWithCodename()
        {
            var toolWindowMock = SetupToolWindow();
            var arg = ShowDocument("snyk://x?action=showInDetailPanel&issueId=ISSUE1&product=code");

            await cut.OnShowDocument(arg);

            toolWindowMock.Verify(t => t.SelectedItemInTree("ISSUE1", "code"), Times.Once);
        }

        [Fact]
        public async Task OnShowDocument_ShouldNormalizeDisplayNameToCodename_ForDetailPanel()
        {
            var toolWindowMock = SetupToolWindow();
            // "Snyk+Code" — NormalizeProduct maps the "+"-encoded display name to the codename.
            var arg = ShowDocument("snyk://x?action=showInDetailPanel&issueId=ISSUE1&product=Snyk+Code");

            await cut.OnShowDocument(arg);

            toolWindowMock.Verify(t => t.SelectedItemInTree("ISSUE1", "code"), Times.Once);
        }

        [Fact]
        public async Task OnShowDocument_ShouldNotSelectIssue_WhenIssueIdMissing()
        {
            var toolWindowMock = SetupToolWindow();
            var arg = ShowDocument("snyk://x?action=showInDetailPanel&product=code");

            await cut.OnShowDocument(arg);

            toolWindowMock.Verify(t => t.SelectedItemInTree(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task OnShowDocument_ShouldNotSelectIssue_WhenProductMissing()
        {
            var toolWindowMock = SetupToolWindow();
            var arg = ShowDocument("snyk://x?action=showInDetailPanel&issueId=ISSUE1");

            await cut.OnShowDocument(arg);

            toolWindowMock.Verify(t => t.SelectedItemInTree(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task OnShowDocument_ShouldReturn_WhenExternalRequest()
        {
            var toolWindowMock = SetupToolWindow();
            // Non-detail action + External=true: an open-in-browser request, not an editor nav.
            var arg = ShowDocument("snyk://x?action=open", external: true);

            // Must not throw (would NRE if it reached VsCodeService) and must not select an issue.
            await cut.OnShowDocument(arg);

            toolWindowMock.Verify(t => t.SelectedItemInTree(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task OnShowDocument_ShouldReturn_WhenSelectionMissing()
        {
            var toolWindowMock = SetupToolWindow();
            // Plain file-open request with no selection range: nothing to navigate to.
            var arg = ShowDocument("snyk://x");

            await cut.OnShowDocument(arg);

            toolWindowMock.Verify(t => t.SelectedItemInTree(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task OnShowDocument_ShouldNotThrow_WhenUriEmpty()
        {
            var arg = JObject.Parse("{'uri':''}");

            await cut.OnShowDocument(arg);
        }

        [Fact]
        public async Task OnShowDocument_ShouldKeepIssueId_WhenValueContainsEquals()
        {
            // base64-ish issue IDs can end in '='/'=='. The pair must not be dropped, and the value
            // must keep its trailing '=' characters.
            var toolWindowMock = SetupToolWindow();
            var arg = ShowDocument("snyk://x?action=showInDetailPanel&issueId=abc==&product=code");

            await cut.OnShowDocument(arg);

            toolWindowMock.Verify(t => t.SelectedItemInTree("abc==", "code"), Times.Once);
        }

        [Fact]
        public async Task OnShowDocument_ShouldDecodeIssueIdOnce_WhenPercentEncoded()
        {
            // The issue id is unescaped exactly once. Double-unescaping would corrupt a value whose
            // decoded form contains a reserved character (here '%26' would wrongly become '&').
            var toolWindowMock = SetupToolWindow();
            var arg = ShowDocument("snyk://x?action=showInDetailPanel&issueId=x%2526y&product=code");

            await cut.OnShowDocument(arg);

            // Exactly one unescape yields "x%26y"; the buggy double-unescape would yield "x&y".
            // Assert "not double-decoded" rather than the exact form to stay robust against
            // System.Uri query canonicalisation differences.
            toolWindowMock.Verify(t => t.SelectedItemInTree(It.Is<string>(id => id != "x&y"), "code"), Times.Once);
        }

        [Fact]
        public async Task OnShowDocument_ShouldNotThrow_WhenToolWindowUnavailableForDetailPanel()
        {
            // ToolWindow is null until OnToolWindowCreated runs; a tree click arriving before then
            // must be a no-op rather than an NRE out of the JSON-RPC handler.
            var arg = ShowDocument("snyk://x?action=showInDetailPanel&issueId=ISSUE1&product=code");

            await cut.OnShowDocument(arg);
        }

        private Mock<ISnykToolWindow> SetupToolWindow()
        {
            var toolWindowMock = new Mock<ISnykToolWindow>();
            serviceProviderMock.SetupGet(sp => sp.ToolWindow).Returns(toolWindowMock.Object);
            return toolWindowMock;
        }

        private static JObject ShowDocument(string uri, bool external = false) =>
            JObject.FromObject(new { uri, external });

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
            // and without updating the override tracker (LS-delivered auth result, not a user settings-edit).
            optionsMock.VerifySet(o => o.CustomEndpoint = "https://api.snyk.io");
            optionsMock.VerifySet(o => o.ApiToken = It.Is<AuthenticationToken>(t => t.Type == AuthenticationType.OAuth && t.ToString() == "test-token"));
            snykOptionsManagerMock.Verify(s => s.Save(It.IsAny<IPersistableOptions>(), false, false, null, null), Times.Once);
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

            // Assert — Save must be called with triggerSettingsChangedEvent=false (avoid loop) and
            // updateOverrideTracker=false (LS-delivered auth, not a user settings-edit).
            snykOptionsManagerMock.Verify(s => s.Save(It.IsAny<IPersistableOptions>(), false, false, null, null), Times.Once);
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
            // updateOverrideTracker:false — LS is pushing trusted-folder set back; must not record as user override.
            snykOptionsManagerMock.Verify(s => s.Save(It.IsAny<IPersistableOptions>(), false, false, It.IsAny<System.Collections.Generic.IReadOnlyCollection<string>>(), It.IsAny<System.Collections.Generic.IReadOnlyCollection<string>>()), Times.Once);
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
            Assert.Equal("main", optionsMock.Object.FolderConfigs[0].GetString(PflagKeys.BaseBranch));
            Assert.Equal("/path/to/folder2", optionsMock.Object.FolderConfigs[1].FolderPath);
            Assert.Equal("master", optionsMock.Object.FolderConfigs[1].GetString(PflagKeys.BaseBranch));
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
            Assert.Equal("auto-determined-org", fc.GetString(PflagKeys.AutoDeterminedOrg));
            Assert.Equal("", fc.GetString(PflagKeys.PreferredOrg));
            Assert.False(Convert.ToBoolean(fc.Settings[PflagKeys.OrgSetByUser].Value));
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
            Assert.Equal("auto-determined-org", fc1.GetString(PflagKeys.AutoDeterminedOrg));
            Assert.Equal("user-specified-org", fc1.GetString(PflagKeys.PreferredOrg));
            Assert.True(Convert.ToBoolean(fc1.Settings[PflagKeys.OrgSetByUser].Value));
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
            Assert.Equal("auto-detected-org", fc.GetString(PflagKeys.AutoDeterminedOrg));
            Assert.Equal("user-preferred-org", fc.GetString(PflagKeys.PreferredOrg));
            Assert.True(Convert.ToBoolean(fc.Settings[PflagKeys.OrgSetByUser].Value));
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
            Assert.Equal("auto-detected-org", fc.GetString(PflagKeys.AutoDeterminedOrg));
            Assert.Equal("", fc.GetString(PflagKeys.PreferredOrg));
            Assert.False(Convert.ToBoolean(fc.Settings[PflagKeys.OrgSetByUser].Value));
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
            Assert.Equal("new-org", optionsMock.Object.FolderConfigs[0].GetString(PflagKeys.PreferredOrg));
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
            // updateOverrideTracker:false — LS-pushed values must never be recorded as user overrides (IDE-2152).
            snykOptionsManagerMock.Verify(m => m.Save(It.IsAny<IPersistableOptions>(), false, false, It.IsAny<System.Collections.Generic.IReadOnlyCollection<string>>(), It.IsAny<System.Collections.Generic.IReadOnlyCollection<string>>()), Times.Once());
        }

        [Fact]
        public async Task OnPublishDiagnostics316_ShouldNotThrow_WhenDiagnosticDataIsNonDeserializable()
        {
            // Arrange — `data` is present but cannot be deserialized as Issue (it is a plain integer).
            // TryParse<Issue> returns null in this case; the handler must skip the entry rather than
            // throwing NullReferenceException on `issue.IsIgnored`.
            var arg = JObject.Parse(@"{
                'uri': 'file:///path/to/file',
                'diagnostics': [
                    {
                        'source': 'Snyk Code',
                        'data': 42
                    }
                ]
            }");
            optionsMock.SetupProperty(o => o.ConsistentIgnoresEnabled, false);

            // Act — must not throw
            await cut.OnPublishDiagnostics316(arg);

            // Assert — no valid issue parsed, flag stays false
            Assert.False(optionsMock.Object.ConsistentIgnoresEnabled);
        }

        [Fact]
        public async Task OnPublishDiagnostics316_ShouldNotThrow_WhenDiagnosticDataIsNonDeserializableString()
        {
            // Arrange — `data` is a bare string, not an Issue object.
            var arg = JObject.Parse(@"{
                'uri': 'file:///path/to/file',
                'diagnostics': [
                    {
                        'source': 'Snyk Code',
                        'data': 'not-an-issue'
                    }
                ]
            }");
            optionsMock.SetupProperty(o => o.ConsistentIgnoresEnabled, false);

            // Act — must not throw
            await cut.OnPublishDiagnostics316(arg);

            // Assert
            Assert.False(optionsMock.Object.ConsistentIgnoresEnabled);
        }

        // ─── Secrets product ──────────────────────────────────────────────────────

        [Fact]
        public async Task OnPublishDiagnostics316_WithSecretsSource_ShouldNotEnableConsistentIgnores_WhenNoIssueIgnored()
        {
            // Arrange — rendering is driven by the LS HTML tree now, so the handler only inspects
            // Secrets diagnostics for the Consistent Ignores flag.
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

            // Assert — no ignored issue seen, flag left alone
            Assert.False(optionsMock.Object.ConsistentIgnoresEnabled);
        }

        [Fact]
        public async Task OnPublishDiagnostics316_WithSecretsSource_ShouldEnableConsistentIgnores_WhenIssueIgnored()
        {
            // Arrange — an ignored Secrets issue flips the Consistent Ignores flag on.
            var arg = JObject.Parse(@"{
                'uri': 'file:///c:/repo/secret.cs',
                'diagnostics': [
                    {
                        'source': 'Snyk Secrets',
                        'data': { 'id': 'secrets-1', 'isIgnored': true }
                    }
                ]
            }");
            optionsMock.SetupProperty(o => o.ConsistentIgnoresEnabled, false);

            // Act
            await cut.OnPublishDiagnostics316(arg);

            // Assert
            Assert.True(optionsMock.Object.ConsistentIgnoresEnabled);
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
        public async Task OnSnykScan_WithSecretsProductSuccess_ShouldFireFinishedAndTaskFinished()
        {
            // Arrange
            var arg = JObject.Parse(@"{'status':'success','product':'secrets','folderPath':'/repo'}");
            tasksServiceMock.Setup(t => t.SnykScanTokenSource).Returns(new CancellationTokenSource());

            // Act
            await cut.OnSnykScan(arg);

            // Assert
            tasksServiceMock.Verify(t => t.FireSecretsScanningFinishedEvent(), Times.Once);
            tasksServiceMock.Verify(t => t.FireTaskFinished(), Times.Once);
        }
    }
}
