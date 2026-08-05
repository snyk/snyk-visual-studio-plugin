using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;
using Moq;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
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

            // Default to "exists and compatible" so tests that aren't about the IDE-2404 protocol gate
            // itself don't need a real CLI binary on disk. The gate tests below override these back to
            // false individually.
            cut.CliExistsCheck = _ => true;
            cut.CliProtocolCompatibilityCheck = _ => CliProtocolCheckResult.Supported;

            // Default no-op so unrelated tests don't touch the real NotificationService.Instance
            // singleton (null under test anyway, but this keeps intent explicit and consistent with
            // the other seams above).
            cut.ShowInfoBar = _ => { };
        }

        private static SemaphoreSlim GetStartStopSemaphore(SnykLanguageClient client) =>
            (SemaphoreSlim)typeof(SnykLanguageClient)
                .GetField("semaphore", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(client);

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

        // IDE-2404: the gate must sit BEFORE StartAsync fires, not inside ActivateAsync. Once StartAsync
        // fires, VS's LSP framework commits to calling ActivateAsync and expects a valid Connection back;
        // returning null there throws an unhandled InvalidOperationException from inside VS's own
        // RemoteLanguageClientInstance (seen manually testing this fix: a second, alarming top-shell
        // banner on top of our actionable one). SnykCliDownloader.CheckCliProtocol's own parsing
        // logic is covered separately (SnykCliDownloaderInstallTests, Integration.Tests); this only
        // asserts the wiring, via the CliProtocolCompatibilityCheck seam - no real CLI binary needed.
        [Fact]
        public async Task StartServerAsync_ShouldNotInvokeStartAsync_WhenCliProtocolVersionIncompatible()
        {
            // Arrange
            cut.CliProtocolCompatibilityCheck = _ => CliProtocolCheckResult.Unsupported;

            var eventInvoked = false;
            cut.StartAsync += (sender, args) =>
            {
                eventInvoked = true;
                return Task.CompletedTask;
            };

            // Act
            await cut.StartServerAsync(true);

            // Assert
            Assert.False(eventInvoked);
        }

        // IDE-2404 follow-up (review finding): a timeout or a failed probe is not a confirmed protocol
        // mismatch - collapsing them into the same "does not support the required protocol version"
        // message misleads the user about the actual cause (e.g. an AV scanner still holding a
        // just-downloaded binary, not a genuinely incompatible CLI) and points them at the wrong fix.
        //
        // Two plain [Fact]s rather than a [Theory]: CliProtocolCheckResult is internal (matching this
        // file's own precedent for ActivationDecision in SnykVSPackageNotInitializedHandlerTests), and a
        // [Theory] method must be public for xUnit to discover it - a public method can't have an
        // internal type in its signature ("inconsistent accessibility").
        [Fact]
        public async Task StartServerAsync_ShowsADistinctMessage_WhenProtocolCheckTimesOut() =>
            await AssertShowsADistinctMessage(CliProtocolCheckResult.TimedOut, "not confirm");

        [Fact]
        public async Task StartServerAsync_ShowsADistinctMessage_WhenProtocolCheckFails() =>
            await AssertShowsADistinctMessage(CliProtocolCheckResult.CheckFailed, "could not check");

        private async Task AssertShowsADistinctMessage(CliProtocolCheckResult result, string expectedMessageFragment)
        {
            // Arrange
            cut.CliProtocolCompatibilityCheck = _ => result;

            string shownMessage = null;
            cut.ShowInfoBar = message => shownMessage = message;

            var eventInvoked = false;
            cut.StartAsync += (sender, args) =>
            {
                eventInvoked = true;
                return Task.CompletedTask;
            };

            // Act
            await cut.StartServerAsync(true);

            // Assert
            Assert.False(eventInvoked);
            Assert.NotNull(shownMessage);
            Assert.Contains(expectedMessageFragment, shownMessage, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("does not support the required Language Server protocol version", shownMessage);
        }

        // IDE-2404: pins the ordering fix from a prior review round - ShowErrorInfoBar blocks
        // synchronously on the main-thread queue, so it must run only AFTER semaphore.Release(), not
        // while still held, or a main-thread caller re-entering StartServerAsync/StopServerAsync while
        // it blocks would deadlock. Without this test, a future refactor moving the call back inside the
        // try/finally has nothing in the suite to catch it. Checks the semaphore's own CurrentCount
        // (reflection, since it's private) at the moment ShowInfoBar is invoked, rather than just
        // asserting invocation happened at all.
        [Fact]
        public async Task StartServerAsync_ShowsInfoBar_OnlyAfterSemaphoreIsReleased_WhenCliProtocolVersionIncompatible()
        {
            // Arrange
            cut.CliProtocolCompatibilityCheck = _ => CliProtocolCheckResult.Unsupported;
            var semaphore = GetStartStopSemaphore(cut);

            // Without a StartAsync subscriber, StartAsync == null && shouldStart short-circuits at the
            // very top of StartServerAsync (FireOnLanguageClientNotInitializedAsync then return) - the
            // gate logic below is never reached at all.
            cut.StartAsync += (sender, args) => Task.CompletedTask;

            string shownMessage = null;
            int? semaphoreCountWhenShown = null;
            cut.ShowInfoBar = message =>
            {
                shownMessage = message;
                semaphoreCountWhenShown = semaphore.CurrentCount;
            };

            // Act
            await cut.StartServerAsync(true);

            // Assert
            Assert.NotNull(shownMessage);
            Assert.Contains("does not support the required Language Server protocol version", shownMessage);
            Assert.Equal(1, semaphoreCountWhenShown); // released (capacity 1), not held (would be 0)
        }

        // IDE-2404 follow-up: a missing CLI binary (e.g. custom path set, auto-update off, file deleted)
        // was showing the protocol-mismatch banner ("does not support the required Language Server
        // protocol version 25") instead of the existing, correct "CLI not found" messaging - because the
        // old single check conflated "missing" with "wrong version". Asserts the protocol check isn't
        // even reached for a missing binary, so it can't misattribute the failure.
        //
        // Also asserts an InfoBar is shown here too (a later review round caught that this branch was
        // logged-only): SnykToolWindowControl's "CLI not found" messagePanel text only fires from an
        // active download attempt or the login flow, neither of which runs when BinariesAutoUpdate is
        // off (the scenario this gate exists for), and neither is reachable if the tool window was never
        // opened - so without this, a missing custom-path CLI could get zero user-facing feedback at all.
        [Fact]
        public async Task StartServerAsync_ShowsInfoBarAndDoesNotInvokeStartAsyncOrProtocolCheck_WhenCliDoesNotExist()
        {
            // Arrange
            cut.CliExistsCheck = _ => false;
            var protocolCheckInvoked = false;
            cut.CliProtocolCompatibilityCheck = _ =>
            {
                protocolCheckInvoked = true;
                return CliProtocolCheckResult.Supported;
            };

            string shownMessage = null;
            cut.ShowInfoBar = message => shownMessage = message;

            var eventInvoked = false;
            cut.StartAsync += (sender, args) =>
            {
                eventInvoked = true;
                return Task.CompletedTask;
            };

            // Act
            await cut.StartServerAsync(true);

            // Assert
            Assert.False(eventInvoked);
            Assert.False(protocolCheckInvoked);
            Assert.NotNull(shownMessage);
            Assert.Contains("was not found", shownMessage);
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


        // Wires a real seeded UserOverrideTracker (through the OptionsManager mock) with a single
        // pending reset queued, plus real options, so DidChangeConfigurationAsync exercises the real
        // peek-then-commit path against BuildSettingsMap. The mock manager's CommitPendingResets is
        // wired to delegate to the real tracker — mirroring the production SnykOptionsManager, which
        // owns the tracker and re-persists on commit (IDE-2152 fix #2/#3): the client commits via the
        // MANAGER (single entry point that keeps persistence in sync), not the tracker directly.
        private UserOverrideTracker ArrangeTrackerWithPendingReset(string resetKey)
        {
            TestUtils.SetupOptionsMock(OptionsMock);
            var tracker = new UserOverrideTracker();
            tracker.MarkSeeded(); // seeded so BuildSettingsMap consults the tracker + folds resets
            tracker.ApplyUserResets(new List<string> { resetKey }); // queue a reset
            OptionsManagerMock.Setup(m => m.OverrideTracker).Returns(tracker);
            OptionsManagerMock
                .Setup(m => m.CommitPendingResets(It.IsAny<IReadOnlyCollection<string>>()))
                .Callback<IReadOnlyCollection<string>>(sent => tracker.CommitPendingResets(sent));
            return tracker;
        }

        // IDE-2152-ACCEPT-002: When the configuration update to the LS fails once (transient), the
        // pending reset must NOT be lost — it is re-delivered on the next configuration update.
        [Fact]
        public async Task DidChangeConfigurationAsync_TransientFailure_ReDeliversPendingResetOnNextUpdate()
        {
            cut.IsReady = true;
            var tracker = ArrangeTrackerWithPendingReset(PflagKeys.SnykOssEnabled);

            // First send fails, second send succeeds.
            var call = 0;
            jsonRpcMock
                .Setup(x => x.InvokeWithParameterObjectAsync<object>(
                    LsConstants.WorkspaceChangeConfiguration,
                    It.IsAny<LSP.DidChangeConfigurationParams>(),
                    It.IsAny<CancellationToken>()))
                .Returns<string, object, CancellationToken>((_, __, ___) =>
                {
                    call++;
                    if (call == 1)
                        throw new InvalidOperationException("transient RPC failure");
                    return Task.FromResult<object>(null);
                });

            // First update: the send throws → the reset MUST remain queued (not consumed).
            await cut.DidChangeConfigurationAsync(CancellationToken.None);
            Assert.Contains(PflagKeys.SnykOssEnabled, tracker.PeekPendingResets());

            // Second update: the send succeeds → the reset is delivered again and only now committed.
            await cut.DidChangeConfigurationAsync(CancellationToken.None);
            Assert.DoesNotContain(PflagKeys.SnykOssEnabled, tracker.PeekPendingResets());

            // The reset reached the LS on BOTH attempts (the first, lost, and the re-delivery).
            jsonRpcMock.Verify(x => x.InvokeWithParameterObjectAsync<object>(
                LsConstants.WorkspaceChangeConfiguration,
                It.IsAny<LSP.DidChangeConfigurationParams>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        // IDE-2152-INTEG-003: A successful config send commits (clears) the pending reset so it is not
        // re-sent on a subsequent update.
        [Fact]
        public async Task DidChangeConfigurationAsync_SuccessfulSend_DoesNotReSendCommittedReset()
        {
            cut.IsReady = true;
            var tracker = ArrangeTrackerWithPendingReset(PflagKeys.SnykOssEnabled);

            jsonRpcMock
                .Setup(x => x.InvokeWithParameterObjectAsync<object>(
                    LsConstants.WorkspaceChangeConfiguration,
                    It.IsAny<LSP.DidChangeConfigurationParams>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((object)null);

            // First update succeeds → reset delivered and committed.
            await cut.DidChangeConfigurationAsync(CancellationToken.None);
            Assert.DoesNotContain(PflagKeys.SnykOssEnabled, tracker.PeekPendingResets());

            // Capture the second update's settings map: the reset key must NOT appear as a reset again.
            LSP.DidChangeConfigurationParams secondParam = null;
            jsonRpcMock
                .Setup(x => x.InvokeWithParameterObjectAsync<object>(
                    LsConstants.WorkspaceChangeConfiguration,
                    It.IsAny<LSP.DidChangeConfigurationParams>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, object, CancellationToken>((_, p, __) => secondParam = (LSP.DidChangeConfigurationParams)p)
                .ReturnsAsync((object)null);

            await cut.DidChangeConfigurationAsync(CancellationToken.None);

            var config = (LspConfigurationParam)secondParam.Settings;
            // snyk_oss_enabled must carry its real value (changed:false, seeded/untouched), NOT a reset.
            Assert.NotNull(config.Settings[PflagKeys.SnykOssEnabled].Value);
        }

        // IDE-2152 fix #7: the reset commit persists settings.json, which the whole persistence
        // subsystem assumes runs on the UI thread. DidChangeConfigurationAsync commits on the RPC
        // continuation (a thread-pool thread after `await ...ConfigureAwait(false)`), so without a
        // marshal it would be the FIRST background-thread settings writer and race the unlocked
        // UI-thread mutation sites. The commit must therefore run on the main thread. This asserts
        // CommitPendingResets is invoked while on the JTF main thread (ThreadHelper.CheckAccess()).
        //
        // This test is only meaningful if the RPC continuation genuinely resumes OFF the main thread —
        // otherwise the production `await SwitchToMainThreadAsync(...)` is a no-op in-test and
        // CheckAccess() would return true even with the marshal deleted (a vacuous guard). To make the
        // marshal load-bearing, the RPC mock below actually hops to the thread pool before returning, so
        // after `await Rpc...ConfigureAwait(false)` the continuation is provably on a background thread.
        [Fact]
        public async Task DidChangeConfigurationAsync_SuccessfulSend_CommitsOnMainThread()
        {
            // xUnit runs async [Fact] bodies on a thread-pool thread, not the JTF main thread. Under
            // MockedVS the framework pumps a mocked main thread, so this is the supported way to bring the
            // test body onto it (VSSDKTestFx docs). It must run before the guard below. It also keeps the
            // main thread pumping while we await DidChangeConfigurationAsync, so the production
            // SwitchToMainThreadAsync marshal can resolve re-entrantly.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Sanity: the test body runs on the JTF main thread under MockedVS. If this ever changes,
            // the off-thread reasoning below (and the marshal it guards) no longer holds.
            Assert.True(ThreadHelper.CheckAccess(), "Test must start on the JTF main thread under MockedVS.");

            cut.IsReady = true;
            TestUtils.SetupOptionsMock(OptionsMock);

            var tracker = new UserOverrideTracker();
            tracker.MarkSeeded();
            tracker.ApplyUserResets(new List<string> { PflagKeys.SnykOssEnabled });
            OptionsManagerMock.Setup(m => m.OverrideTracker).Returns(tracker);

            bool? committedOnMainThread = null;
            OptionsManagerMock
                .Setup(m => m.CommitPendingResets(It.IsAny<IReadOnlyCollection<string>>()))
                .Callback<IReadOnlyCollection<string>>(sent =>
                {
                    committedOnMainThread = ThreadHelper.CheckAccess();
                    tracker.CommitPendingResets(sent);
                });

            // The RPC continuation state captured at the moment the mock returns is exactly the context
            // the production code resumes onto after `await Rpc...ConfigureAwait(false)` (i.e. BEFORE the
            // marshal on line ~618). We force a real thread hop so this point is off the main thread.
            bool? onMainThreadWhenRpcReturned = null;
            jsonRpcMock
                .Setup(x => x.InvokeWithParameterObjectAsync<object>(
                    LsConstants.WorkspaceChangeConfiguration,
                    It.IsAny<LSP.DidChangeConfigurationParams>(),
                    It.IsAny<CancellationToken>()))
                .Returns<string, object, CancellationToken>(async (_, __, ___) =>
                {
                    // Genuinely leave the main thread: Task.Yield hands off, Task.Run guarantees a
                    // thread-pool thread. ConfigureAwait(false) keeps the completion off the main thread
                    // so the production continuation resumes on the thread pool.
                    await Task.Yield();
                    await Task.Run(() => { }).ConfigureAwait(false);
                    onMainThreadWhenRpcReturned = ThreadHelper.CheckAccess();
                    return null;
                });

            await cut.DidChangeConfigurationAsync(CancellationToken.None);

            // Precondition for the assertion below to have any teeth: the continuation was genuinely off
            // the main thread when the RPC completed. If this is true, the ONLY way CheckAccess() can be
            // true inside CommitPendingResets is the production SwitchToMainThreadAsync marshal.
            Assert.True(onMainThreadWhenRpcReturned.HasValue, "The RPC mock did not run.");
            Assert.False(onMainThreadWhenRpcReturned.Value,
                "The RPC continuation must resume OFF the main thread for this test to guard the marshal; " +
                "if it stayed on the main thread the assertion below would pass vacuously.");

            Assert.True(committedOnMainThread.HasValue, "CommitPendingResets was not invoked on a successful send.");
            Assert.True(committedOnMainThread.Value,
                "CommitPendingResets (which persists settings.json) must run on the UI thread, not the RPC " +
                "continuation thread — otherwise it is a background settings writer that races UI-thread saves. " +
                "Because the RPC continuation resumed off the main thread (asserted above), this can only be " +
                "true via the production SwitchToMainThreadAsync marshal — deleting it makes this assertion fail.");
        }

        // IDE-2152 fix #3: overlapping DidChangeConfigurationAsync calls must be SERIALIZED — the
        // peek→send→commit sequence runs one at a time. With a single reset queued and two concurrent
        // calls, the second must not invoke the RPC until the first has finished; and the reset must be
        // delivered exactly once as a reset (the second call peeks AFTER the first committed, so it sees
        // an empty queue and sends the real value). Before the gate, both calls peeked the same queue
        // and double-delivered the reset.
        [Fact]
        public async Task DidChangeConfigurationAsync_OverlappingCalls_AreSerialized_NoDoubleDelivery()
        {
            cut.IsReady = true;
            var tracker = ArrangeTrackerWithPendingReset(PflagKeys.SnykOssEnabled);

            var firstSendEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirstSend = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var sentParams = new System.Collections.Concurrent.ConcurrentQueue<LSP.DidChangeConfigurationParams>();
            var callCount = 0;

            jsonRpcMock
                .Setup(x => x.InvokeWithParameterObjectAsync<object>(
                    LsConstants.WorkspaceChangeConfiguration,
                    It.IsAny<LSP.DidChangeConfigurationParams>(),
                    It.IsAny<CancellationToken>()))
                .Returns<string, object, CancellationToken>(async (_, p, __) =>
                {
                    var n = Interlocked.Increment(ref callCount);
                    sentParams.Enqueue((LSP.DidChangeConfigurationParams)p);
                    if (n == 1)
                    {
                        firstSendEntered.TrySetResult(true);
                        await releaseFirstSend.Task.ConfigureAwait(false); // hold the gate
                    }
                    return null;
                });

            // Start the first call; it enters the RPC send and blocks there (holding configSendGate).
            var first = cut.DidChangeConfigurationAsync(CancellationToken.None);
            await firstSendEntered.Task;

            // Start the second call while the first is still in-flight. It must block on the gate and
            // NOT invoke the RPC yet.
            var second = cut.DidChangeConfigurationAsync(CancellationToken.None);
            await Task.Delay(100); // give the second call a chance to (wrongly) proceed if unserialized
            Assert.Equal(1, Volatile.Read(ref callCount)); // second must be blocked on the gate

            // Release the first send; both calls complete.
            releaseFirstSend.TrySetResult(true);
            await Task.WhenAll(first, second);

            Assert.Equal(2, Volatile.Read(ref callCount));

            // The reset was delivered on exactly ONE of the two sends (the first). The second, having
            // run after the first committed, must NOT re-send the reset.
            var payloads = sentParams.ToArray();
            var resetDeliveries = 0;
            foreach (var payload in payloads)
            {
                var cfg = (LspConfigurationParam)payload.Settings;
                if (cfg.Settings[PflagKeys.SnykOssEnabled].Value == null &&
                    cfg.Settings[PflagKeys.SnykOssEnabled].Changed)
                {
                    resetDeliveries++;
                }
            }
            Assert.Equal(1, resetDeliveries);
            Assert.DoesNotContain(PflagKeys.SnykOssEnabled, tracker.PeekPendingResets());
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
        public async Task InvokeWorkspaceScanAsync_ShouldInvoke_IfReady()
        {
            // Arrange
            // The LS enforces trust (trust_enabled=true), so a ready client forwards the scan command
            // regardless of folder trust.
            cut.IsReady = true;

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

            // VS sets this by calling OnLoadedAsync. Raising StartAsync before that point is out of
            // contract — VS discards it — so StartServerAsync deliberately defers, and this test would
            // otherwise be asserting behaviour that cannot happen in production.
            cut.VsHasLoadedClient = true;

            // Act
            await cut.StartServerAsync(true);

            // Assert
            Assert.True(eventInvoked);
        }

        [Fact]
        public async Task StartServerAsync_ShouldNotInvokeStartAsync_BeforeVsHasLoadedTheClient()
        {
            // The regression this guards. VS subscribes to StartAsync when it instantiates the client,
            // but will not activate it until it has called OnLoadedAsync. A start raised in that window
            // is silently discarded: the invoke returns immediately, ActivateAsync is never called, no
            // CLI process appears, and nothing retries — so the language server stays down for the whole
            // session. The download-finished handler wins that race whenever no download is needed.
            var eventInvoked = false;
            cut.StartAsync += (sender, args) =>
            {
                eventInvoked = true;
                return Task.CompletedTask;
            };

            TasksServiceMock.Setup(ts => ts.ShouldDownloadCli()).Returns(false);

            // The other half of the contract, and the reason this test exists twice over: returning
            // early must still REQUEST activation. Opening the bundled init file is what makes VS load
            // the client and call OnLoadedAsync; a return without this event loses the start entirely,
            // because nothing else triggers activation.
            var activationRequested = false;
            cut.OnLanguageClientNotInitializedAsync += (sender, args) =>
            {
                activationRequested = true;
                return Task.CompletedTask;
            };

            // Deliberately NOT setting VsHasLoadedClient: this is the pre-OnLoadedAsync window.
            await cut.StartServerAsync(true);

            Assert.False(eventInvoked);
            Assert.True(activationRequested);
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

            // A restart only happens after the server has been up, so VS has loaded the client by then.
            cut.VsHasLoadedClient = true;

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
            var folderConfig = new FolderConfig();
            folderConfig.Set(PflagKeys.AdditionalParameters, new List<string> { flag });
            OptionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { folderConfig });

            var method = typeof(SnykLanguageClient).GetMethod("GetLsDebugLevelAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var level = await (Task<string>)method.Invoke(cut, null);

            Assert.Equal("debug", level);
        }

        [Fact]
        public async Task GetLsDebugLevelAsync_ReturnsInfo_WhenNoDebugFlagAnywhere()
        {
            OptionsMock.SetupGet(o => o.AdditionalParameters).Returns(new List<string> { "--some-other-flag" });
            var folderConfig = new FolderConfig();
            folderConfig.Set(PflagKeys.AdditionalParameters, new List<string> { "--filter=something" });
            OptionsMock.SetupGet(o => o.FolderConfigs).Returns(new List<FolderConfig> { folderConfig });

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

        [Fact]
        public async Task OnServerInitializedAsync_WhenSolutionOpenedAfterLsReady_RunsMigration()
        {
            // Arrange — wire a real SnykVsSolutionLoadEvents so we can fire AfterBackgroundSolutionLoadComplete.
            // This test verifies Finding A: migration must run whenever a solution is opened while the LS
            // is already alive (multi-solution VS session), not only at LS startup.
            var migrationTcs = new TaskCompletionSource<bool>();

            var solutionServiceMock = new Mock<ISolutionService>();
            var solutionEvents = new SnykVsSolutionLoadEvents(solutionServiceMock.Object);
            solutionServiceMock.SetupGet(s => s.SolutionEvents).Returns(solutionEvents);
            solutionServiceMock.Setup(s => s.GetSolutionFolderAsync()).ReturnsAsync("/repo");

            ServiceProviderMock.Setup(sp => sp.SolutionService).Returns(solutionServiceMock.Object);

            OptionsManagerMock
                .Setup(m => m.MigrateLegacySolutionSettings(It.IsAny<string>()))
                .Callback<string>(_ => migrationTcs.TrySetResult(true))
                .Returns(false);

            // Act — simulate LS becoming ready (subscribes to solution-opened event)
            await cut.OnServerInitializedAsync();

            // Fire the solution-opened event (simulates user opening a different solution without restarting VS)
            solutionEvents.OnAfterBackgroundSolutionLoadComplete();

            // Wait up to 5 s for the async handler to call migration
            var completed = await Task.WhenAny(migrationTcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));

            // Assert — migration must have been triggered by the solution-opened event
            Assert.True(completed == migrationTcs.Task, "MigrateLegacySolutionSettings was not called after solution-opened event");
            OptionsManagerMock.Verify(m => m.MigrateLegacySolutionSettings("/repo"), Times.AtLeastOnce);
        }

        [Fact]
        public async Task OnServerInitializedAsync_WhenSolutionEventsNotAvailable_DoesNotThrow()
        {
            // ISolutionService.SolutionEvents returns null (e.g. service not yet initialized) —
            // OnServerInitializedAsync must not throw, it just skips the subscription.
            var solutionServiceMock = new Mock<ISolutionService>();
            solutionServiceMock.SetupGet(s => s.SolutionEvents).Returns((SnykVsSolutionLoadEvents)null);
            ServiceProviderMock.Setup(sp => sp.SolutionService).Returns(solutionServiceMock.Object);

            // Act — should not throw
            var ex = await Record.ExceptionAsync(() => cut.OnServerInitializedAsync());
            Assert.Null(ex);
        }
    }
}
