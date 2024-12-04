using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Snyk.VisualStudio.Extension.Language;
using StreamJsonRpc;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    [Collection(MockedVS.Collection)]
    public class SnykLanguageClientTest : PackageBaseTest
    {
        private readonly SnykLanguageClient cut;
        private readonly Mock<ILanguageClientInitializationInfo> initializationInfoMock;

        public SnykLanguageClientTest(GlobalServiceProvider sp) : base(sp)
        {
            sp.Reset();
            var jsonRpcMock = new JsonRpc(new MemoryStream(), new MemoryStream());
            initializationInfoMock = new Mock<ILanguageClientInitializationInfo>();

            cut = new SnykLanguageClient
            {
                Rpc = jsonRpcMock
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
        public async Task AttachForCustomMessageAsync_ShouldSetRpcAndIsReady()
        {
            // Arrange
            var rpc = new JsonRpc(new MemoryStream(), new MemoryStream());

            // Act
            await cut.AttachForCustomMessageAsync(rpc);

            // Assert
            Assert.Equal(rpc, cut.Rpc);
            Assert.True(cut.IsReady);
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
            bool eventInvoked = false;
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
            bool eventInvoked = false;
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
            bool startInvoked = false;
            bool stopInvoked = false;

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
            bool startInvoked = false;
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
            bool eventInvoked = false;
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
            bool eventInvoked = false;
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


        [Fact]
        public async Task InvokeLogin_ShouldReturnNull_WhenNotReady()
        {
            // Arrange
            cut.IsReady = false;

            // Act
            var result = await cut.InvokeLogin(CancellationToken.None);

            // Assert
            Assert.Null(result);
        }
    }
}
