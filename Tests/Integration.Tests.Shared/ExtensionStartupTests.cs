namespace Integration.Tests.Shared
{
	using System;
	using System.Threading.Tasks;
	using Microsoft.VisualStudio.Shell;
	using Microsoft.VisualStudio.Shell.Interop;
	using Microsoft.VisualStudio.Threading;
	using Snyk.VisualStudio.Extension;
	using Xunit;
	using Xunit.Abstractions;
	using Task = System.Threading.Tasks.Task;

#if VS_VERSION_PRE22
	[IdeSettings(MinVersion = VisualStudioVersion.VS2019, MaxVersion = VisualStudioVersion.VS2019)]
#endif

    public class ExtensionStartupTests
    {
        private const int testTimeoutInSeconds = 60;
        private readonly ITestOutputHelper output;

        public ExtensionStartupTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Trait("integration", "true")]
        [IdeFact]
        public async Task OpenToolWindow_ExtensionIsLoaded()
        {
            await Test().WithTimeout(TimeSpan.FromSeconds(testTimeoutInSeconds));

            async Task Test()
            {
                // Arrange
                this.output.WriteLine("Extension loading test started");
                this.output.WriteLine("Switching to UI thread");
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.output.WriteLine("Locating shell objects");
                var shell = ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) as IVsShell7;
                var uiShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;
                Assert.True(shell != null, "Failed to load shell");
                Assert.True(uiShell != null, "Failed to load UI shell");

                // Act
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

                // Assert
                Assert.True(packageObject != null, "Failed to find Snyk package");
                Assert.IsType<SnykVSPackage>(packageObject);
                var snykVsPackage = (SnykVSPackage) packageObject;
                Assert.True(snykVsPackage.ToolWindow != null, "Tool window failed to open");
                Assert.True(snykVsPackage.IsInitialized, "Snyk package was not initialized");
            }
        }
    }
}
