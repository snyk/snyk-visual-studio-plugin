namespace Integration.Tests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;
    using Snyk.VisualStudio.Extension;
    using Snyk.VisualStudio.Extension.Language;
    using Xunit;
    using Xunit.Abstractions;
    using Task = System.Threading.Tasks.Task;

#if VS_VERSION_PRE22
	[IdeSettings(MinVersion = VisualStudioVersion.VS2019, MaxVersion = VisualStudioVersion.VS2019)]
#endif

    /// <summary>
    /// Acceptance tests for IDE-1752: Snyk extension must reach initialized/ready state when
    /// Visual Studio opens with no solution or folder open.
    /// </summary>
    public class NoSolutionInitTests
    {
        private const int testTimeoutInSeconds = 300;
        private readonly ITestOutputHelper output;

        public NoSolutionInitTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Scenario: Plugin initializes when Visual Studio opens with no solution or folder.
        /// Given Visual Studio 2022 is opened with no solution or folder open,
        /// And the Snyk extension has loaded,
        /// When the language client reports it is not yet initialized,
        /// Then the Snyk language server is activated and started,
        /// And the Snyk panel reaches a usable, ready state,
        /// And it does not remain stuck on "waiting for Visual Studio to initialize".
        ///
        /// The inner poll deadline is testTimeoutInSeconds - 10 (290s) anchored to test entry,
        /// so the named assertion always fires before the outer WithTimeout — intentionally
        /// generous to accommodate a cold CI runner where the one-time CLI binary download
        /// dominates startup time before the LSP handshake can complete.
        ///
        /// Static RED/GREEN reasoning (IDE-1752):
        ///   RED (before fix): LanguageClientManagerOnLanguageClientNotInitializedAsync enters the
        ///   Task.Delay(3000) dead-loop when IsSolutionOpen() == false, so IsLanguageServerReady()
        ///   never returns true. PackageInitializedAwaiter does complete (package init ≠ LS ready),
        ///   but the LS-ready poll here times out → test fails by timeout / assertion failure.
        ///   GREEN (after fix): the handler performs the same temp-file activation for the
        ///   no-solution branch as for the solution-open branch, so the LS starts and
        ///   IsLanguageServerReady() eventually returns true within the timeout.
        /// </summary>
        [Trait("integration", "true")]
        [IdeFact]
        public async Task InitializesWhenNoSolution()
        {
            await Test().WithTimeout(TimeSpan.FromSeconds(testTimeoutInSeconds));

            async Task Test()
            {
                // Anchor the inner deadline to test entry so it always fires before the outer
                // WithTimeout, ensuring the named IDE-1752 assertion message surfaces instead of
                // a generic harness cancellation if the LS does not start in time.
                var lsReadyDeadline = DateTime.UtcNow.AddSeconds(testTimeoutInSeconds - 10);

                // Arrange
                this.output.WriteLine("No-solution extension loading test started (IDE-1752)");
                this.output.WriteLine("Switching to UI thread");
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.output.WriteLine("Locating shell objects");
#pragma warning disable VSSDK006
                var shell = ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) as IVsShell7;
                var uiShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;
                var solution = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) as IVsSolution;
#pragma warning restore VSSDK006
                Assert.True(shell != null, "Failed to load shell");
                Assert.True(uiShell != null, "Failed to load UI shell");

                // Precondition: assert no solution is open (IdeFact host starts with no solution).
                // Fail-fast if IVsSolution is unavailable — silently skipping the precondition
                // would let the test pass even when a solution is open, hiding the regression.
                Assert.True(solution != null, "IVsSolution service unavailable; cannot verify the no-solution precondition");
                solution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var isSolutionOpenObj);
                var isSolutionOpen = isSolutionOpenObj is bool b && b;
                this.output.WriteLine($"Precondition: IsSolutionOpen = {isSolutionOpen}");
                Assert.False(isSolutionOpen, "Precondition failed: a solution is open but this test requires no solution to be open");

                // Act — open the Snyk tool window (same as the existing startup test)
                this.output.WriteLine("Opening Snyk tool-window");
                uiShell.PostExecCommand(SnykGuids.SnykVSPackageCommandSet,
                    SnykGuids.OpenToolWindowCommandId,
                    0,
                    null);

                this.output.WriteLine("Asynchronously waiting for package to be initialized...");
                await SnykVSPackage.PackageInitializedAwaiter;
                this.output.WriteLine("Package initialized, switching to background thread");
                await TaskScheduler.Default;

                this.output.WriteLine("Loading Snyk package");
                var guid = Guid.Parse(SnykVSPackage.PackageGuidString);
                var packageObject = await shell.LoadPackageAsync(ref guid);

                // Assert: package initialized
                Assert.True(packageObject != null, "Failed to find Snyk package");
                Assert.IsType<SnykVSPackage>(packageObject);
                var snykVsPackage = (SnykVSPackage)packageObject;
                Assert.True(snykVsPackage.IsInitialized, "Snyk package was not initialized");

                // Assert: language server reaches ready state within the bounded timeout.
                // lsReadyDeadline was captured at test entry (testTimeoutInSeconds - 10s = 290s),
                // so this assertion always fires before the 300s outer WithTimeout — emitting a
                // clear IDE-1752 message rather than a generic harness cancellation.
                // On a cold CI runner the one-time CLI download dominates; this budget covers it.
                this.output.WriteLine("Polling for language server ready state (no-solution path)...");
                while (!LanguageClientHelper.IsLanguageServerReady())
                {
                    if (DateTime.UtcNow > lsReadyDeadline)
                    {
                        Assert.True(false,
                            "Language server did not reach ready state within the timeout when VS opened with no solution. " +
                            "This is the IDE-1752 hang: the no-solution branch never activated the ILanguageClient.");
                    }
                    await Task.Delay(500);
                }
                this.output.WriteLine("Language server reached ready state.");
            }
        }
    }
}
