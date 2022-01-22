namespace Snyk.VisualStudio.Extension.Shared.Tests
{
    using System;
    using System.Collections.Specialized;
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

            Assert.Throws<InvalidTokenException>(() => cli.GetApiToken());
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
            var cli = new SnykCli
            {
                Options = new SnykMockOptions(),
            };

            Assert.True(SnykCli.IsSuccessCliJsonString("{\"vulnerabilities\": []}"));
        }

        [Fact]
        public void IsSuccessCliJsonString_False()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions(),
            };

            Assert.False(SnykCli.IsSuccessCliJsonString("{\"error\": \"Error details.\"}"));
        }

        [Fact]
        public void ConvertRawCliStringToCliResult_VulnerabilitiesArrayJson()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions(),
            };

            var cliResult = SnykCli.ConvertRawCliStringToCliResult(this.GetFileContent("VulnerabilitiesArray.json"));

            Assert.Equal(2, cliResult.CliVulnerabilitiesList.Count);
        }

        [Fact]
        public void ConvertRawCliStringToCliResult_VulnerabilitiesSingleJson()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions(),
            };

            var cliResult = SnykCli.ConvertRawCliStringToCliResult(this.GetFileContent("VulnerabilitiesSingleObject.json"));

            Assert.Single(cliResult.CliVulnerabilitiesList);
        }

        [Fact]
        public void ConvertRawCliStringToCliResult_ErrorJson()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions(),
            };

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
        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }

    class SnykMockConsoleRunner: SnykConsoleRunner
    {
        private string consoleResult;

        public SnykMockConsoleRunner(string result)
        {
            this.consoleResult = result;
        }

        public override string Run(string fileName, string arguments, StringDictionary environmentVariables = null)
        {
            return consoleResult;
        }

        public override string Execute()
        {
            return consoleResult;
        }
    }

    class SnykMockOptions : ISnykOptions
    {
        private string apiToken = "";
        private string customEndpoint = "";
        private string organization = "";
        private bool ignoreUnknownCA = false;
        private string additionalOptions = "";
        private bool isScanAllProjects = false;
        private bool usageAnalyticsEnabled = true;

        public SnykMockOptions() { }

        public SnykMockOptions(string apiToken = "", 
            string customEndpoint = "", 
            string organization = "", 
            string additionalOptions = "", 
            bool ignoreUnknownCA = false,
            bool isScanAllProjects = false)
        {
            CustomEndpoint = customEndpoint;
            ApiToken = apiToken;
            Organization = organization;
            IgnoreUnknownCA = ignoreUnknownCA;
            AdditionalOptions = additionalOptions;
            IsScanAllProjects = isScanAllProjects;
        }
        
        public string ApiToken
        {
            get
            {
                return apiToken;
            }
            set
            {
                apiToken = value;
            }
        }

        public string CustomEndpoint
        {
            get
            {
                return customEndpoint;
            }
            set
            {
                customEndpoint = value;
            }
        }

        public string Organization
        {
            get
            {
                return organization;
            }
            set
            {
                organization = value;
            }
        }

        public bool IgnoreUnknownCA
        {
            get
            {
                return ignoreUnknownCA;
            }
            set
            {
                ignoreUnknownCA = value;
            }
        }

        public string AdditionalOptions
        {
            get
            {
                return additionalOptions;
            }

            set
            {
                additionalOptions = value;
            }
        }

        public bool IsScanAllProjects
        {
            set
            {
                isScanAllProjects = value;
            }

            get
            {
                return isScanAllProjects;
            }
        }

        public bool UsageAnalyticsEnabled
        {
            get
            {
                return usageAnalyticsEnabled;
            }

            set
            {
                usageAnalyticsEnabled = value;
            }
        }

        public bool OssEnabled => throw new NotImplementedException();

        public bool SnykCodeSecurityEnabled => throw new NotImplementedException();

        public bool SnykCodeQualityEnabled => throw new NotImplementedException();

        public event EventHandler<SnykSettingsChangedEventArgs> SettingsChanged;

        public void Authenticate(Action<string> successCallbackAction, Action<string> errorCallbackAction)
        {
            throw new NotImplementedException();
        }

        public void LoadSettingsFromStorage()
        {
            throw new NotImplementedException();
        }
    }
}
