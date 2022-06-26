namespace SnykVsIntegrationTests
{
	using Xunit;

	public class ExtensionStartupTests
	{
		[IdeFact]
		public void Nothing_Test()
		{
			Assert.Equal(0, 0);
		}

		[IdeFact]
		public void Nothing_Test_2()
		{
			Assert.Equal(1, 1);
		}
	}
}
