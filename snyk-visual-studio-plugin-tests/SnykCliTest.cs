using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Settings;
using System;

namespace Snyk.VisualStudio.Extension.Tests
{
    [TestClass]
    public class SnykCliTest
    {
        [TestMethod]
        public void BuildArguments_WithoutOptions()
        {
            var cli = new SnykCli
            {
                Options = new DummySnykOptions(),
                ServiceProvider = new DummyServiceProvider(),
            };

            Assert.AreEqual("--json test", cli.BuildArguments());
        }

        [TestMethod]
        public void BuildArguments_WithCustomEndpointOption()
        {
            var dummyPackage = new SnykVSPackage();            
            var dummyServiceProvider = new DummyServiceProvider();

            var cli = new SnykCli
            {
                Options = new DummySnykOptions()
                {
                    CustomEndpoint = "https://github.com/snyk/"
                },
                ServiceProvider = new DummyServiceProvider(),
            };

            Assert.AreEqual("--json test --api=https://github.com/snyk/", cli.BuildArguments());
        }

        [TestMethod]
        public void BuildArguments_WithInsecureOption()
        {
            var dummyPackage = new SnykVSPackage();
            var dummyServiceProvider = new DummyServiceProvider();

            var cli = new SnykCli
            {
                Options = new DummySnykOptions()
                {
                    IgnoreUnknownCA = true
                },
                ServiceProvider = new DummyServiceProvider(),
            };

            Assert.AreEqual("--json test --insecure", cli.BuildArguments());
        }

        [TestMethod]
        public void BuildArguments_WithOrganizationOption()
        {
            var dummyPackage = new SnykVSPackage();
            var dummyServiceProvider = new DummyServiceProvider();

            var cli = new SnykCli
            {
                Options = new DummySnykOptions()
                {
                    Organization = "test-snyk-organization"
                },
                ServiceProvider = new DummyServiceProvider(),
            };

            Assert.AreEqual("--json test --org=test-snyk-organization", cli.BuildArguments());
        }

        [TestMethod]
        public void BuildArguments_WithAdditionalOptions()
        {
            var dummyPackage = new SnykVSPackage();
            var dummyServiceProvider = new DummyServiceProvider();

            var cli = new SnykCli
            {
                Options = new DummySnykOptions()
                {
                    AdditionalOptions = "--file=C:\build.pom"
                },
                ServiceProvider = new DummyServiceProvider(),
            };

            Assert.AreEqual("--json test --file=C:\build.pom", cli.BuildArguments());
        }

        [TestMethod]
        public void BuildArguments_WithAllOptions()
        {
            var dummyPackage = new SnykVSPackage();
            var dummyServiceProvider = new DummyServiceProvider();

            var cli = new SnykCli
            {
                Options = new DummySnykOptions()
                {
                    CustomEndpoint = "https://github.com/snyk/",
                    IgnoreUnknownCA = true,
                    Organization = "test-snyk-organization",
                    AdditionalOptions = "--file=C:\build.pom"
                },
                ServiceProvider = new DummyServiceProvider(),
            };

            Assert.AreEqual("--json test --api=https://github.com/snyk/ --insecure --org=test-snyk-organization --file=C:\build.pom", cli.BuildArguments());
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
