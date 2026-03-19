using System;
using System.Collections.Generic;
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
    }
}
