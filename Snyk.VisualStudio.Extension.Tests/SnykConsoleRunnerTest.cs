using System;
using System.IO;
using Moq;
using Snyk.Common.Authentication;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.Shared.CLI;
using Xunit;

namespace Snyk.VisualStudio.Extension.Shared.Tests
{
    public class SnykConsoleRunnerTest
    {

        [Fact]
        public void SnykConsoleRunnerTest_CreateProcess_Respects_Analytics_Off_IsAnalyticsNotPermitted()
        {
            var optionsMock = new Mock<ISnykOptions>();
            optionsMock
                .Setup(options => options.UsageAnalyticsEnabled)
                .Returns(false);

            optionsMock
                .Setup(options => options.IsAnalyticsPermitted())
                .Returns(false);

            var cut = new SnykConsoleRunner(optionsMock.Object);

            var process = cut.CreateProcess("snyk", "test");

            Assert.Equal("1", process.StartInfo.EnvironmentVariables["SNYK_CFG_DISABLE_ANALYTICS"]);
        }
        
        [Fact]
        public void SnykConsoleRunnerTest_CreateProcess_Respects_Analytics_Off_IsAnalyticsPermitted()
        {
            var optionsMock = new Mock<ISnykOptions>();
            optionsMock
                .Setup(options => options.UsageAnalyticsEnabled)
                .Returns(false);

            optionsMock
                .Setup(options => options.IsAnalyticsPermitted())
                .Returns(true);

            var cut = new SnykConsoleRunner(optionsMock.Object);

            var process = cut.CreateProcess("snyk", "test");

            Assert.Equal("1", process.StartInfo.EnvironmentVariables["SNYK_CFG_DISABLE_ANALYTICS"]);
        }

        [Fact]
        public void SnykConsoleRunnerTest_CreateProcess_Respects_Analytics_On_IsAnalyticsPermitted()
        {
            var optionsMock = new Mock<ISnykOptions>();
            optionsMock
                .Setup(options => options.UsageAnalyticsEnabled)
                .Returns(true);

            optionsMock
                .Setup(options => options.IsAnalyticsPermitted())
                .Returns(true);

            var cut = new SnykConsoleRunner(optionsMock.Object);

            var process = cut.CreateProcess("snyk", "test");

            Assert.Null(process.StartInfo.EnvironmentVariables["SNYK_CFG_DISABLE_ANALYTICS"]);
        }

        [Fact]
        public void SnykConsoleRunnerTest_CreateProcess_Respects_Analytics_On_IsAnalyticsNotPermitted()
        {
            var optionsMock = new Mock<ISnykOptions>();
            optionsMock
                .Setup(options => options.UsageAnalyticsEnabled)
                .Returns(true);

            optionsMock
                .Setup(options => options.IsAnalyticsPermitted())
                .Returns(false);

            var cut = new SnykConsoleRunner(optionsMock.Object);

            var process = cut.CreateProcess("snyk", "test");

            Assert.Equal("1", process.StartInfo.EnvironmentVariables["SNYK_CFG_DISABLE_ANALYTICS"]);
        }
    }
}