namespace SnykVsIntegrationTests
{
	using System;
	using System.IO;
	using System.Threading.Tasks;
	using Microsoft;
	using Microsoft.VisualStudio.Shell;
	using Microsoft.VisualStudio.Shell.Interop;
	using Microsoft.VisualStudio.Threading;
	using Snyk.VisualStudio.Extension.Shared;
	using Snyk.VisualStudio.Extension.Shared.UI.Toolwindow;
	using Xunit;
	using Xunit.Abstractions;
	using static Microsoft.VisualStudio.Shell.Interop.__VSSLNOPENOPTIONS;
	using static Microsoft.VisualStudio.Shell.Interop.__VSSLNOPENOPTIONS3;

	[IdeSettings(MinVersion = VisualStudioVersion.VS2019, MaxVersion = VisualStudioVersion.VS2022)]
	public class ExtensionStartupTests
	{
		private const int testTimeout = 120;

		private readonly ITestOutputHelper output;

		public ExtensionStartupTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		[IdeFact(Timeout = testTimeout)]
		public async Task OpenToolWindow_ExtensionIsLoaded()
		{
			this.output.WriteLine("Extension loading test started");

			this.output.WriteLine("Switching to UI thread");
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			this.output.WriteLine("Locating shell objects");
			var shell = ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) as IVsShell7;
			var uiShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;
			Assert.True(shell != null, "Failed to load shell");
			Assert.True(uiShell != null, "Failed to load UI shell");

			
			this.output.WriteLine("Opening Snyk tool-window");
			uiShell.PostExecCommand(SnykGuids.SnykVSPackageCommandSet, 
				(uint) SnykGuids.OpenToolWindowCommandId, 
				0,
				null);

			this.output.WriteLine("Waiting 1 second");
			await Task.Delay(TimeSpan.FromSeconds(1));

			this.output.WriteLine("Switch to background thread");
			await TaskScheduler.Default;

			this.output.WriteLine("Loading Snyk package");
			var guid = Guid.Parse(SnykVSPackage.PackageGuidString);
			var packageObject = await shell.LoadPackageAsync(ref guid);
			Assert.True(packageObject != null, "Failed to find Snyk package");
			Assert.IsType<SnykVSPackage>(packageObject);

			this.output.WriteLine("Test complete!");
		}
	}
}
