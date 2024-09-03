using System;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Serilog;
using Snyk.Code.Library.Domain.Analysis;
using Snyk.Common;
using Snyk.VisualStudio.Extension.Model;
using Snyk.VisualStudio.Extension.Service;
using Task = System.Threading.Tasks.Task;

namespace Snyk.VisualStudio.Extension.CLI
{
    public class SnykIdeAnalyticsService
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykIdeAnalyticsService>();
        private readonly ISnykOptionsProvider optionsProvider;
        private readonly ICliProvider cliProvider;
        private readonly ISnykScanTopicProvider scanTopicProvider;
        private readonly ISolutionService solutionService;

        private DateTime codeScanningStarted;
        private DateTime ossScanningStarted;

        public SnykIdeAnalyticsService(
            ISnykOptionsProvider optionsProvider,
            ICliProvider cliProvider,
            ISnykScanTopicProvider scanTopicProvider,
            ISolutionService solutionService
        )
        {
            this.optionsProvider = optionsProvider;
            this.cliProvider = cliProvider;
            this.scanTopicProvider = scanTopicProvider;
            this.solutionService = solutionService;
        }

        public void Initialize()
        {
            scanTopicProvider.OssScanningFinished += OnOssScanningFinished;
            scanTopicProvider.OssScanningStarted += OnCliScanningStarted;
            scanTopicProvider.OssScanningUpdate += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(async () => await OnOssScanningUpdateAsync(sender, args)).FireAndForget();
            scanTopicProvider.SnykCodeScanningUpdate += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(async () => await OnSnykCodeScanningUpdateAsync(sender, args)).FireAndForget();
            scanTopicProvider.SnykCodeScanningStarted += OnSnykCodeScanningStarted;
            scanTopicProvider.SnykCodeScanningFinished += OnSnykCodeScanningFinished;
        }

        public string GetAnalyticsPayload(string product, int durationMs, int critical, int high, int medium, int low, string directoryPath)
        {
            ScanDoneEvent e = new()
            {
                Data = new Data
                {
                    Attributes = new Attributes(optionsProvider.Options)
                    {
                        DurationMs = durationMs.ToString(),
                        ScanType = product,
                        Path = directoryPath,
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

        public void OnCliScanningStarted(object sender, SnykOssScanEventArgs e)
        {
            ossScanningStarted = DateTime.UtcNow;
        }

        public async Task OnOssScanningUpdateAsync(object sender, SnykOssScanEventArgs e)
        {

        }

        public void OnOssScanningFinished(object sender, SnykOssScanEventArgs e)
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
                Logger.Information("Reporting analytics");
                var durationMs = DateTime.UtcNow.Subtract(codeScanningStarted).Milliseconds;
                int critical = 0, high = 0, medium = 0, low = 0;
                var fileAnalyses = e.Result;
                foreach (var fileAnalysis in fileAnalyses)
                {
                    foreach (var suggestion in fileAnalysis.Value)
                    {
                        switch (suggestion.Severity)
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

                var directoryPath = await solutionService.GetSolutionFolderAsync();
                var payload = GetAnalyticsPayload("Snyk Code", durationMs, critical, high, medium, low, directoryPath);
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