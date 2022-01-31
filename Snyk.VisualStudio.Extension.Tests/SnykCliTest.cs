namespace Snyk.VisualStudio.Extension.Shared.Tests
{
    using System;
    using System.IO;
    using Snyk.VisualStudio.Extension.Shared.CLI;
    using Snyk.VisualStudio.Extension.Shared.Settings;
    using Xunit;

    public class SnykCliTest
    {
        [Fact]
        public void SnykCliTest_CliReturnError_GetApiTokenThrowException()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions(),
                ConsoleRunner = new SnykMockConsoleRunner("cli file note exists"),
            };

            Assert.Throws<InvalidTokenException>(() => cli.GetApiTokenOrThrowException());
        }

        [Fact]
        public void ConvertRawCliStringToCliResultWithCriticalSeverity()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions(),
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
        public void Scan()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions(),
                ConsoleRunner = new SnykMockConsoleRunner(this.GetFileContent("VulnerabilitiesSingleObject.json")),
            };

            var cliResult = cli.Scan(string.Empty);

            Assert.Single(cliResult.CliVulnerabilitiesList);
        }

        [Fact]
        public void SnykCliTest_GetApiToken_Successful()
        {
            string testGuid = Guid.NewGuid().ToString();

            var cli = new SnykCli
            {
                Options = new SnykMockOptions(),
                ConsoleRunner = new SnykMockConsoleRunner(testGuid),
            };

            Assert.Equal(testGuid, cli.GetApiToken());
        }

        [Fact]
        public void Authenticate()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions(),
                ConsoleRunner = new SnykMockConsoleRunner("Your account has been authenticated. Snyk is now ready to be used."),
            };

            Assert.Equal("Your account has been authenticated. Snyk is now ready to be used.", cli.Authenticate());
        }

        [Fact]
        public void BuildArguments_WithoutOptions()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions(),
            };

            Assert.Equal("--json test", cli.BuildScanArguments());
        }

        [Fact]
        public void BuildArguments_WithCustomEndpointOption()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
                {
                    CustomEndpoint = "https://github.com/snyk/",
                },
            };

            Assert.Equal("--json test --API=https://github.com/snyk/", cli.BuildScanArguments());
        }

        [Fact]
        public void BuildArguments_WithInsecureOption()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
                {
                    IgnoreUnknownCA = true,
                },
            };

            Assert.Equal("--json test --insecure", cli.BuildScanArguments());
        }

        [Fact]
        public void BuildArguments_WithOrganizationOption()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
                {
                    Organization = "test-snyk-organization",
                },
            };

            Assert.Equal("--json test --org=test-snyk-organization", cli.BuildScanArguments());
        }

        [Fact]
        public void BuildArguments_WithAdditionalOptions()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
                {
                    AdditionalOptions = "--file=C:\build.pom",
                },
            };

            Assert.Equal("--json test --file=C:\build.pom", cli.BuildScanArguments());
        }

        [Fact]
        public void BuildArguments_WithScanAllProjects()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
                {
                    IsScanAllProjects = true,
                },
            };

            Assert.Equal("--json test --all-projects", cli.BuildScanArguments());
        }

        [Fact]
        public void BuildArguments_WithAllOptions()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
                {
                    CustomEndpoint = "https://github.com/snyk/",
                    IgnoreUnknownCA = true,
                    Organization = "test-snyk-organization",
                    AdditionalOptions = "--ignore-policy",
                    IsScanAllProjects = true,
                    UsageAnalyticsEnabled = false,
                },
            };

            Assert.Equal("--json test --API=https://github.com/snyk/ --insecure --org=test-snyk-organization --ignore-policy --all-projects --DISABLE_ANALYTICS", cli.BuildScanArguments());
        }

        [Fact]
        public void BuildArguments_WithDisableAnalytics()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
                {
                    UsageAnalyticsEnabled = false,
                },
            };

            Assert.Equal("--json test --DISABLE_ANALYTICS", cli.BuildScanArguments());
        }

        [Fact]
        public void BuildEnvironmentVariables_WithAllOptions()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
                {
                    CustomEndpoint = "https://github.com/snyk/",
                    IgnoreUnknownCA = true,
                    Organization = "test-snyk-organization",
                    AdditionalOptions = "--ignore-policy",
                    IsScanAllProjects = true,
                    UsageAnalyticsEnabled = false,
                    ApiToken = "test-token",
                },
            };

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
        public void ConvertRawCliStringToCliResult_PlainTextError()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions(),
            };

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

    class SnykMockOptions : ISnykOptions
    {
        private string apiToken = string.Empty;
        private string customEndpoint = string.Empty;
        private string organization = string.Empty;
        private bool ignoreUnknownCA = false;
        private string additionalOptions = string.Empty;
        private bool isScanAllProjects = false;
        private bool usageAnalyticsEnabled = true;

        public SnykMockOptions() { }

        public SnykMockOptions(
            string apiToken = "",
            string customEndpoint = "",
            string organization = "",
            string additionalOptions = "",
            bool ignoreUnknownCA = false,
            bool isScanAllProjects = false)
        {
            this.CustomEndpoint = customEndpoint;
            this.ApiToken = apiToken;
            this.Organization = organization;
            this.IgnoreUnknownCA = ignoreUnknownCA;
            this.AdditionalOptions = additionalOptions;
            this.IsScanAllProjects = isScanAllProjects;
        }

        public event EventHandler<SnykSettingsChangedEventArgs> SettingsChanged;

        public string ApiToken
        {
            get => this.apiToken;
            set => this.apiToken = value;
        }

        public string CustomEndpoint
        {
            get => this.customEndpoint;
            set => this.customEndpoint = value;
        }

        public string Organization
        {
            get => this.organization;
            set => this.organization = value;
        }

        public bool IgnoreUnknownCA
        {
            get => this.ignoreUnknownCA;
            set => this.ignoreUnknownCA = value;
        }

        public string AdditionalOptions
        {
            get => this.additionalOptions;
            set => this.additionalOptions = value;
        }

        public bool IsScanAllProjects
        {
            get => this.isScanAllProjects;
            set => this.isScanAllProjects = value;
        }

        public bool UsageAnalyticsEnabled
        {
            get => this.usageAnalyticsEnabled;
            set => this.usageAnalyticsEnabled = value;
        }

        public bool OssEnabled => throw new NotImplementedException();

        public bool SnykCodeSecurityEnabled => throw new NotImplementedException();

        public bool SnykCodeQualityEnabled => throw new NotImplementedException();

        public string AnonymousId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Authenticate(Action<string> successCallbackAction, Action<string> errorCallbackAction) => throw new NotImplementedException();

        public void LoadSettingsFromStorage() => throw new NotImplementedException();
    }
}
