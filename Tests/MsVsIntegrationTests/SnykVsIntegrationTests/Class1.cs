namespace SnykVsIntegrationTests
{
	using Xunit;

	public class Class1
	{
		[IdeFact(MinVersion = VisualStudioVersion.VS2022)]
		public void Nothing_Test()
		{
			Assert.Equal(0, 0);
		}

		[IdeFact(MinVersion = VisualStudioVersion.VS2022)]
		public void Nothing_Test_2()
		{
			Assert.Equal(1, 1);
		}
	}
}
