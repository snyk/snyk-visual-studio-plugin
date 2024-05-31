using System;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Serilog;
using Snyk.Code.Library.Domain.Analysis;
using Snyk.Common;
using Snyk.VisualStudio.Extension.Shared.Model;
using Snyk.VisualStudio.Extension.Shared.Service;
using Task = System.Threading.Tasks.Task;

namespace Snyk.VisualStudio.Extension.Shared.CLI
{
    public class SnykIdeAnalyticsService
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykIdeAnalyticsService>();
        private readonly ISnykOptionsProvider optionsProvider;
        private readonly ICliProvider cliProvider;
        private readonly ISnykScanTopicProvider scanTopicProvider;

        private DateTime codeScanningStarted;
        private DateTime ossScanningStarted;

        public SnykIdeAnalyticsService(
            ISnykOptionsProvider optionsProvider,
            ICliProvider cliProvider,
            ISnykScanTopicProvider scanTopicProvider
        )
        {
            this.optionsProvider = optionsProvider;
            this.cliProvider = cliProvider;
            this.scanTopicProvider = scanTopicProvider;
        }

        public void Initialize()
        {
            scanTopicProvider.OssScanningFinished += OnOssScanningFinished;
            scanTopicProvider.CliScanningStarted += OnCliScanningStarted;
            scanTopicProvider.OssScanningUpdate += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(async () => await OnOssScanningUpdateAsync(sender, args)).FireAndForget();
            scanTopicProvider.SnykCodeScanningUpdate += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(async () => await OnSnykCodeScanningUpdateAsync(sender, args)).FireAndForget();
            scanTopicProvider.SnykCodeScanningStarted += OnSnykCodeScanningStarted;
            scanTopicProvider.SnykCodeScanningFinished += OnSnykCodeScanningFinished;
        }

        public string GetAnalyticsPayload(string product, int durationMs, int critical, int high, int medium, int low)
        {
            ScanDoneEvent e = new()
            {
                Data = new Data
                {
                    Attributes = new Attributes(optionsProvider.Options)
                    {
                        DurationMs = durationMs.ToString(),
                        ScanType = product,
                        UniqueIssueCount = new UniqueIssueCount
                        {
                            Critical = critical,
                            High = high,
                            Medium = medium,
                            Low = low
                        }
                    }
                }
            };
            
            return JsonConvert.SerializeObject(e);
        }

        public void OnCliScanningStarted(object sender, SnykCliScanEventArgs e)
        {
            ossScanningStarted = DateTime.UtcNow;
        }

        public async Task OnOssScanningUpdateAsync(object sender, SnykCliScanEventArgs e)
        {
            try
            {
                var durationMs = DateTime.UtcNow.Subtract(ossScanningStarted).Milliseconds;
                int critical = 0, high = 0, medium = 0, low = 0;
                if (e.Result != null)
                {
                    // we just want to make sure that it's calculated
                    var unused = e.Result.GroupVulnerabilities;
                    critical = e.Result.CriticalSeverityCount;
                    high = e.Result.HighSeverityCount;
                    medium = e.Result.MediumSeverityCount;
                    low = e.Result.LowSeverityCount;
                }

                var payload = GetAnalyticsPayload("Snyk Open Source", durationMs, critical, high, medium, low);
                await cliProvider.Cli.ReportAnalyticsAsync(payload);
            }
            catch (Exception exception)
            {
                Logger.Warning(exception, "Failed to report analytics");
            }
        }

        public void OnOssScanningFinished(object sender, SnykCliScanEventArgs e)
        {
            // do nothing for now
        }

        public void OnSnykCodeScanningStarted(object sender, SnykCodeScanEventArgs e)
        {
            codeScanningStarted = DateTime.UtcNow;
        }

        public async Task OnSnykCodeScanningUpdateAsync(object sender, SnykCodeScanEventArgs e)
        {
            try
            {
                if (e.Result is not { Status: AnalysisStatus.Complete }) return;

                Logger.Information("Reporting analytics");
                var durationMs = DateTime.UtcNow.Subtract(codeScanningStarted).Milliseconds;
                int critical = 0, high = 0, medium = 0, low = 0;
                var fileAnalyses = e.Result.FileAnalyses;
                foreach (var fileAnalysis in fileAnalyses)
                {
                    foreach (var suggestion in fileAnalysis.Suggestions)
                    {
                        switch (Severity.FromInt(suggestion.Severity))
                        {
                            case Severity.Critical:
                                critical++;
                                continue;
                            case Severity.High:
                                high++;
                                continue;
                            case Severity.Medium:
                                medium++;
                                continue;
                            case Severity.Low:
                                low++;
                                continue;
                        }
                    }
                }

                var payload = GetAnalyticsPayload("Snyk Code", durationMs, critical, high, medium, low);
                await cliProvider.Cli.ReportAnalyticsAsync(payload);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to report analytics");
            }
        }

        public void OnSnykCodeScanningFinished(object sender, SnykCodeScanEventArgs e)
        {
            // do nothing for now
        }
    }
}