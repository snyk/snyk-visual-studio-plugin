using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Snyk.VisualStudio.Extension.Language;
using StreamJsonRpc;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    [Collection(MockedVS.Collection)]
    public class SnykLanguageClientTest : PackageBaseTest
    {
        private readonly SnykLanguageClient cut;
        private readonly Mock<ILanguageClientInitializationInfo> initializationInfoMock;
        private readonly Mock<IJsonRpc> jsonRpcMock;

        public SnykLanguageClientTest(GlobalServiceProvider sp) : base(sp)
        {
            sp.Reset();
            jsonRpcMock = new Mock<IJsonRpc>();
            initializationInfoMock = new Mock<ILanguageClientInitializationInfo>();

            cut = new SnykLanguageClient
            {
                Rpc = jsonRpcMock.Object
            };
        }

        [Fact]
        public async Task OnServerInitializeFailedAsync_ShouldReturnFailureContext_WithErrorMessage()
        {
            // Arrange
            var exception = new Exception("Initialization failed");
            initializationInfoMock.SetupGet(i => i.InitializationException).Returns(exception);

            // Act
            var failureContext = await cut.OnServerInitializeFailedAsync(initializationInfoMock.Object);

            // Assert
            Assert.NotNull(failureContext);
            Assert.Contains("Initialization failed", failureContext.FailureMessage);
        }

        [Fact]
        public async Task DidChangeConfigurationAsync_ShouldReturnNull_WhenNotReady()
        {
            // Arrange
            cut.IsReady = false;

            // Act
            var result = await cut.DidChangeConfigurationAsync(CancellationToken.None);

            // Assert
            jsonRpcMock.Verify(x => x.InvokeWithParameterObjectAsync<object>(LsConstants.WorkspaceChangeConfiguration,
                It.IsAny<LSP.DidChangeConfigurationParams>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
            Assert.Null(result);
        }

        [Fact]
        public async Task DidChangeConfigurationAsync_ShouldReturnDefault_WhenConfigIsNull()
        {
            // Arrange – make ServiceProvider null so LsSettingsV25.GetLspConfigurationParam returns null
            cut.IsReady = true;
            VsPackage.SetServiceProvider(null);

            // Act – must not throw even when the config param cannot be built
            var result = await cut.DidChangeConfigurationAsync(CancellationToken.None);

            // Assert – nothing sent to the language server.
            // Note: Logger.Warning is intentionally not asserted here. SnykLanguageClient uses
            // LogManager.ForContext<T>() which writes to a Lazy<Logger> backed by a file sink,
            // not to Serilog.Log.Logger (the mock set in PackageBaseTest). The warning cannot
            // be intercepted without injecting the logger — a larger refactor outside this scope.
            jsonRpcMock.Verify(x => x.InvokeWithParameterObjectAsync<object>(LsConstants.WorkspaceChangeConfiguration,
                It.IsAny<LSP.DidChangeConfigurationParams>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
            Assert.Null(result);
        }

        [Fact]
        public async Task DidChangeConfigurationAsync_ShouldInvoke()
        {
            // Arrange
            cut.IsReady = true;
            TestUtils.SetupOptionsMock(OptionsMock);
            TestUtils.SetupOptionsManagerMock(OptionsManagerMock);

            // Act
            var result = await cut.DidChangeConfigurationAsync(CancellationToken.None);

            // Assert
            jsonRpcMock.Verify(x => x.InvokeWithParameterObjectAsync<object>(LsConstants.WorkspaceChangeConfiguration,
                It.IsAny<LSP.DidChangeConfigurationParams>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
            Assert.Null(result);
        }


        [Fact]
        public void Rpc_Disconnected_ShouldSetIsReadyToFalse()
        {
            // Arrange
            cut.IsReady = true;
            var eventArgs = new JsonRpcDisconnectedEventArgs("disposed", DisconnectedReason.LocallyDisposed);

            // Act
            var method = typeof(SnykLanguageClient).GetMethod("Rpc_Disconnected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(cut, new object[] { null, eventArgs });

            // Assert
            Assert.False(cut.IsReady);
        }

        [Fact]
        public async Task AttachForCustomMessageAsync_ShouldSetRpc()
        {
            // Arrange
            var rpc = new JsonRpc(new MemoryStream(), new MemoryStream());

            // Act
            await cut.AttachForCustomMessageAsync(rpc);

            // Assert
            Assert.NotNull(cut.Rpc);
            Assert.Null(cut.Rpc.ActivityTracingStrategy);
        }

        [Fact]
        public async Task InvokeWorkspaceScanAsync_ShouldReturnNull_WhenNotReady()
        {
            // Arrange
            cut.IsReady = false;

            // Act
            var result = await cut.InvokeWorkspaceScanAsync(CancellationToken.None);

            // Assert
            Assert.Null(result);
            jsonRpcMock.Verify(x => x.InvokeWithParameterObjectAsync<object>(LsConstants.WorkspaceExecuteCommand,
                    It.Is<LSP.ExecuteCommandParams>(param => param.Command == LsConstants.SnykWorkspaceScan),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task InvokeWorkspaceScanAsync_ShouldInvoke_IfReady_FolderTrusted()
        {
            // Arrange
            cut.IsReady = true;
            TasksServiceMock.Setup(x => x.IsFolderTrustedAsync()).Returns(Task.FromResult(true));

            // Act
            var result = await cut.InvokeWorkspaceScanAsync(CancellationToken.None);

            // Assert
            Assert.Null(result);
            jsonRpcMock.Verify(x => x.InvokeWithParameterObjectAsync<object>(LsConstants.WorkspaceExecuteCommand,
                It.Is<LSP.ExecuteCommandParams>(param => param.Command == LsConstants.SnykWorkspaceScan),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeWorkspaceScanAsync_ShouldNotInvoke_IfReady_FolderNotTrusted()
        {
            // Arrange
            cut.IsReady = true;
            TasksServiceMock.Setup(x => x.IsFolderTrustedAsync()).Returns(Task.FromResult(false));

            // Act
            var result = await cut.InvokeWorkspaceScanAsync(CancellationToken.None);

            // Assert
            Assert.Null(result);
            jsonRpcMock.Verify(x => x.InvokeWithParameterObjectAsync<object>(LsConstants.WorkspaceExecuteCommand,
                It.Is<LSP.ExecuteCommandParams>(param => param.Command == LsConstants.SnykWorkspaceScan),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task StartServerAsync_ShouldInvokeStartAsync_WhenShouldStartIsTrue()
        {
            // Arrange
            bool eventInvoked = false;
            cut.StartAsync += (sender, args) =>
            {
                eventInvoked = true;
                return Task.CompletedTask;
            };

            TasksServiceMock.Setup(ts => ts.ShouldDownloadCli()).Returns(false);

            // Act
            await cut.StartServerAsync(true);

            // Assert
            Assert.True(eventInvoked);
        }

        [Fact]
        public async Task StartServerAsync_ShouldNotInvokeStartAsync_WhenShouldStartIsFalse()
        {
            // Arrange
            var eventInvoked = false;
            cut.StartAsync += (sender, args) =>
            {
                eventInvoked = true;
                return Task.CompletedTask;
            };

            // Act
            await cut.StartServerAsync(false);

            // Assert
            Assert.False(eventInvoked);
        }

        [Fact]
        public async Task StopServerAsync_ShouldInvokeStopAsync()
        {
            // Arrange
            var eventInvoked = false;
            cut.StopAsync += (sender, args) =>
            {
                eventInvoked = true;
                return Task.CompletedTask;
            };

            // Act
            await cut.StopServerAsync();

            // Assert
            Assert.True(eventInvoked);
        }

        [Fact]
        public async Task RestartAsync_ShouldRestartServer()
        {
            // Arrange
            var startInvoked = false;
            var stopInvoked = false;

            cut.StartAsync += (sender, args) =>
            {
                startInvoked = true;
                return Task.CompletedTask;
            };
            cut.StopAsync += (sender, args) =>
            {
                stopInvoked = true;
                return Task.CompletedTask;
            };

            // Act
            await cut.RestartServerAsync();

            // Assert
            Assert.True(stopInvoked);
            Assert.True(startInvoked);
        }

        [Fact]
        public async Task OnLoadedAsync_ShouldStartServer_WhenPackageIsInitialized()
        {
            // Arrange
            var startInvoked = false;
            cut.StartAsync += (sender, args) =>
            {
                startInvoked = true;
                return Task.CompletedTask;
            };

            TasksServiceMock.Setup(ts => ts.ShouldDownloadCli()).Returns(false);
            // Act
            await cut.OnLoadedAsync();

            // Assert
            Assert.True(startInvoked);
        }

        [Fact]
        public async Task OnLoadedAsync_ShouldNotStartServer_WhenPackageIsNotInitialized()
        {
            // Arrange
            bool startInvoked = false;
            cut.StartAsync += (sender, args) =>
            {
                startInvoked = true;
                return Task.CompletedTask;
            };
            typeof(SnykVSPackage).GetProperty("IsInitialized").SetValue(VsPackage, false, null);

            // Act
            await cut.OnLoadedAsync();

            // Assert
            Assert.False(startInvoked);
        }

        [Fact]
        public void FireOnLanguageServerReadyAsyncEvent_ShouldInvokeEvent()
        {
            // Arrange
            var eventInvoked = false;
            cut.OnLanguageServerReadyAsync += (sender, args) =>
            {
                eventInvoked = true;
                Assert.True(args.IsReady);
                return Task.CompletedTask;
            };

            // Act
            cut.FireOnLanguageServerReadyAsyncEvent();

            // Assert
            Assert.True(eventInvoked);
        }

        [Fact]
        public void FireOnLanguageClientNotInitializedAsync_ShouldInvokeEvent()
        {
            // Arrange
            var eventInvoked = false;
            cut.OnLanguageClientNotInitializedAsync += (sender, args) =>
            {
                eventInvoked = true;
                Assert.False(args.IsReady);
                return Task.CompletedTask;
            };

            // Act
            cut.FireOnLanguageClientNotInitializedAsync();

            // Assert
            Assert.True(eventInvoked);
        }


        [Theory]
        [InlineData("-d")]
        [InlineData("--debug")]
        public async Task GetLsDebugLevelAsync_ReturnsDebug_WhenGlobalAdditionalParametersContainsDebugFlag(string flag)
        {
            OptionsMock.SetupGet(o => o.AdditionalParameters).Returns(new List<string> { flag });
            OptionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig>());

            var method = typeof(SnykLanguageClient).GetMethod("GetLsDebugLevelAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var level = await (Task<string>)method.Invoke(cut, null);

            Assert.Equal("debug", level);
        }

        [Theory]
        [InlineData("-d")]
        [InlineData("--debug")]
        public async Task GetLsDebugLevelAsync_ReturnsDebug_WhenFolderAdditionalParametersContainsDebugFlag(string flag)
        {
            OptionsMock.SetupGet(o => o.AdditionalParameters).Returns(new List<string>());
            OptionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig>
            {
                new FolderConfig { AdditionalParameters = new List<string> { flag } }
            });

            var method = typeof(SnykLanguageClient).GetMethod("GetLsDebugLevelAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var level = await (Task<string>)method.Invoke(cut, null);

            Assert.Equal("debug", level);
        }

        [Fact]
        public async Task GetLsDebugLevelAsync_ReturnsInfo_WhenNoDebugFlagAnywhere()
        {
            OptionsMock.SetupGet(o => o.AdditionalParameters).Returns(new List<string> { "--some-other-flag" });
            OptionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig>
            {
                new FolderConfig { AdditionalParameters = new List<string> { "--filter=something" } }
            });

            var method = typeof(SnykLanguageClient).GetMethod("GetLsDebugLevelAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var level = await (Task<string>)method.Invoke(cut, null);

            Assert.Equal("info", level);
        }

        [Fact]
        public async Task InvokeLogin_ShouldReturnNull_WhenNotReady()
        {
            // Arrange
            cut.IsReady = false;
            // Act
            var result = await cut.InvokeLogin(CancellationToken.None);

            // Assert
            Assert.Null(result);
            jsonRpcMock.Verify(x => x.InvokeWithParameterObjectAsync<object>(LsConstants.WorkspaceExecuteCommand,
                    It.Is<LSP.ExecuteCommandParams>(param => param.Command == LsConstants.SnykLogin),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task InvokeLogin_ShouldInvokeRpcRequest()
        {
            // Arrange
            cut.IsReady = true;

            // Act
            var result = await cut.InvokeLogin(CancellationToken.None);

            // Assert
            Assert.Null(result);
            jsonRpcMock.Verify(x => x.InvokeWithParameterObjectAsync<object>(LsConstants.WorkspaceExecuteCommand,
                    It.Is<LSP.ExecuteCommandParams>(param => param.Command == LsConstants.SnykLogin),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
