using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        public void Dispatcher_RoutesRawMessageToBridge_EndToEnd()
        {
            // Exercise the full route the WebView2 host uses — raw {method,args} JSON → dispatcher →
            // bridge — instead of calling the bridge method directly, wiring the dispatcher exactly
            // as HtmlSettingsControl does.
            var dispatcher = new WebView2MessageDispatcher()
                .Register("__ideExecuteCommand__", 3, a =>
                    bridge.__ideExecuteCommand__(a[0].Value<string>(), a[1].Value<string>(), a[2].Value<string>()));

            var commandArgs = JsonConvert.SerializeObject(new object[] { "oauth", "https://api.snyk.io", false });
            var message = JsonConvert.SerializeObject(new
            {
                method = "__ideExecuteCommand__",
                args = new object[] { "snyk.login", commandArgs, string.Empty },
            });

            dispatcher.Dispatch(message);

            optionsMock.VerifySet(o => o.AuthenticationMethod = AuthenticationType.OAuth);
        }

        [Fact]
        public void ParseJsBool_HandlesJsValueShapesDeterministically()
        {
            Assert.True(HtmlSettingsScriptingBridge.ParseJsBool(true));
            Assert.False(HtmlSettingsScriptingBridge.ParseJsBool(false));
            Assert.False(HtmlSettingsScriptingBridge.ParseJsBool(null));
            Assert.True(HtmlSettingsScriptingBridge.ParseJsBool("true"));
            Assert.False(HtmlSettingsScriptingBridge.ParseJsBool("false"));
            Assert.True(HtmlSettingsScriptingBridge.ParseJsBool("1"));
            Assert.False(HtmlSettingsScriptingBridge.ParseJsBool("0"));
            Assert.True(HtmlSettingsScriptingBridge.ParseJsBool(1L));
            Assert.False(HtmlSettingsScriptingBridge.ParseJsBool(0L));
            Assert.True(HtmlSettingsScriptingBridge.ParseJsBool(1));
            // The original bug: Convert.ToBoolean threw on these; now they deterministically fall to false.
            Assert.False(HtmlSettingsScriptingBridge.ParseJsBool("yes"));
        }

        [Fact]
        public void SaveIdeConfig_AllKeysUnrecognised_FailsSave()
        {
            // A wholesale key rename (no recognised keys) must fail the save, not silently no-op.
            var json = JsonConvert.SerializeObject(new { renamed_one = true, renamed_two = false });

            bridge.__saveIdeConfig__(json);

            Assert.True(bridge.SaveCompletion.IsCompleted);
            Assert.False(bridge.SaveCompletion.Result);
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("file:///etc/passwd")]
        [InlineData("not a url")]
        public void SaveIdeConfig_DoesNotPersistNonWebEndpoint(string endpoint)
        {
            var config = JsonConvert.SerializeObject(new { api_endpoint = endpoint });

            bridge.__saveIdeConfig__(config);

            optionsMock.VerifySet(o => o.CustomEndpoint = endpoint, Times.Never());
        }

        [Fact]
        public void SaveIdeConfig_PersistsAbsoluteHttpsEndpoint()
        {
            var config = JsonConvert.SerializeObject(new { api_endpoint = "https://api.eu.snyk.io" });

            bridge.__saveIdeConfig__(config);

            optionsMock.VerifySet(o => o.CustomEndpoint = "https://api.eu.snyk.io");
        }

        [Fact]
        public void SaveIdeConfig_AllowsEmptyEndpoint_ToResetToDefault()
        {
            var config = JsonConvert.SerializeObject(new { api_endpoint = "" });

            bridge.__saveIdeConfig__(config);

            optionsMock.VerifySet(o => o.CustomEndpoint = string.Empty);
        }

        [Fact]
        public void SaveIdeConfig_AppliesEveryGlobalSetting_FullRoundTrip()
        {
            // Pin the full snake_case payload -> Options mapping in one place: a missing/renamed
            // [JsonProperty] on IdeConfigData or a broken Apply* wiring would drop a setting here with
            // a failing assertion, rather than silently. Uses a fully-stubbed options object so the
            // landed values can be read back.
            var localOptions = new Mock<ISnykOptions>();
            localOptions.SetupAllProperties();
            localOptions.Object.AuthenticationMethod = AuthenticationType.OAuth; // same as payload → no token-clear

            var sp = new Mock<ISnykServiceProvider>();
            sp.SetupGet(x => x.Options).Returns(localOptions.Object);
            sp.SetupGet(x => x.SnykOptionsManager).Returns(new Mock<ISnykOptionsManager>().Object);
            var localBridge = new HtmlSettingsScriptingBridge(sp.Object, onModified: () => { });

            var config = JsonConvert.SerializeObject(new
            {
                snyk_oss_enabled = true,
                snyk_code_enabled = true,
                snyk_iac_enabled = true,
                snyk_secrets_enabled = true,
                scan_automatic = true,
                scan_net_new = true,
                severity_filter_critical = true,
                severity_filter_high = false,
                severity_filter_medium = true,
                severity_filter_low = false,
                issue_view_open_issues = true,
                issue_view_ignored_issues = true,
                api_endpoint = "https://api.eu.snyk.io",
                organization = "my-org",
                proxy_insecure = true,
                authentication_method = "oauth",
                token = "my-token",
                cli_path = @"C:\cli\snyk.exe",
                automatic_download = false,
                binary_base_url = "https://downloads.snyk.io",
                cli_release_channel = "preview",
                risk_score_threshold = 500,
            });

            localBridge.__saveIdeConfig__(config);

            var o = localOptions.Object;
            Assert.True(o.OssEnabled);
            Assert.True(o.SnykCodeSecurityEnabled);
            Assert.True(o.IacEnabled);
            Assert.True(o.SecretsEnabled);
            Assert.True(o.AutoScan);
            Assert.True(o.EnableDeltaFindings);
            Assert.True(o.FilterCritical);
            Assert.False(o.FilterHigh);
            Assert.True(o.FilterMedium);
            Assert.False(o.FilterLow);
            Assert.True(o.OpenIssuesEnabled);
            Assert.True(o.IgnoredIssuesEnabled);
            Assert.Equal("https://api.eu.snyk.io", o.CustomEndpoint);
            Assert.Equal("my-org", o.Organization);
            Assert.True(o.IgnoreUnknownCA);
            Assert.Equal(AuthenticationType.OAuth, o.AuthenticationMethod);
            Assert.Equal("my-token", o.ApiToken.ToString());
            Assert.Equal(@"C:\cli\snyk.exe", o.CliCustomPath);
            Assert.False(o.BinariesAutoUpdate);
            Assert.Equal("https://downloads.snyk.io", o.CliBaseDownloadURL);
            Assert.Equal("preview", o.CliReleaseChannel);
            Assert.Equal(500, o.RiskScoreThreshold);
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

        [Theory]
        [InlineData("null")]
        [InlineData("")]
        [InlineData("   ")]
        public void SaveIdeConfig_PayloadDeserializesToNull_FailsSave(string json)
        {
            // A payload that maps to nothing (empty / "null" / whitespace) must fail the save so
            // OnApply surfaces the failure dialog, rather than reporting success with no changes.
            bridge.__saveIdeConfig__(json);

            Assert.True(bridge.SaveCompletion.IsCompleted);
            Assert.False(bridge.SaveCompletion.Result);
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
        public void IdeSaveAttemptFinished_ValidationError_FailsSaveCompletionImmediately()
        {
            // No __saveIdeConfig__ is sent on a validation error; the status callback must be
            // what fails the save so SaveAsync doesn't hang until its 5s timeout.
            Assert.False(bridge.SaveCompletion.IsCompleted);

            bridge.__ideSaveAttemptFinished__("validation_error");

            Assert.True(bridge.SaveCompletion.IsCompleted);
            Assert.False(bridge.SaveCompletion.Result);
        }

        [Fact]
        public void IdeSaveAttemptFinished_Success_DoesNotSignalCompletion()
        {
            // Success is signalled by __saveIdeConfig__; a "success" status here must be a no-op
            // so it can't race / pre-empt the real save result.
            bridge.__ideSaveAttemptFinished__("success");

            Assert.False(bridge.SaveCompletion.IsCompleted);
        }

        [Fact]
        public async Task IdeSaveAttemptFinished_DoesNotOverrideSuccessfulSave()
        {
            // __saveIdeConfig__ wins first (TrySetResult); a later failure status is ignored.
            bridge.__saveIdeConfig__("{}");

            // Await the completion (with a timeout) instead of assuming it has already completed
            // synchronously, so the test stays valid if the save path ever becomes genuinely async.
            Assert.True(await AwaitWithTimeout(bridge.SaveCompletion));

            bridge.__ideSaveAttemptFinished__("validation_error");

            Assert.True(await AwaitWithTimeout(bridge.SaveCompletion));
        }

        [Fact]
        public async Task SaveIdeConfig_RollsBackConsistentIgnoresEnabled_WhenApplyThrows()
        {
            // ConsistentIgnoresEnabled must be captured in the rollback snapshot so a failed save
            // does not leave it in a partially-applied state (IDE-1842 finding B).
            var localOptions = new Mock<ISnykOptions>();
            localOptions.SetupAllProperties();
            localOptions.Object.ConsistentIgnoresEnabled = true; // baseline to be restored

            var thrown = false;
            localOptions.SetupSet(o => o.RiskScoreThreshold = It.IsAny<int?>())
                .Callback<int?>(_ =>
                {
                    if (!thrown)
                    {
                        thrown = true;
                        throw new InvalidOperationException("boom");
                    }
                });

            var localManager = new Mock<ISnykOptionsManager>();
            var sp = new Mock<ISnykServiceProvider>();
            sp.SetupGet(x => x.Options).Returns(localOptions.Object);
            sp.SetupGet(x => x.SnykOptionsManager).Returns(localManager.Object);
            var localBridge = new HtmlSettingsScriptingBridge(sp.Object, onModified: () => { });

            // snyk_oss_enabled is applied early (before risk_score_threshold which throws).
            // ConsistentIgnoresEnabled is runtime-only so cannot be included in the config payload;
            // the test verifies that the snapshot captured it before apply and restores it on rollback.
            var config = JsonConvert.SerializeObject(new { snyk_oss_enabled = true, risk_score_threshold = 500 });
            localBridge.__saveIdeConfig__(config);

            Assert.False(await AwaitWithTimeout(localBridge.SaveCompletion)); // apply failed
            Assert.True(localOptions.Object.ConsistentIgnoresEnabled); // rolled back to baseline (true)
        }

        [Fact]
        public async Task SaveIdeConfig_RollsBackInMemoryOptions_WhenApplyThrows()
        {
            // Use a fully-stubbed options object so we can observe property state, and make a late
            // apply step (risk score) throw exactly once — after earlier fields were already applied.
            var localOptions = new Mock<ISnykOptions>();
            localOptions.SetupAllProperties();
            localOptions.Object.OssEnabled = false; // baseline we expect to be restored

            var thrown = false;
            localOptions.SetupSet(o => o.RiskScoreThreshold = It.IsAny<int?>())
                .Callback<int?>(_ =>
                {
                    if (!thrown)
                    {
                        thrown = true;
                        throw new InvalidOperationException("boom");
                    }
                });

            var localManager = new Mock<ISnykOptionsManager>();
            var sp = new Mock<ISnykServiceProvider>();
            sp.SetupGet(x => x.Options).Returns(localOptions.Object);
            sp.SetupGet(x => x.SnykOptionsManager).Returns(localManager.Object);
            var localBridge = new HtmlSettingsScriptingBridge(sp.Object, onModified: () => { });

            // snyk_oss_enabled is applied early; risk_score_threshold (which throws) is applied late.
            var config = JsonConvert.SerializeObject(new { snyk_oss_enabled = true, risk_score_threshold = 500 });
            localBridge.__saveIdeConfig__(config);

            Assert.False(await AwaitWithTimeout(localBridge.SaveCompletion)); // apply failed
            Assert.False(localOptions.Object.OssEnabled); // rolled back to baseline, not left at true
            // A failed apply never reaches the persistence step.
            localManager.Verify(m => m.Save(It.IsAny<IPersistableOptions>(), It.IsAny<bool>()), Times.Never);
        }

        private static async Task<bool> AwaitWithTimeout(Task<bool> task)
        {
            var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.True(completed == task, "SaveCompletion did not finish within the timeout");
            return await task;
        }

        [Fact]
        public void SaveIdeConfig_SetsOssEnabled_FromSnakeCaseKey()
        {
            var config = JsonConvert.SerializeObject(new IdeConfigData
            {
                SnykOssEnabled = true,
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
        public void SaveIdeConfig_SetsFilterSeverity_FromFlatSnakeCaseKeys()
        {
            var config = JsonConvert.SerializeObject(new
            {
                severity_filter_critical = true,
                severity_filter_high = false,
                severity_filter_medium = true,
                severity_filter_low = false,
            });

            bridge.__saveIdeConfig__(config);

            optionsMock.VerifySet(o => o.FilterCritical = true);
            optionsMock.VerifySet(o => o.FilterHigh = false);
            optionsMock.VerifySet(o => o.FilterMedium = true);
            optionsMock.VerifySet(o => o.FilterLow = false);
        }

        [Fact]
        public void SaveIdeConfig_SetsProductEnablement_FromSnakeCaseKeys()
        {
            var config = JsonConvert.SerializeObject(new
            {
                snyk_oss_enabled = true,
                snyk_code_enabled = false,
                snyk_iac_enabled = true,
            });

            bridge.__saveIdeConfig__(config);

            optionsMock.VerifySet(o => o.OssEnabled = true);
            optionsMock.VerifySet(o => o.SnykCodeSecurityEnabled = false);
            optionsMock.VerifySet(o => o.IacEnabled = true);
        }

        [Fact]
        public void SaveIdeConfig_PartialPayload_DoesNotTouchUnprovidedSettings()
        {
            var config = JsonConvert.SerializeObject(new IdeConfigData
            {
                SnykOssEnabled = true,
                // SecretsEnabled and SnykCodeSecurityEnabled are NOT provided
            });

            bridge.__saveIdeConfig__(config);

            optionsMock.VerifySet(o => o.OssEnabled = true);
            optionsMock.VerifySet(o => o.SecretsEnabled = It.IsAny<bool>(), Times.Never);
            optionsMock.VerifySet(o => o.SnykCodeSecurityEnabled = It.IsAny<bool>(), Times.Never);
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

            bridge = new HtmlSettingsScriptingBridge(
                serviceProviderMock.Object,
                onModified: () => { });
        }

        [Fact]
        public void SaveIdeConfig_FolderConfigs_UpdatesInMemoryFolderConfig()
        {
            // Pre-seed Options.FolderConfigs so the in-memory mirror path triggers
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig>
            {
                new FolderConfig { FolderPath = "/path/to/solution" }
            });

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

            // In-memory state updated — LS is the real persistence (no disk Save*Async calls)
            var fc = optionsMock.Object.FolderConfigs[0];
            Assert.Equal("my-org", fc.GetString(PflagKeys.PreferredOrg));
            Assert.True(Convert.ToBoolean(fc.Settings[PflagKeys.OrgSetByUser].Value));
            Assert.Equal("ENV_VAR=1", fc.GetString(PflagKeys.AdditionalEnvironment));
            Assert.Equal("auto-org", fc.GetString(PflagKeys.AutoDeterminedOrg));
        }

        [Fact]
        public void SaveIdeConfig_FolderConfigs_MirrorsChangedOverridesToExistingConfig()
        {
            // Existing in-memory global config for the current solution folder.
            var existing = new FolderConfig { FolderPath = "/path/to/solution" };
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { existing });

            var config = JsonConvert.SerializeObject(new IdeConfigData
            {
                FolderConfigs = new List<FolderConfigData>
                {
                    new FolderConfigData
                    {
                        FolderPath = "/path/to/solution",
                        PreferredOrg = "my-org",
                        BaseBranch = "develop",
                        SnykOssEnabled = true,
                        SnykCodeEnabled = false,
                        SeverityFilterHigh = false,
                        ScanAutomatic = true,
                        IssueViewIgnoredIssues = false,
                        RiskScoreThreshold = 500,
                    },
                },
            });

            bridge.__saveIdeConfig__(config);

            Assert.Equal("my-org", existing.GetString(PflagKeys.PreferredOrg));
            // BaseBranch has no solution-storage slot — only the mirror persists it.
            Assert.Equal("develop", existing.GetString(PflagKeys.BaseBranch));
            Assert.True(Convert.ToBoolean(existing.Settings[PflagKeys.SnykOssEnabled].Value));
            Assert.False(Convert.ToBoolean(existing.Settings[PflagKeys.SnykCodeEnabled].Value));
            Assert.False(Convert.ToBoolean(existing.Settings[PflagKeys.SeverityFilterHigh].Value));
            Assert.True(Convert.ToBoolean(existing.Settings[PflagKeys.ScanAutomatic].Value));
            Assert.False(Convert.ToBoolean(existing.Settings[PflagKeys.IssueViewIgnoredIssues].Value));
            Assert.Equal(500, Convert.ToInt32(existing.Settings[PflagKeys.RiskScoreThreshold].Value));
        }

        [Fact]
        public void SaveIdeConfig_FolderConfigs_AbsentFieldsDoNotClobberExistingOverrides()
        {
            // Existing config already carries several per-folder overrides.
            var existing = new FolderConfig { FolderPath = "/path/to/solution" };
            existing.SetString(PflagKeys.PreferredOrg, "original-org");
            existing.Set(PflagKeys.SnykOssEnabled, true);
            existing.Set(PflagKeys.SeverityFilterHigh, true);
            existing.Set(PflagKeys.RiskScoreThreshold, 700);
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { existing });

            // Changed-only payload touching a single override. Serialize an anonymous object with
            // ONLY the touched wire keys (folderPath + snyk_code_enabled) so the JSON matches the
            // real JS form output. Serializing a full FolderConfigData would emit every untouched
            // nullable field as explicit null (incl. preferred_org), which the reset path would
            // then treat as a deliberate reset and wrongly clobber the stored override.
            var config = JsonConvert.SerializeObject(new
            {
                folderConfigs = new[]
                {
                    new
                    {
                        folderPath = "/path/to/solution",
                        snyk_code_enabled = false,
                    },
                },
            });

            bridge.__saveIdeConfig__(config);

            // The one changed field is applied...
            Assert.False(Convert.ToBoolean(existing.Settings[PflagKeys.SnykCodeEnabled].Value));
            // ...and every untouched field survives.
            Assert.Equal("original-org", existing.GetString(PflagKeys.PreferredOrg));
            Assert.True(Convert.ToBoolean(existing.Settings[PflagKeys.SnykOssEnabled].Value));
            Assert.True(Convert.ToBoolean(existing.Settings[PflagKeys.SeverityFilterHigh].Value));
            Assert.Equal(700, Convert.ToInt32(existing.Settings[PflagKeys.RiskScoreThreshold].Value));
        }

        [Fact]
        public void SaveIdeConfig_FolderConfigs_MultipleFolders_AppliedPerPath()
        {
            // Two workspace folders. Each posted entry must land on the stored config with the
            // matching path — not collapse every edit onto the first entry.
            var folderA = new FolderConfig { FolderPath = "/repo/a" };
            var folderB = new FolderConfig { FolderPath = "/repo/b" };
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { folderA, folderB });

            var config = JsonConvert.SerializeObject(new IdeConfigData
            {
                FolderConfigs = new List<FolderConfigData>
                {
                    new FolderConfigData { FolderPath = "/repo/a", PreferredOrg = "org-a", SnykOssEnabled = true },
                    new FolderConfigData { FolderPath = "/repo/b", PreferredOrg = "org-b", SnykOssEnabled = false },
                },
            });

            bridge.__saveIdeConfig__(config);

            Assert.Equal("org-a", folderA.GetString(PflagKeys.PreferredOrg));
            Assert.True(Convert.ToBoolean(folderA.Settings[PflagKeys.SnykOssEnabled].Value));
            Assert.Equal("org-b", folderB.GetString(PflagKeys.PreferredOrg));
            Assert.False(Convert.ToBoolean(folderB.Settings[PflagKeys.SnykOssEnabled].Value));
        }

        [Fact]
        public void SaveIdeConfig_GlobalAdditionalParameters_SplitsSpaceJoinedString()
        {
            // Form sends additional_parameters as a plain string (text input → setFieldValue → string).
            var config = JsonConvert.SerializeObject(new { additional_parameters = "--debug --severity-threshold=high" });

            bridge.__saveIdeConfig__(config);

            optionsMock.VerifySet(o => o.AdditionalParameters = new List<string> { "--debug", "--severity-threshold=high" });
        }

        [Fact]
        public void SaveIdeConfig_GlobalAdditionalParameters_EmptyString_SetsEmptyList()
        {
            var config = JsonConvert.SerializeObject(new { additional_parameters = "" });

            bridge.__saveIdeConfig__(config);

            optionsMock.VerifySet(o => o.AdditionalParameters = new List<string>());
        }

        [Fact]
        public void SaveIdeConfig_FolderConfigs_NoExistingConfig_DoesNotThrow()
        {
            // No matching global config entry (FolderConfigs stays null) — the mirror block is
            // skipped; no exception is thrown; no in-memory update since no matching FolderConfig exists.
            var config = JsonConvert.SerializeObject(new IdeConfigData
            {
                FolderConfigs = new List<FolderConfigData>
                {
                    new FolderConfigData
                    {
                        FolderPath = "/path/to/solution",
                        PreferredOrg = "my-org",
                        SnykOssEnabled = true,
                    },
                },
            });

            bridge.__saveIdeConfig__(config);
        }

        [Fact]
        public void SaveIdeConfig_FolderConfigs_PresentNullFields_FlagsResetKeys()
        {
            // "Reset overrides" sends folder fields as flat JSON null (here the non-scalar
            // additional_parameters: array, additional_environment: string, scan_command_config:
            // object). Present-null can't be distinguished from absent by the typed model, so the
            // bridge re-reads the raw JSON: every folder field present as JSON null (except
            // folderPath) is flagged on the stored config's ResetKeys, so BuildFolderConfigs emits
            // {value:null, changed:true}. snyk-ls is authoritative and Unsets the user:folder:
            // override for the keys it recognizes. The bridge no longer clears the stored typed
            // value — BuildFolderConfigs's reset emit already overwrites any stored value.
            var existing = new FolderConfig { FolderPath = "/repo" };
            existing.Set(PflagKeys.AdditionalParameters, new List<string> { "--debug" });
            existing.SetString(PflagKeys.AdditionalEnvironment, "FOO=bar");
            existing.Set(PflagKeys.ScanCommandConfig, new Dictionary<string, ScanCommandConfig>
            {
                ["oss"] = new ScanCommandConfig { PreScanCommand = "echo hi" },
            });
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { existing });

            // Raw payload with explicit JSON nulls for the three non-scalar keys (array/string/object
            // fields). This is the exact present-null shape the dialog's reset emits.
            var config = "{" +
                "\"folderConfigs\":[{" +
                    "\"folderPath\":\"/repo\"," +
                    "\"additional_parameters\":null," +
                    "\"additional_environment\":null," +
                    "\"scan_command_config\":null" +
                "}]}";

            bridge.__saveIdeConfig__(config);

            // Every present-null field is forwarded as a reset; folderPath is never flagged.
            Assert.NotNull(existing.ResetKeys);
            Assert.Contains(PflagKeys.AdditionalParameters, existing.ResetKeys);
            Assert.Contains(PflagKeys.AdditionalEnvironment, existing.ResetKeys);
            Assert.Contains(PflagKeys.ScanCommandConfig, existing.ResetKeys);
            Assert.DoesNotContain("folderPath", existing.ResetKeys);
        }

        [Fact]
        public void SaveIdeConfig_FolderConfigs_UnknownPresentNullField_StillForwardedAsReset()
        {
            // The whitelist is gone: any folder field sent as JSON null is forwarded as a reset,
            // even one this build has no typed property for. snyk-ls is authoritative and ignores
            // nulls on keys it doesn't treat as folder-scoped, so forwarding extra present-nulls is
            // a safe no-op rather than something the IDE must filter.
            var existing = new FolderConfig { FolderPath = "/repo" };
            optionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { existing });

            var config = "{" +
                "\"folderConfigs\":[{" +
                    "\"folderPath\":\"/repo\"," +
                    "\"some_future_folder_key\":null" +
                "}]}";

            bridge.__saveIdeConfig__(config);

            Assert.NotNull(existing.ResetKeys);
            Assert.Contains("some_future_folder_key", existing.ResetKeys);
        }
    }
}
