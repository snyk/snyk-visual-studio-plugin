using System;
using System.IO;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    [Collection(MockedVS.Collection)]
    public class SnykOptionsTest : IDisposable
    {
        private readonly SnykOptions cut;
        private readonly string settingsFilePath;

        public SnykOptionsTest(GlobalServiceProvider sp)
        {
            sp.Reset();
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            var snykSolutionService = new SnykSolutionService();
            serviceProviderMock.Setup(x => x.SolutionService).Returns(snykSolutionService);
            var serviceProvider = serviceProviderMock.Object;
            // IDE-1483: use a non-pre-created unique path so the file does not exist at
            // construction time (avoids the JSON-parse error that Path.GetTempFileName()'s
            // empty pre-created file causes) and does not accumulate toward the Windows
            // GetTempFileName 65535-file ceiling on long-lived CI agents.
            settingsFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
            serviceProviderMock.Setup(x => x.SnykOptionsManager).Returns(new SnykOptionsManager(settingsFilePath, serviceProvider));
            cut = new SnykOptions();
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(settingsFilePath))
                    File.Delete(settingsFilePath);
            }
            catch (Exception)
            {
                // Swallow cleanup errors so they cannot mask a real test failure.
            }
        }

        [Theory]
        [InlineData(null, "https://app.snyk.io/manage/snyk-code")]
        [InlineData("", "https://app.snyk.io/manage/snyk-code")]
        [InlineData("https://snyk.io", "https://app.snyk.io/manage/snyk-code")]
        [InlineData("https://snyk.io/api", "https://app.snyk.io/manage/snyk-code")]
        [InlineData("https://snyk.io/api/", "https://app.snyk.io/manage/snyk-code")]
        [InlineData("https://snykgov.io/api", "https://app.snykgov.io/manage/snyk-code")]
        [InlineData("https://app.snyk.io/api", "https://app.snyk.io/manage/snyk-code")]
        [InlineData("https://app.eu.snyk.io/api", "https://app.eu.snyk.io/manage/snyk-code")]
        [InlineData("https://app.snykgov.io/api", "https://app.snykgov.io/manage/snyk-code")]
        [InlineData("https://api.snyk.io", "https://app.snyk.io/manage/snyk-code")]
        [InlineData("https://api.snyk.io/", "https://app.snyk.io/manage/snyk-code")]
        [InlineData("https://api.snykgov.io", "https://app.snykgov.io/manage/snyk-code")]
        [InlineData("https://api.eu.snyk.io", "https://app.eu.snyk.io/manage/snyk-code")]
        public void SnykCodeSettingsUrl(string endpoint, string expected)
        {
            cut.CustomEndpoint = endpoint;
            Assert.Equal(expected, cut.SnykCodeSettingsUrl);
        }

        [Theory]
        [InlineData(null, "https://api.snyk.io")]
        [InlineData("", "https://api.snyk.io")]
        [InlineData("https://snyk.io/api", "https://api.snyk.io")]
        [InlineData("https://app.snyk.io/api", "https://api.snyk.io")]
        [InlineData("https://app.snykgov.io/api", "https://api.snykgov.io")]
        [InlineData("https://app.eu.snyk.io/api", "https://api.eu.snyk.io")]
        [InlineData("https://api.snyk.io", "https://api.snyk.io")]
        [InlineData("https://api.snykgov.io", "https://api.snykgov.io")]
        [InlineData("https://api.eu.snyk.io", "https://api.eu.snyk.io")]
        public void TransformApiToNewSchema(string endpoint, string expected)
        {
            cut.CustomEndpoint = endpoint;
            Assert.Equal(expected, cut.GetCustomApiEndpoint());
        }

        [Theory]
        [InlineData(null, "https://app.snyk.io")]
        [InlineData("", "https://app.snyk.io")]
        [InlineData("https://app.snyk.io/api", "https://app.snyk.io")]
        [InlineData("https://app.snykgov.io/api", "https://app.snykgov.io")]
        [InlineData("https://app.eu.snyk.io/api", "https://app.eu.snyk.io")]
        [InlineData("https://api.snyk.io", "https://app.snyk.io")]
        [InlineData("https://api.snykgov.io", "https://app.snykgov.io")]
        [InlineData("https://api.eu.snyk.io", "https://app.eu.snyk.io")]
        public void GetBaseAppUrl(string endpoint, string expected)
        {
            cut.CustomEndpoint = endpoint;
            Assert.Equal(expected, cut.GetBaseAppUrl());
        }
    }
}