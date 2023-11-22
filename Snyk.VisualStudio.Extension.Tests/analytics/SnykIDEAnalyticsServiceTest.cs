using System.Collections.Generic;
using Moq;
using Snyk.Code.Library.Api.Dto.Analysis;
using Snyk.Code.Library.Domain.Analysis;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.Shared;
using Snyk.VisualStudio.Extension.Shared.CLI;
using Snyk.VisualStudio.Extension.Shared.Service;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.analytics
{
    public class SnykIdeAnalyticsServiceTest
    {
        private static void SetupOptionsMock(Mock<ISnykOptionsProvider> optionsProviderMock,
            Mock<ISnykOptions> optionsMock)
        {
            optionsProviderMock
                .Setup(optionsProvider => optionsProvider.Options)
                .Returns(optionsMock.Object);

            optionsMock.SetupGet(options => options.AnonymousId).Returns("anonymousId");
            optionsMock.SetupGet(options => options.Application).Returns("application");
            optionsMock.SetupGet(options => options.ApplicationVersion).Returns("1.0.0");
            optionsMock.SetupGet(options => options.IntegrationName).Returns("integrationName");
            optionsMock.SetupGet(options => options.IntegrationVersion).Returns("1.0.0");
            optionsMock.SetupGet(options => options.IntegrationEnvironment).Returns("integrationEnvironment");
            optionsMock.SetupGet(options => options.IntegrationEnvironmentVersion).Returns("1.0.0");
        }

        [Fact]
        public void SnykIdeAnalyticsServiceTest_shouldCreateTheCorrectPayloadString()
        {
            var optionsProviderMock = new Mock<ISnykOptionsProvider>();
            var optionsMock = new Mock<ISnykOptions>();
            var scanTopicMock = new Mock<ISnykScanTopicProvider>();
            var cliProviderMock = new Mock<ICliProvider>();
            var service = new SnykIdeAnalyticsService(optionsProviderMock.Object, cliProviderMock.Object, scanTopicMock.Object);
            SetupOptionsMock(optionsProviderMock, optionsMock);

            var payload = service.GetAnalyticsPayload("product", 100, 1, 2, 3, 4);

            // assert payload json
            Assert.Contains("\"device_id\":\"anonymousId\"", payload);
            Assert.Contains("\"application\":\"application\"", payload);
            Assert.Contains("\"application_version\":\"1.0.0\"", payload);
            Assert.Contains("\"os\":\"windows\"", payload);
            Assert.Contains("\"integration_name\":\"integrationName\"", payload);
            Assert.Contains("\"integration_version\":\"1.0.0\"", payload);
            Assert.Contains("\"integration_environment\":\"integrationEnvironment\"", payload);
            Assert.Contains("\"integration_environment_version\":\"1.0.0\"", payload);
            Assert.Contains("\"event_type\":\"Scan done\"", payload);
            Assert.Contains("\"status\":\"Succeeded\"", payload);
            Assert.Contains("\"scan_type\":\"product\"", payload);
            Assert.Contains("\"unique_issue_count\":{\"critical\":1,\"high\":2,\"medium\":3,\"low\":4}", payload);
            Assert.Contains("\"duration_ms\":\"100\"", payload);
            Assert.Contains("\"type\":\"analytics\"", payload);
        }

        [Fact]
        public void SnykIdeAnalyticsServiceTest_shouldReportAnalyticsOnSnykCodeScanningUpdate()
        {
            var optionsProviderMock = new Mock<ISnykOptionsProvider>();
            var optionsMock = new Mock<ISnykOptions>();
            var scanTopicMock = new Mock<ISnykScanTopicProvider>();
            var cliProviderMock = new Mock<ICliProvider>();
            var cliMock = new Mock<ICli>();
            cliProviderMock.SetupGet(cli => cli.Cli).Returns(cliMock.Object);
            var service = new SnykIdeAnalyticsService(optionsProviderMock.Object, cliProviderMock.Object, scanTopicMock.Object);
            SetupOptionsMock(optionsProviderMock, optionsMock);
            var analysisResult = SetupAnalysisResult();
            var e = new SnykCodeScanEventArgs(analysisResult);

            service.OnSnykCodeScanningUpdate(this, e);

            // assert payload is generated (anonymousID called) and cli reportAnalytics is called
            optionsMock.VerifyGet(options => options.AnonymousId, Times.Once);
            cliProviderMock.Verify(cli => cli.Cli, Times.Once);
            cliMock.Verify( c => c.ReportAnalyticsAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void SnykIdeAnalyticsServiceTest_shouldReportAnalyticsOnOssScanningUpdate()
        {
            var optionsProviderMock = new Mock<ISnykOptionsProvider>();
            var optionsMock = new Mock<ISnykOptions>();
            var scanTopicMock = new Mock<ISnykScanTopicProvider>();
            var cliProviderMock = new Mock<ICliProvider>();
            var cliMock = new Mock<ICli>();
            cliProviderMock.SetupGet(cli => cli.Cli).Returns(cliMock.Object);
            var service = new SnykIdeAnalyticsService(optionsProviderMock.Object, cliProviderMock.Object, scanTopicMock.Object);
            SetupOptionsMock(optionsProviderMock, optionsMock);
            var cliResult = SetupCliResult();

            var e = new SnykCliScanEventArgs(cliResult);
            service.OnOssScanningUpdate(this, e);

            // assert payload is generated (anonymousID called) and cli reportAnalytics is called
            optionsMock.VerifyGet(options => options.AnonymousId, Times.Once);
            cliProviderMock.Verify(cli => cli.Cli, Times.Once);
            cliMock.Verify( c => c.ReportAnalyticsAsync(It.IsAny<string>()), Times.Once);
        }

        private static CliResult SetupCliResult()
        {
            var cliResult = new CliResult
            {
                CliVulnerabilitiesList = new List<CliVulnerabilities> { new CliVulnerabilities() }
            };

            var vulnerability = new Vulnerability
            {
                Id = "testId",
                Severity = "critical"
            };

            cliResult.CliVulnerabilitiesList[0].Vulnerabilities = new[] { vulnerability };
            return cliResult;
        }

        private static AnalysisResult SetupAnalysisResult()
        {
            var analysisResult = new AnalysisResult
            {
                Status = AnalysisStatus.Complete,
                Progress = 1,
                FileAnalyses = new List<FileAnalysis>()
            };
            var fileAnalysis = new FileAnalysis
            {
                Suggestions = new List<Suggestion>()
            };
            var suggestionDto = new SuggestionDto
            {
                Severity = 1
            };
            var fileDto = new FileDto
            {
                Rows = new[] { 1, 2 },
                Cols = new[] { 1, 2 },
                Markers = new List<MarkerDto>()
            };
            var suggestion = new Suggestion("fileName", suggestionDto, fileDto);
            fileAnalysis.Suggestions.Add(suggestion);
            analysisResult.FileAnalyses.Add(fileAnalysis);
            return analysisResult;
        }
    }
}