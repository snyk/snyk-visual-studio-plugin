namespace SnykVsIntegrationTests
{
	using System;
	using System.Threading.Tasks;
	using Microsoft.VisualStudio.Shell;
	using Microsoft.VisualStudio.Shell.Interop;
	using Microsoft.VisualStudio.Threading;
	using Snyk.VisualStudio.Extension.Shared;
	using Xunit;
	using Xunit.Abstractions;
	using Task = System.Threading.Tasks.Task;

#if VS_VERSION_PRE22
	[IdeSettings(MinVersion = VisualStudioVersion.VS2019, MaxVersion = VisualStudioVersion.VS2019)]
#endif
	public class ExtensionStartupTests
	{
		private const int testTimeout = 120;

		private readonly ITestOutputHelper output;

		public ExtensionStartupTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Trait("integration", "true")]
		[IdeFact]
		public async Task OpenToolWindow_ExtensionIsLoaded()
		{
			// Arrange
			const int waitAfterOpen = 10;
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
				(uint) SnykGuids.OpenToolWindowCommandId, 
				0,
				null);

			// It's necessary to wait here because when the test finishes too fast, the "Xunit.Instances.VisuaStudio (...)"
			// pseudo-test will keep running after VS has already been closed, leading to a VS exp instance being open
			// after the test finished.
			this.output.WriteLine($"Waiting {waitAfterOpen} second");
			await Task.Delay(TimeSpan.FromSeconds(waitAfterOpen));
			this.output.WriteLine("Switch to background thread");
			await TaskScheduler.Default;
			
			this.output.WriteLine("Loading Snyk package");
			var guid = Guid.Parse(SnykVSPackage.PackageGuidString);
			var packageObject = await shell.LoadPackageAsync(ref guid);
			
			// Assert
			Assert.True(packageObject != null, "Failed to find Snyk package");
			Assert.IsType<SnykVSPackage>(packageObject);
			var snykVsPackage = packageObject as SnykVSPackage;
			Assert.True(snykVsPackage.ToolWindow != null, "Tool window failed to open");
			Assert.True(snykVsPackage.IsInitialized, "Snyk package was not initialized");
			this.output.WriteLine("Test complete!");
		}
	}
}
