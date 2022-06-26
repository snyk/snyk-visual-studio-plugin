namespace SnykVsIntegrationTests
{
	using System;
	using System.IO;
	using System.Threading.Tasks;
	using Microsoft;
	using Microsoft.VisualStudio.Shell;
	using Microsoft.VisualStudio.Shell.Interop;
	using Xunit;
	using static Microsoft.VisualStudio.Shell.Interop.__VSSLNOPENOPTIONS;
	using static Microsoft.VisualStudio.Shell.Interop.__VSSLNOPENOPTIONS3;

	public class ExtensionStartupTests
	{
		[IdeFact]
		public void Nothing_Test()
		{
			Assert.Equal(0, 0);
		}

		[IdeFact]
		public async Task Nothing_Test_Async()
		{
			await Task.Delay(TimeSpan.FromSeconds(2));
			Assert.Equal(0, 0);
		}

		[IdeFact]
		public async Task LoadShell()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			var shell = ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) as IVsShell7;
			Assert.NotNull(shell);
		}
	}
}
