using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Serilog;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using StreamJsonRpc;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Language
{
    [Collection(MockedVS.Collection)]
    public class SnykLanguageClientTests
    {
        private readonly SnykLanguageClient cut;
        private readonly Mock<ISnykOptions> optionsMock;
        private readonly Mock<ISnykServiceProvider> serviceProviderMock;
        private readonly Mock<ILanguageClientInitializationInfo> initializationInfoMock;
        private readonly Mock<ISnykTasksService> tasksServiceMock;
        private readonly JsonRpc jsonRpcMock;
        private readonly Mock<ILogger> loggerMock;
        private readonly Mock<AuthenticationToken> authenticationTokenMock;

        public SnykLanguageClientTests(GlobalServiceProvider sp)
        {
            sp.Reset();
            // Set up mocks
            optionsMock = new Mock<ISnykOptions>();
            serviceProviderMock = new Mock<ISnykServiceProvider>();
            initializationInfoMock = new Mock<ILanguageClientInitializationInfo>();
            tasksServiceMock = new Mock<ISnykTasksService>();
            loggerMock = new Mock<ILogger>();
            authenticationTokenMock = new Mock<AuthenticationToken>();
            var sendingStream = new MemoryStream();
            var receivingStream = new MemoryStream();
            jsonRpcMock = new JsonRpc(sendingStream, receivingStream);
            // Set up service provider to return options mock
            serviceProviderMock.Setup(x => x.Options).Returns(optionsMock.Object);
            serviceProviderMock.Setup(x => x.TasksService).Returns(tasksServiceMock.Object);
            sp.AddService(typeof(ISnykService),serviceProviderMock.Object);
            // Set the static ServiceProvider to our mock
            //var snykVsPackage = new SnykVSPackage();
            //var instanceField = typeof(SnykVSPackage).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            //instanceField.SetValue(null, snykVsPackage);

            //var serviceProviderField = typeof(SnykVSPackage).GetField("serviceProvider", BindingFlags.Instance | BindingFlags.NonPublic);
            //serviceProviderField.SetValue(SnykVSPackage.ServiceProvider, serviceProviderMock.Object);

            // Set up the logger
            Log.Logger = loggerMock.Object;

            // Initialize the language client
            cut = new SnykLanguageClient();
            cut.Rpc = jsonRpcMock;
            SnykVSPackage.SetServiceProvider(serviceProviderMock.Object);
        }

        [Fact(Skip = "Need to Properly mock SnykVsPackage")]
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

        [Fact(Skip = "Need to Properly mock SnykVsPackage")]
        public async Task DidChangeConfigurationAsync_ShouldInvokeRpcMethod_WhenReady()
        {
            // Arrange
            cut.IsReady = true;

            // Act
            await cut.DidChangeConfigurationAsync(CancellationToken.None);

            // Assert
            //jsonRpcMock.Verify(rpc => rpc.InvokeWithParameterObjectAsync<object>(
            //    LsConstants.WorkspaceChangeConfiguration,
            //    It.IsAny<object>(),
            //    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact(Skip = "Need to Properly mock SnykVsPackage")]
        public async Task DidChangeConfigurationAsync_ShouldReturnNull_WhenNotReady()
        {
            // Arrange
            cut.IsReady = false;

            // Act
            var result = await cut.DidChangeConfigurationAsync(CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact(Skip = "Need to Properly mock SnykVsPackage")]
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

        [Fact(Skip = "Need to Properly mock SnykVsPackage")]
        public async Task AttachForCustomMessageAsync_ShouldSetRpcAndIsReady()
        {
            // Arrange
            var rpc = new JsonRpc(new System.IO.MemoryStream(), new System.IO.MemoryStream());

            // Act
            await cut.AttachForCustomMessageAsync(rpc);

            // Assert
            Assert.Equal(rpc, cut.Rpc);
            Assert.True(cut.IsReady);
        }

        [Fact(Skip = "Need to Properly mock SnykVsPackage")]
        public async Task InvokeWorkspaceScanAsync_ShouldReturnNull_WhenNotReady()
        {
            // Arrange
            cut.IsReady = false;

            // Act
            var result = await cut.InvokeWorkspaceScanAsync(CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact(Skip = "Need to Properly mock SnykVsPackage")]
        public async Task StartServerAsync_ShouldInvokeStartAsync_WhenShouldStartIsTrue()
        {
            // Arrange
            bool eventInvoked = false;
            cut.StartAsync += (sender, args) =>
            {
                eventInvoked = true;
                return Task.CompletedTask;
            };

            SnykVSPackage.Instance = new SnykVSPackage();
            var serviceProviderField = typeof(SnykVSPackage).GetField("serviceProvider", BindingFlags.Instance | BindingFlags.NonPublic);
            serviceProviderField.SetValue(SnykVSPackage.Instance, serviceProviderMock.Object);

            //SnykVSPackage.Instance.IsInitialized = true;
            tasksServiceMock.Setup(ts => ts.ShouldDownloadCli()).Returns(false);

            // Act
            await cut.StartServerAsync(true);

            // Assert
            Assert.True(eventInvoked);
        }

        [Fact(Skip = "Need to Properly mock SnykVsPackage")]
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

        [Fact(Skip = "Need to Properly mock SnykVsPackage")]
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

        [Fact(Skip = "Need to Properly mock SnykVsPackage")]
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

            // Access the private RestartAsync method using reflection
            var restartMethod = typeof(SnykLanguageClient).GetMethod("RestartAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var task = (Task)restartMethod.Invoke(cut, new object[] { false });
            await task;

            // Assert
            Assert.True(stopInvoked);
            Assert.True(startInvoked);
        }

        [Fact(Skip = "Need to Properly mock SnykVsPackage")]
        public async Task OnLoadedAsync_ShouldStartServer_WhenPackageIsInitialized()
        {
            // Arrange
            bool startInvoked = false;
            cut.StartAsync += (sender, args) =>
            {
                startInvoked = true;
                return Task.CompletedTask;
            };

            var serviceProviderField = typeof(SnykVSPackage).GetField("serviceProvider", BindingFlags.Instance | BindingFlags.NonPublic);
            serviceProviderField.SetValue(SnykVSPackage.Instance, serviceProviderMock.Object);

            tasksServiceMock.Setup(ts => ts.ShouldDownloadCli()).Returns(false);

            // Act
            await cut.OnLoadedAsync();

            // Assert
            Assert.True(startInvoked);
        }

        [Fact(Skip = "Need to Properly mock SnykVsPackage")]
        public async Task OnLoadedAsync_ShouldNotStartServer_WhenPackageIsNotInitialized()
        {
            // Arrange
            bool startInvoked = false;
            cut.StartAsync += (sender, args) =>
            {
                startInvoked = true;
                return Task.CompletedTask;
            };

            // Act
            await cut.OnLoadedAsync();

            // Assert
            Assert.False(startInvoked);
        }

        [Fact(Skip = "Need to Properly mock SnykVsPackage")]
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

        [Fact(Skip = "Need to Properly mock SnykVsPackage")]
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


        [Fact(Skip = "Need to Properly mock SnykVsPackage")]
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
