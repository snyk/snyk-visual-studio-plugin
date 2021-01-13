using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Services;
using Snyk.VisualStudio.Extension.Settings;
using System;
using System.IO;
using System.Reflection;

namespace Snyk.VisualStudio.Extension.Tests
{   
    [TestClass]
    public class SnykCliTest
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            SnykSolutionService.Initialize(new SnykVSPackage());
        }

        [TestMethod]
        public void BuildArguments_WithoutOptions()
        {
            var cli = new SnykCli
            {
                Options = new DummySnykOptions()
            };

            Assert.AreEqual("--json test", cli.BuildArguments());
        }

        [TestMethod]
        public void BuildArguments_WithCustomEndpointOption()
        {
            var cli = new SnykCli
            {
                Options = new DummySnykOptions()
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
                Options = new DummySnykOptions()
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
                Options = new DummySnykOptions()
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
                Options = new DummySnykOptions()
                {
                    AdditionalOptions = "--file=C:\build.pom"
                }
            };

            Assert.AreEqual("--json test --file=C:\build.pom", cli.BuildArguments());
        }

        [TestMethod]
        public void BuildArguments_WithAllOptions()
        {
            var cli = new SnykCli
            {
                Options = new DummySnykOptions()
                {
                    CustomEndpoint = "https://github.com/snyk/",
                    IgnoreUnknownCA = true,
                    Organization = "test-snyk-organization",
                    AdditionalOptions = "--file=C:\build.pom"
                }               
            };

            Assert.AreEqual("--json test --api=https://github.com/snyk/ --insecure --org=test-snyk-organization --file=C:\build.pom", cli.BuildArguments());
        }

        [TestMethod]
        public void IsSuccessCliJsonString_True()
        {
            var cli = new SnykCli
            {
                Options = new DummySnykOptions()
            };

            Assert.IsTrue(cli.IsSuccessCliJsonString("{\"vulnerabilities\": []}"));
        }

        [TestMethod]
        public void IsSuccessCliJsonString_False()
        {
            var cli = new SnykCli
            {
                Options = new DummySnykOptions()
            };

            Assert.IsFalse(cli.IsSuccessCliJsonString("{\"error\": \"Error details.\"}"));
        }

        [TestMethod]
        public void ConvertRawCliStringToCliResult_VulnerabilitiesArrayJson()
        {
            var cli = new SnykCli
            {
                Options = new DummySnykOptions()
            };

            var cliResult = cli.ConvertRawCliStringToCliResult(GetFileContents("VulnerabilitiesArray.json"));

            Assert.AreEqual(2, cliResult.CLIVulnerabilities.Count);
        }

        [TestMethod]
        public void ConvertRawCliStringToCliResult_VulnerabilitiesSingleJson()
        {
            var cli = new SnykCli
            {
                Options = new DummySnykOptions()
            };

            var cliResult = cli.ConvertRawCliStringToCliResult(GetFileContents("VulnerabilitiesSingleObject.json"));

            Assert.AreEqual(1, cliResult.CLIVulnerabilities.Count);
        }

        [TestMethod]
        public void ConvertRawCliStringToCliResult_ErrorJson()
        {
            var cli = new SnykCli
            {
                Options = new DummySnykOptions()
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
                Options = new DummySnykOptions()
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
            var resourceFilePath = $"Snyk.VisualStudio.Extension.Tests.Resources.{resourceFileName}";

            using (var inputStream = assembly.GetManifestResourceStream(resourceFilePath))
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
    
    class DummyServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }

    class DummySnykOptions : ISnykOptions
    {
        private string apiToken = "";
        private string customEndpoint = "";
        private string organization = "";
        private bool ignoreUnknownCA = false;
        private string additionalOptions = "";

        public DummySnykOptions() { }

        public DummySnykOptions(string apiToken = "", 
            string customEndpoint = "", 
            string organization = "", 
            string additionalOptions = "", 
            bool ignoreUnknownCA = false)
        {
            CustomEndpoint = customEndpoint;
            ApiToken = apiToken;
            Organization = organization;
            IgnoreUnknownCA = ignoreUnknownCA;
            AdditionalOptions = additionalOptions;
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
    }
}
