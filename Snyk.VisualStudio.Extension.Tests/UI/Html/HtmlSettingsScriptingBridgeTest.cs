using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Newtonsoft.Json;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    [Collection(MockedVS.Collection)]
    public class HtmlSettingsScriptingBridgeTest : PackageBaseTest
    {
        private readonly Mock<ISnykOptions> optionsMock;
        private readonly Mock<ISnykOptionsManager> snykOptionsManagerMock;
        private readonly HtmlSettingsScriptingBridge bridge;

        public HtmlSettingsScriptingBridgeTest(GlobalServiceProvider gsp) : base(gsp)
        {
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            optionsMock = new Mock<ISnykOptions>();
            snykOptionsManagerMock = new Mock<ISnykOptionsManager>();

            serviceProviderMock.SetupGet(sp => sp.Options).Returns(optionsMock.Object);
            serviceProviderMock.SetupGet(sp => sp.SnykOptionsManager).Returns(snykOptionsManagerMock.Object);
            serviceProviderMock.SetupGet(sp => sp.LanguageClientManager).Returns((ILanguageClientManager)null);

            bridge = new HtmlSettingsScriptingBridge(
                serviceProviderMock.Object,
                onModified: () => { });
        }

        [Fact]
        public void IdeExecuteCommand_SnykLogin_SavesOAuthMethod()
        {
            var args = JsonConvert.SerializeObject(new object[] { "oauth", "https://api.snyk.io", false });

            bridge.__ideExecuteCommand__("snyk.login", args, "");

            optionsMock.VerifySet(o => o.AuthenticationMethod = AuthenticationType.OAuth);
        }

        [Fact]
        public void IdeExecuteCommand_SnykLogin_SavesPatMethod()
        {
            var args = JsonConvert.SerializeObject(new object[] { "pat", "https://api.snyk.io", false });

            bridge.__ideExecuteCommand__("snyk.login", args, "");

            optionsMock.VerifySet(o => o.AuthenticationMethod = AuthenticationType.Pat);
        }

        [Fact]
        public void IdeExecuteCommand_SnykLogin_SavesTokenMethod()
        {
            var args = JsonConvert.SerializeObject(new object[] { "token", "https://api.snyk.io", false });

            bridge.__ideExecuteCommand__("snyk.login", args, "");

            optionsMock.VerifySet(o => o.AuthenticationMethod = AuthenticationType.Token);
        }

        [Theory]
        [InlineData("unknown")]
        [InlineData("")]
        [InlineData(" ")]
        public void IdeExecuteCommand_SnykLogin_DefaultsToOAuthForInvalidMethod(string authMethod)
        {
            var args = JsonConvert.SerializeObject(new object[] { authMethod, "https://api.snyk.io", false });

            bridge.__ideExecuteCommand__("snyk.login", args, "");

            optionsMock.VerifySet(o => o.AuthenticationMethod = AuthenticationType.OAuth);
        }

        [Fact]
        public void IdeExecuteCommand_SnykLogin_SavesEndpoint()
        {
            var args = JsonConvert.SerializeObject(new object[] { "oauth", "https://api.eu.snyk.io", false });

            bridge.__ideExecuteCommand__("snyk.login", args, "");

            optionsMock.VerifySet(o => o.CustomEndpoint = "https://api.eu.snyk.io");
        }

        [Fact]
        public void IdeExecuteCommand_SnykLogin_SavesInsecure()
        {
            var args = JsonConvert.SerializeObject(new object[] { "oauth", "https://api.snyk.io", true });

            bridge.__ideExecuteCommand__("snyk.login", args, "");

            optionsMock.VerifySet(o => o.IgnoreUnknownCA = true);
        }

        [Fact]
        public void IdeExecuteCommand_SnykLogin_NoSaveWhenFewerThan3Args()
        {
            var args = JsonConvert.SerializeObject(new object[] { "oauth" });

            bridge.__ideExecuteCommand__("snyk.login", args, "");

            optionsMock.VerifySet(o => o.AuthenticationMethod = It.IsAny<AuthenticationType>(), Times.Never);
            optionsMock.VerifySet(o => o.CustomEndpoint = It.IsAny<string>(), Times.Never);
        }

        [Fact]
        public void IdeExecuteCommand_OtherCommand_DoesNotSaveAuthParams()
        {
            var args = JsonConvert.SerializeObject(new object[] { });

            bridge.__ideExecuteCommand__("snyk.logout", args, "");

            optionsMock.VerifySet(o => o.AuthenticationMethod = It.IsAny<AuthenticationType>(), Times.Never);
            optionsMock.VerifySet(o => o.CustomEndpoint = It.IsAny<string>(), Times.Never);
            optionsMock.VerifySet(o => o.IgnoreUnknownCA = It.IsAny<bool>(), Times.Never);
        }

        [Fact]
        public void SaveIdeConfig_ClearsToken_WhenAuthMethodChanges()
        {
            var setApiTokenCalls = new List<AuthenticationToken>();

            optionsMock.SetupGet(o => o.AuthenticationMethod).Returns(AuthenticationType.OAuth);
            optionsMock.SetupSet(o => o.AuthenticationMethod = It.IsAny<AuthenticationType>())
                .Callback<AuthenticationType>(v => optionsMock.SetupGet(o => o.AuthenticationMethod).Returns(v));
            optionsMock.SetupGet(o => o.ApiToken)
                .Returns(new AuthenticationToken(AuthenticationType.OAuth, "existing-oauth-token"));
            optionsMock.SetupSet(o => o.ApiToken = It.IsAny<AuthenticationToken>())
                .Callback<AuthenticationToken>(t => setApiTokenCalls.Add(t));

            var config = JsonConvert.SerializeObject(new IdeConfigData
            {
                AuthenticationMethod = "token",
                Token = "new-pat-token",
            });

            bridge.__saveIdeConfig__(config);

            Assert.True(setApiTokenCalls.Count >= 1, "ApiToken setter should have been called");
            Assert.Equal(string.Empty, setApiTokenCalls[0].ToString());
        }

        [Fact]
        public void SaveCompletion_IsInitiallyIncomplete()
        {
            Assert.False(bridge.SaveCompletion.IsCompleted);
        }

        [Fact]
        public void SaveCompletion_CompletesAfterSaveIdeConfig()
        {
            Assert.False(bridge.SaveCompletion.IsCompleted);

            bridge.__saveIdeConfig__("{}");

            Assert.True(bridge.SaveCompletion.IsCompleted);
        }

        [Fact]
        public void BeginSave_ResetsToNewIncompleteTask()
        {
            bridge.__saveIdeConfig__("{}");
            var firstCompletion = bridge.SaveCompletion;
            Assert.True(firstCompletion.IsCompleted);

            bridge.BeginSave();

            Assert.NotSame(firstCompletion, bridge.SaveCompletion);
            Assert.False(bridge.SaveCompletion.IsCompleted);
        }

        [Fact]
        public void SaveIdeConfig_DoesNotClearToken_WhenAuthMethodUnchanged()
        {
            var setApiTokenCalls = new List<AuthenticationToken>();

            optionsMock.SetupGet(o => o.AuthenticationMethod).Returns(AuthenticationType.OAuth);
            optionsMock.SetupGet(o => o.ApiToken)
                .Returns(new AuthenticationToken(AuthenticationType.OAuth, "existing-token"));
            optionsMock.SetupSet(o => o.ApiToken = It.IsAny<AuthenticationToken>())
                .Callback<AuthenticationToken>(t => setApiTokenCalls.Add(t));

            var config = JsonConvert.SerializeObject(new IdeConfigData
            {
                AuthenticationMethod = "oauth",
                Token = "new-oauth-token",
            });

            bridge.__saveIdeConfig__(config);

            // No clear-token call; any ApiToken set should be the new value, not empty
            Assert.DoesNotContain(setApiTokenCalls, t => t.ToString() == string.Empty);
        }

        [Fact]
        public void SaveIdeConfig_SetsOssEnabled_FromActivateSnykOpenSource()
        {
            var config = JsonConvert.SerializeObject(new IdeConfigData
            {
                ActivateSnykOpenSource = true,
            });

            bridge.__saveIdeConfig__(config);

            optionsMock.VerifySet(o => o.OssEnabled = true);
        }

        [Fact]
        public void SaveIdeConfig_SetsSecretsEnabled_FromSnakeCaseKey()
        {
            var config = JsonConvert.SerializeObject(new { snyk_secrets_enabled = true });

            bridge.__saveIdeConfig__(config);

            optionsMock.VerifySet(o => o.SecretsEnabled = true);
        }

        [Fact]
        public void SaveIdeConfig_SetsSecretsEnabled_FromCamelCaseKey()
        {
            var config = JsonConvert.SerializeObject(new { activateSnykSecrets = false });

            bridge.__saveIdeConfig__(config);

            optionsMock.VerifySet(o => o.SecretsEnabled = false);
        }

        [Fact]
        public void SaveIdeConfig_SetsFilterSeverity()
        {
            var config = JsonConvert.SerializeObject(new IdeConfigData
            {
                FilterSeverity = new FilterSeverity
                {
                    Critical = true,
                    High = false,
                    Medium = true,
                    Low = false,
                },
            });

            bridge.__saveIdeConfig__(config);

            optionsMock.VerifySet(o => o.FilterCritical = true);
            optionsMock.VerifySet(o => o.FilterHigh = false);
            optionsMock.VerifySet(o => o.FilterMedium = true);
            optionsMock.VerifySet(o => o.FilterLow = false);
        }

        [Fact]
        public void SaveIdeConfig_PartialPayload_DoesNotTouchUnprovidedSettings()
        {
            var config = JsonConvert.SerializeObject(new IdeConfigData
            {
                ActivateSnykOpenSource = true,
                // SecretsEnabled and SnykCodeSecurityEnabled are NOT provided
            });

            bridge.__saveIdeConfig__(config);

            optionsMock.VerifySet(o => o.OssEnabled = true);
            optionsMock.VerifySet(o => o.SecretsEnabled = It.IsAny<bool>(), Times.Never);
            optionsMock.VerifySet(o => o.SnykCodeSecurityEnabled = It.IsAny<bool>(), Times.Never);
        }
    }

    [Collection(MockedVS.Collection)]
    public class HtmlSettingsScriptingBridgeCliRestartTest : PackageBaseTest
    {
        private readonly Mock<ISnykOptions> optionsMock;
        private readonly Mock<ISnykOptionsManager> snykOptionsManagerMock;
        private readonly Mock<ISnykTasksService> tasksServiceMock;
        private readonly HtmlSettingsScriptingBridge bridge;

        public HtmlSettingsScriptingBridgeCliRestartTest(GlobalServiceProvider gsp) : base(gsp)
        {
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            optionsMock = new Mock<ISnykOptions>();
            snykOptionsManagerMock = new Mock<ISnykOptionsManager>();
            tasksServiceMock = new Mock<ISnykTasksService>();

            // BinariesAutoUpdate=true so HandleCliChange goes through TasksService.Download
            // and does not touch the static LanguageClientHelper (which has no LS in unit tests).
            optionsMock.SetupGet(o => o.BinariesAutoUpdate).Returns(true);
            optionsMock.SetupGet(o => o.CliCustomPath).Returns(string.Empty);

            serviceProviderMock.SetupGet(sp => sp.Options).Returns(optionsMock.Object);
            serviceProviderMock.SetupGet(sp => sp.SnykOptionsManager).Returns(snykOptionsManagerMock.Object);
            serviceProviderMock.SetupGet(sp => sp.TasksService).Returns(tasksServiceMock.Object);
            serviceProviderMock.SetupGet(sp => sp.LanguageClientManager).Returns((ILanguageClientManager)null);

            bridge = new HtmlSettingsScriptingBridge(
                serviceProviderMock.Object,
                onModified: () => { });
        }

        [Fact]
        public void SaveIdeConfig_CliPathChanged_TriggersCliChangeHandling()
        {
            var config = JsonConvert.SerializeObject(new IdeConfigData
            {
                CliPath = "/new/cli/path/snyk",
            });

            bridge.__saveIdeConfig__(config);

            tasksServiceMock.Verify(t => t.CancelTasks(), Times.Once);
            tasksServiceMock.Verify(t => t.Download(null), Times.Once);
        }

        [Fact]
        public void SaveIdeConfig_CliPathUnchanged_DoesNotTriggerCliChangeHandling()
        {
            optionsMock.SetupGet(o => o.CliCustomPath).Returns("/existing/path/snyk");

            var config = JsonConvert.SerializeObject(new IdeConfigData
            {
                CliPath = "/existing/path/snyk",
            });

            bridge.__saveIdeConfig__(config);

            tasksServiceMock.Verify(t => t.CancelTasks(), Times.Never);
            tasksServiceMock.Verify(t => t.Download(null), Times.Never);
        }

        [Fact]
        public void SaveIdeConfig_NoCliFieldsInPayload_DoesNotTriggerCliChangeHandling()
        {
            var config = JsonConvert.SerializeObject(new IdeConfigData
            {
                ActivateSnykOpenSource = true,
            });

            bridge.__saveIdeConfig__(config);

            tasksServiceMock.Verify(t => t.CancelTasks(), Times.Never);
            tasksServiceMock.Verify(t => t.Download(null), Times.Never);
        }
    }

    [Collection(MockedVS.Collection)]
    public class HtmlSettingsScriptingBridgeFolderConfigTest : PackageBaseTest
    {
        private readonly Mock<ISnykOptions> optionsMock;
        private readonly Mock<ISnykOptionsManager> snykOptionsManagerMock;
        private readonly Mock<ISolutionService> solutionServiceMock;
        private readonly HtmlSettingsScriptingBridge bridge;

        public HtmlSettingsScriptingBridgeFolderConfigTest(GlobalServiceProvider gsp) : base(gsp)
        {
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            optionsMock = new Mock<ISnykOptions>();
            snykOptionsManagerMock = new Mock<ISnykOptionsManager>();
            solutionServiceMock = new Mock<ISolutionService>();

            optionsMock.SetupAllProperties();
            optionsMock.SetupGet(o => o.FolderConfigs).Returns((List<FolderConfig>)null);

            serviceProviderMock.SetupGet(sp => sp.Options).Returns(optionsMock.Object);
            serviceProviderMock.SetupGet(sp => sp.SnykOptionsManager).Returns(snykOptionsManagerMock.Object);
            serviceProviderMock.SetupGet(sp => sp.LanguageClientManager).Returns((ILanguageClientManager)null);
            serviceProviderMock.SetupGet(sp => sp.SolutionService).Returns(solutionServiceMock.Object);

            solutionServiceMock.Setup(s => s.GetSolutionFolderAsync())
                .ReturnsAsync("/path/to/solution");

            snykOptionsManagerMock.Setup(m => m.SavePreferredOrgAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            snykOptionsManagerMock.Setup(m => m.SaveAutoDeterminedOrgAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            snykOptionsManagerMock.Setup(m => m.SaveAdditionalEnvAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            snykOptionsManagerMock.Setup(m => m.SaveOrgSetByUserAsync(It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            bridge = new HtmlSettingsScriptingBridge(
                serviceProviderMock.Object,
                onModified: () => { });
        }

        [Fact]
        public void SaveIdeConfig_FolderConfigs_SavesAllFolderProperties()
        {
            var config = JsonConvert.SerializeObject(new IdeConfigData
            {
                FolderConfigs = new List<FolderConfigData>
                {
                    new FolderConfigData
                    {
                        PreferredOrg = "my-org",
                        OrgSetByUser = true,
                        AdditionalParameters = new List<string>(),
                        AdditionalEnv = "ENV_VAR=1",
                        AutoDeterminedOrg = "auto-org",
                    },
                },
            });

            bridge.__saveIdeConfig__(config);

            snykOptionsManagerMock.Verify(m => m.SavePreferredOrgAsync("my-org"), Times.Once);
            snykOptionsManagerMock.Verify(m => m.SaveOrgSetByUserAsync(true), Times.Once);
            snykOptionsManagerMock.Verify(m => m.SaveAdditionalEnvAsync("ENV_VAR=1"), Times.Once);
            snykOptionsManagerMock.Verify(m => m.SaveAutoDeterminedOrgAsync("auto-org"), Times.Once);
        }
    }
}
