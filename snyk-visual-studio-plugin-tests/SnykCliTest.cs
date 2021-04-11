using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Settings;
using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Snyk.VisualStudio.Extension.Tests
{   
    [TestClass]
    public class SnykCliTest
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {           
        }

        [TestMethod]
        public void Scan()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions(),
                ConsoleRunner = new SnykMockConsoleRunner(GetFileContents("VulnerabilitiesSingleObject.json"))
            };

            var cliResult = cli.Scan("");

            Assert.AreEqual(1, cliResult.CliVulnerabilitiesList.Count);
        }

        [TestMethod]
        public void GetApiToken()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions(),
                ConsoleRunner = new SnykMockConsoleRunner("1111-0000-2222-3333-4444")
            };

            Assert.AreEqual("1111-0000-2222-3333-4444", cli.GetApiToken());
        }

        [TestMethod]
        public void Authenticate()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions(),
                ConsoleRunner = new SnykMockConsoleRunner("Your account has been authenticated. Snyk is now ready to be used.")
            };

            Assert.AreEqual("Your account has been authenticated. Snyk is now ready to be used.", cli.Authenticate());
        }

        [TestMethod]
        public void BuildArguments_WithoutOptions()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
            };

            Assert.AreEqual("--json test", cli.BuildArguments());
        }

        [TestMethod]
        public void BuildArguments_WithCustomEndpointOption()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
                {
                    CustomEndpoint = "https://github.com/snyk/"
                }
            };

            Assert.AreEqual("--json test --api=https://github.com/snyk/", cli.BuildArguments());
        }

        [TestMethod]
        public void BuildArguments_WithInsecureOption()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
                {
                    IgnoreUnknownCA = true
                }
            };

            Assert.AreEqual("--json test --insecure", cli.BuildArguments());
        }

        [TestMethod]
        public void BuildArguments_WithOrganizationOption()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
                {
                    Organization = "test-snyk-organization"
                }
            };

            Assert.AreEqual("--json test --org=test-snyk-organization", cli.BuildArguments());
        }

        [TestMethod]
        public void BuildArguments_WithAdditionalOptions()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
                {
                    AdditionalOptions = "--file=C:\build.pom"
                }
            };

            Assert.AreEqual("--json test --file=C:\build.pom", cli.BuildArguments());
        }

        [TestMethod]
        public void BuildArguments_WithScanAllProjects()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
                {
                    IsScanAllProjects = true
                }
            };

            Assert.AreEqual("--json test --all-projects", cli.BuildArguments());
        }

        [TestMethod]
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
                    IsScanAllProjects = true
                }
            };

            Assert.AreEqual("--json test --api=https://github.com/snyk/ --insecure --org=test-snyk-organization --ignore-policy --all-projects", cli.BuildArguments());
        }

        [TestMethod]
        public void IsSuccessCliJsonString_True()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
            };

            Assert.IsTrue(cli.IsSuccessCliJsonString("{\"vulnerabilities\": []}"));
        }

        [TestMethod]
        public void IsSuccessCliJsonString_False()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
            };

            Assert.IsFalse(cli.IsSuccessCliJsonString("{\"error\": \"Error details.\"}"));
        }

        [TestMethod]
        public void ConvertRawCliStringToCliResult_VulnerabilitiesArrayJson()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
            };

            var cliResult = cli.ConvertRawCliStringToCliResult(GetFileContents("VulnerabilitiesArray.json"));

            Assert.AreEqual(2, cliResult.CliVulnerabilitiesList.Count);
        }

        [TestMethod]
        public void ConvertRawCliStringToCliResult_VulnerabilitiesSingleJson()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
            };

            var cliResult = cli.ConvertRawCliStringToCliResult(GetFileContents("VulnerabilitiesSingleObject.json"));

            Assert.AreEqual(1, cliResult.CliVulnerabilitiesList.Count);
        }

        [TestMethod]
        public void ConvertRawCliStringToCliResult_ErrorJson()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
            };

            var cliResult = cli.ConvertRawCliStringToCliResult(GetFileContents("ErrorJsonObject.json"));

            Assert.IsNotNull(cliResult.Error);
            Assert.IsFalse(cliResult.Error.IsSuccess);
            Assert.IsTrue(cliResult.Error.Message.Contains("Could not detect supported target files in C:\\Users\\Test\\Documents\\MultiProjectConsoleApplication."));
            Assert.AreEqual("C:\\Users\\Test\\Documents\\MultiProjectConsoleApplication", cliResult.Error.Path);
        }

        [TestMethod]
        public void ConvertRawCliStringToCliResult_PlainTextError()
        {
            var cli = new SnykCli
            {
                Options = new SnykMockOptions()
            };

            var cliResult = cli.ConvertRawCliStringToCliResult(GetFileContents("ErrorPlainText.json"));

            Assert.IsNotNull(cliResult.Error);
            Assert.IsFalse(cliResult.Error.IsSuccess);
            Assert.IsTrue(cliResult.Error.Message.Contains("Please see our documentation for supported languages and target files:"));
            Assert.AreEqual("", cliResult.Error.Path);
        }

        private string GetFileContents(string resourceFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceFilePath = $"Resources.{resourceFileName}";

            using (var inputStream = assembly.GetEmbeddedResourceStream(resourceFilePath))
            {
                if (inputStream != null)
                {
                    var streamReader = new StreamReader(inputStream);

                    return streamReader.ReadToEnd();
                }
            }

            return String.Empty;
        }        
    }    

    static class AssemblyExtensions
    {
        public static Stream GetEmbeddedResourceStream(this Assembly assembly, string relativeResourcePath)
        {
            if (string.IsNullOrEmpty(relativeResourcePath))
            {
                throw new ArgumentNullException("relativeResourcePath");
            }

            var resourcePath = String.Format("{0}.{1}",
                Regex.Replace(assembly.ManifestModule.Name, @"\.(exe|dll)$",
                      string.Empty, RegexOptions.IgnoreCase), relativeResourcePath);

            var stream = assembly.GetManifestResourceStream(resourcePath);

            if (stream == null)
            {
                throw new ArgumentException(String.Format("The specified embedded resource \"{0}\" is not found.", relativeResourcePath));
            }

            return stream;
        }
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

        public override string Run(string fileName, string arguments)
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

        public bool UsageAnalyticsEnabled { get; set; }

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
