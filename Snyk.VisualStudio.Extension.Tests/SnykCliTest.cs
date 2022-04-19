namespace Snyk.VisualStudio.Extension.Shared.Tests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Moq;
    using Snyk.VisualStudio.Extension.Shared.CLI;
    using Snyk.VisualStudio.Extension.Shared.Model;
    using Snyk.VisualStudio.Extension.Shared.Settings;
    using Xunit;

    public class SnykCliTest
    {
        private Mock<ISnykOptions> optionsMock;

        public SnykCliTest()
        {
            this.optionsMock = new Mock<ISnykOptions>();

            this.optionsMock
                .Setup(options => options.UsageAnalyticsEnabled)
                .Returns(true);
        }

        [Fact]
        public void SnykCliTest_CliReturnError_GetApiTokenThrowException()
        {
            var cli = new SnykCli
            {
                Options = this.optionsMock.Object,
                ConsoleRunner = new SnykMockConsoleRunner("cli file note exists"),
            };

            Assert.Throws<InvalidTokenException>(() => cli.GetApiTokenOrThrowException());
        }

        [Fact]
        public void SnykCliTest_ConvertRawCliStringToCliResultWithCriticalSeverity_CriticalSeverityVulnExists()
        {
            var cli = new SnykCli
            {
                Options = this.optionsMock.Object,
            };

            var cliResult = SnykCli.ConvertRawCliStringToCliResult(this.GetFileContent("CriticalSeverityObject.json"));

            bool isCriticalSeverityVulnExists = false;

            foreach (Vulnerability vulnerability in cliResult.CliVulnerabilitiesList[0].Vulnerabilities)
            {
                if (vulnerability.Severity == Severity.Critical)
                {
                    isCriticalSeverityVulnExists = true;
                }
            }

            Assert.True(isCriticalSeverityVulnExists);
        }

        [Fact]
        public async Task SnykCliTest_RunScan_SuccessfulCliResultAsync()
        {
            var cli = new SnykCli
            {
                Options = this.optionsMock.Object,
                ConsoleRunner = new SnykMockConsoleRunner(this.GetFileContent("VulnerabilitiesSingleObject.json")),
            };

            var cliResult = await cli.ScanAsync(string.Empty);

            Assert.Single(cliResult.CliVulnerabilitiesList);
        }

        [Fact]
        public void SnykCliTest_GetApiToken_Successful()
        {
            string testGuid = Guid.NewGuid().ToString();

            var cli = new SnykCli
            {
                Options = this.optionsMock.Object,
                ConsoleRunner = new SnykMockConsoleRunner(testGuid),
            };

            Assert.Equal(testGuid, cli.GetApiToken());
        }

        [Fact]
        public void SnykCliTest_Authenticate_Successful()
        {
            var cli = new SnykCli
            {
                Options = this.optionsMock.Object,
                ConsoleRunner = new SnykMockConsoleRunner("Your account has been authenticated. Snyk is now ready to be used."),
            };

            Assert.Equal("Your account has been authenticated. Snyk is now ready to be used.", cli.Authenticate());
        }

        [Fact]
        public async Task SnykCliTest_BuildArguments_WithoutOptionsAsync()
        {
            var cli = new SnykCli
            {
                Options = this.optionsMock.Object,
            };

            Assert.Equal("--json test", await cli.BuildScanArgumentsAsync());
        }

        [Fact]
        public async Task SnykCliTest_BuildArguments_WithCustomEndpointOptionAsync()
        {
            this.optionsMock
                .Setup(options => options.CustomEndpoint)
                .Returns("https://github.com/snyk/");

            var cli = new SnykCli { Options = this.optionsMock.Object, };

            Assert.Equal("--json test --API=https://github.com/snyk/", await cli.BuildScanArgumentsAsync());
        }

        [Fact]
        public async Task SnykCliTest_BuildArguments_WithInsecureOptionAsync()
        {
            this.optionsMock
                .Setup(options => options.IgnoreUnknownCA)
                .Returns(true);

            var cli = new SnykCli { Options = this.optionsMock.Object, };

            Assert.Equal("--json test --insecure", await cli.BuildScanArgumentsAsync());
        }

        [Fact]
        public async Task SnykCliTest_BuildArguments_WithOrganizationOptionAsync()
        {
            this.optionsMock
                .Setup(options => options.Organization)
                .Returns("test-snyk-organization");

            var cli = new SnykCli { Options = this.optionsMock.Object, };

            Assert.Equal("--json test --org=test-snyk-organization", await cli.BuildScanArgumentsAsync());
        }

        [Fact]
        public async Task SnykCliTest_BuildArguments_WithAdditionalOptionsAsync()
        {
            this.optionsMock
                .Setup(options => options.GetAdditionalOptionsAsync())
                .ReturnsAsync("--file=C:\build.pom");

            var cli = new SnykCli { Options = this.optionsMock.Object, };

            Assert.Equal("--json test --file=C:\build.pom", await cli.BuildScanArgumentsAsync());
        }

        [Fact]
        public async Task SnykCliTest_BuildArguments_WithScanAllProjectsAsync()
        {
            this.optionsMock
                .Setup(options => options.IsScanAllProjectsAsync())
                .ReturnsAsync(true);

            var cli = new SnykCli { Options = this.optionsMock.Object, };

            Assert.Equal("--json test --all-projects", await cli.BuildScanArgumentsAsync());
        }

        [Fact]
        public async Task SnykCliTest_BuildArguments_WithAllOptionsAsync()
        {
            this.optionsMock
                .Setup(options => options.CustomEndpoint)
                .Returns("https://github.com/snyk/");

            this.optionsMock
                .Setup(options => options.IgnoreUnknownCA)
                .Returns(true);

            this.optionsMock
                .Setup(options => options.Organization)
                .Returns("test-snyk-organization");

            this.optionsMock
                .Setup(options => options.UsageAnalyticsEnabled)
                .Returns(false);

            this.optionsMock
                .Setup(options => options.GetAdditionalOptionsAsync())
                .ReturnsAsync("--ignore-policy");

            this.optionsMock
                   .Setup(options => options.IsScanAllProjectsAsync())
                   .ReturnsAsync(true);

            var cli = new SnykCli { Options = this.optionsMock.Object, };

            Assert.Equal(
                "--json test --API=https://github.com/snyk/ --insecure --org=test-snyk-organization --ignore-policy --all-projects --DISABLE_ANALYTICS",
                await cli.BuildScanArgumentsAsync());
        }

        [Fact]
        public async Task SnykCliTest_BuildArguments_WithDisableAnalyticsAsync()
        {
            this.optionsMock
                .Setup(options => options.UsageAnalyticsEnabled)
                .Returns(false);

            var cli = new SnykCli { Options = this.optionsMock.Object, };

            Assert.Equal("--json test --DISABLE_ANALYTICS", await cli.BuildScanArgumentsAsync());
        }

        [Fact]
        public void SnykCliTest_BuildEnvironmentVariables_WithAllOptions()
        {
            this.optionsMock
                .Setup(options => options.CustomEndpoint)
                .Returns("https://github.com/snyk/");

            this.optionsMock
                .Setup(options => options.IgnoreUnknownCA)
                .Returns(true);

            this.optionsMock
                .Setup(options => options.Organization)
                .Returns("test-snyk-organization");

            this.optionsMock
                .Setup(options => options.UsageAnalyticsEnabled)
                .Returns(false);

            this.optionsMock
                .Setup(options => options.GetAdditionalOptionsAsync())
                .ReturnsAsync("--ignore-policy");

            this.optionsMock
                   .Setup(options => options.IsScanAllProjectsAsync())
                   .ReturnsAsync(true);

            this.optionsMock
                   .Setup(options => options.ApiToken)
                   .Returns("test-token");

            var cli = new SnykCli { Options = this.optionsMock.Object, };

            var result = cli.BuildScanEnvironmentVariables();

            Assert.Equal(result["SNYK_API"], cli.Options.CustomEndpoint);
            Assert.Equal(result["SNYK_TOKEN"], cli.Options.ApiToken);
        }

        [Fact]
        public void IsSuccessCliJsonString_True()
        {
            Assert.True(SnykCli.IsSuccessCliJsonString("{\"vulnerabilities\": []}"));
        }

        [Fact]
        public void IsSuccessCliJsonString_False()
        {
            Assert.False(SnykCli.IsSuccessCliJsonString("{\"error\": \"Error details.\"}"));
        }

        [Fact]
        public void ConvertRawCliStringToCliResult_VulnerabilitiesArrayJson()
        {
            var cliResult = SnykCli.ConvertRawCliStringToCliResult(this.GetFileContent("VulnerabilitiesArray.json"));

            Assert.Equal(2, cliResult.CliVulnerabilitiesList.Count);
        }

        [Fact]
        public void ConvertRawCliStringToCliResult_VulnerabilitiesSingleJson()
        {
            var cliResult = SnykCli.ConvertRawCliStringToCliResult(this.GetFileContent("VulnerabilitiesSingleObject.json"));

            Assert.Single(cliResult.CliVulnerabilitiesList);
        }

        [Fact]
        public void ConvertRawCliStringToCliResult_ErrorJson()
        {
            var cliResult = SnykCli.ConvertRawCliStringToCliResult(this.GetFileContent("ErrorJsonObject.json"));

            Assert.NotNull(cliResult.Error);
            Assert.False(cliResult.Error.IsSuccess);
            Assert.Contains("Could not detect supported target files in C:\\Users\\Test\\Documents\\MultiProjectConsoleApplication.", cliResult.Error.Message);
            Assert.Equal("C:\\Users\\Test\\Documents\\MultiProjectConsoleApplication", cliResult.Error.Path);
        }

        [Fact]
        public void SnykCliTest_ConvertRawCliStringToCliResult_PlainTextError()
        {
            var cli = new SnykCli { Options = this.optionsMock.Object, };

            var cliResult = SnykCli.ConvertRawCliStringToCliResult(this.GetFileContent("ErrorPlainText.json"));

            Assert.NotNull(cliResult.Error);
            Assert.False(cliResult.Error.IsSuccess);
            Assert.Contains("Please see our documentation for supported languages and target files:", cliResult.Error.Message);
            Assert.Equal(string.Empty, cliResult.Error.Path);
        }

        /// <summary>
        /// Get full path for file in test resources.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <returns>Full path string.</returns>
        private string GetFileFullPath(string fileName)
            => Path.Combine(Directory.GetCurrentDirectory(), "Resources", fileName);

        /// <summary>
        /// Get path to Resources directory.
        /// </summary>
        /// <returns>Resources directory path string.</returns>
        private string GetResourcesPath() => Path.Combine(Directory.GetCurrentDirectory(), "Resources");

        /// <summary>
        /// Get file content as string.
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>File content string.</returns>
        private string GetFileContent(string fileName) => File.ReadAllText(this.GetFileFullPath(fileName));
    }

    class MockServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType) => throw new NotImplementedException();
    }
}
