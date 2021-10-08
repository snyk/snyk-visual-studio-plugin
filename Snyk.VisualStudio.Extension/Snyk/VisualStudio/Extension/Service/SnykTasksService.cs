namespace Snyk.VisualStudio.Extension.Service
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.VisualStudio.Shell;
    using Serilog;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.CLI;
    using static Snyk.VisualStudio.Extension.CLI.SnykCliDownloader;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Incapsulate logic with background tasks work.
    /// </summary>
    public class SnykTasksService
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykTasksService>();

        private static SnykTasksService instance;

        private CancellationTokenSource cliScanTokenSource;

        private CancellationTokenSource snykCodeScanTokenSource;

        private CancellationTokenSource downloadCliTokenSource;

        private Task ossScanTask;

        private Task snykCodeScanTask;

        private Task downloadCliTask;

        private ISnykServiceProvider serviceProvider;

        private SnykCli cli;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykTasksService"/> class.
        /// </summary>
        private SnykTasksService()
        {
        }

        /// <summary>
        /// Cli scanning started event handler.
        /// </summary>
        public event EventHandler<SnykCliScanEventArgs> CliScanningStarted;

        /// <summary>
        /// SnykCode scanning started event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> SnykCodeScanningStarted;

        /// <summary>
        /// Scanning OSS finished event handler.
        /// </summary>
        public event EventHandler<SnykCliScanEventArgs> OssScanningFinished;

        /// <summary>
        /// Scanning SnykCode finished event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> SnykCodeScanningFinished;

        /// <summary>
        /// Cli Scanning update event handler.
        /// </summary>
        public event EventHandler<SnykCliScanEventArgs> CliScanningUpdate;

        /// <summary>
        /// SnykCode scanning update event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> SnykCodeScanningUpdate;

        /// <summary>
        /// Sli scan error event handler.
        /// </summary>
        public event EventHandler<SnykCliScanEventArgs> OssScanError;

        /// <summary>
        /// SnykCode scan error event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> SnykCodeScanError;

        /// <summary>
        /// SnykCode disabled event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> SnykCodeDisabled;

        /// <summary>
        /// Scanning cancelled event handler.
        /// </summary>
        public event EventHandler<SnykCliScanEventArgs> ScanningCancelled;

        /// <summary>
        /// Download started event handler.
        /// </summary>
        public event EventHandler<SnykCliDownloadEventArgs> DownloadStarted;

        /// <summary>
        /// Download finished event handler.
        /// </summary>
        public event EventHandler<SnykCliDownloadEventArgs> DownloadFinished;

        /// <summary>
        /// Download update event handler.
        /// </summary>
        public event EventHandler<SnykCliDownloadEventArgs> DownloadUpdate;

        /// <summary>
        /// Download cancelled event handler.
        /// </summary>
        public event EventHandler<SnykCliDownloadEventArgs> DownloadCancelled;

        /// <summary>
        /// Gets a value indicating whether <see cref="SnykTasksService"/> singleton instance.
        /// </summary>
        public static SnykTasksService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SnykTasksService();
                }

                return instance;
            }
        }

        /// <summary>
        /// Initialize service.
        /// </summary>
        /// <param name="serviceProvider">Service provider.</param>
        /// <returns>Task.</returns>
        public static async Task InitializeAsync(ISnykServiceProvider serviceProvider)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            instance = new SnykTasksService();

            instance.serviceProvider = serviceProvider;

            Logger.Information("SnykTasksService initialized");
        }

        /// <summary>
        /// Check is Scan running (oss or snykcode) or CLI download.
        /// </summary>
        /// <returns>True if Oss or SnykCode scan running.</returns>
        public bool IsTaskRunning() => this.IsOssScanRunning() || this.IsSnykCodeScanRunning() || this.IsDownloadRunning();

        /// <summary>
        /// Cancel current task.
        /// </summary>
        public void CancelTasks()
        {
            if (this.cliScanTokenSource != null)
            {
                Logger.Information("Cancel OSS task");

                this.cliScanTokenSource.Cancel();

                this.cli?.ConsoleRunner?.Stop();
            }

            if (this.snykCodeScanTokenSource != null)
            {
                Logger.Information("Cancel SnykCode task");

                this.snykCodeScanTokenSource.Cancel();
            }

            if (this.downloadCliTokenSource != null)
            {
                Logger.Information("Cancel download Cli task");

                this.downloadCliTokenSource.Cancel();
            }
        }

        /// <summary>
        /// Handle UI loaded event. Check CLI download on this event.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event arguments.</param>
        public void OnUiLoaded(object sender, RoutedEventArgs eventArgs) => this.Download();

        /// <summary>
        /// Start scan in background task.
        /// </summary>
        public void Scan()
        {
            Logger.Information("Enter Scan method");

            this.ScanOss();

            _ = this.ScanSnykCodeAsync();

            this.serviceProvider.AnalyticsService.LogUserTriggersAnAnalysisEvent(); // todo: move this to ScanOss and add for SnykCode.
        }

        /// <summary>
        /// Start download task in background thread.
        /// </summary>
        /// <param name="downloadFinishedCallback"><see cref="CliDownloadFinishedCallback"/> callback object.</param>
        public void Download(CliDownloadFinishedCallback downloadFinishedCallback = null)
        {
            Logger.Information("Enter Download method");

            if (this.IsTaskRunning())
            {
                Logger.Information("There is already a task in progress");

                return;
            }

            this.downloadCliTokenSource = new CancellationTokenSource();

            var progressWorker = new SnykProgressWorker
            {
                TasksService = this,
                TokenSource = this.downloadCliTokenSource,
            };

            Logger.Information("Start run task");

            this.downloadCliTask = Task.Run(
                () =>
                {
                    try
                    {
                        var userStorageService = this.serviceProvider.UserStorageSettingsService;

                        string currentCliVersion = userStorageService.GetCurrentCliVersion();

                        DateTime lastCliReleaseDate = userStorageService.GetCliReleaseLastCheckDate();

                        var cliDownloader = new SnykCliDownloader(currentCliVersion);

                        List<CliDownloadFinishedCallback> downloadFinishedCallbacks = new List<CliDownloadFinishedCallback>();

                        if (downloadFinishedCallback != null)
                        {
                            downloadFinishedCallbacks.Add(downloadFinishedCallback);
                        }

                        downloadFinishedCallbacks.Add(new CliDownloadFinishedCallback(() =>
                        {
                            userStorageService.SaveCurrentCliVersion(cliDownloader.GetLatestReleaseInfo().CliVersion);
                            userStorageService.SaveCliReleaseLastCheckDate(DateTime.UtcNow);
                        }));

                        cliDownloader.AutoUpdateCli(
                            lastCliReleaseDate,
                            progressWorker: progressWorker,
                            downloadFinishedCallbacks: downloadFinishedCallbacks);
                    }
                    catch (Exception exception)
                    {
                        Logger.Information(exception.Message);

                        this.OnDownloadCancelled(exception.Message);
                    }
                }, progressWorker.TokenSource.Token);
        }

        /// <summary>
        /// Fire error event. Create <see cref="CliError"/> instance.
        /// </summary>
        /// <param name="message">Error message.</param>
        public void FireOssError(string message) => this.FireOssError(new CliError(message));

        /// <summary>
        /// Fire error event with <see cref="SnykCliScanEventArgs"/>.
        /// </summary>
        /// <param name="error"><see cref="CliError"/> object.</param>
        public void FireOssError(CliError error) => this.OssScanError?.Invoke(this, new SnykCliScanEventArgs(error));

        /// <summary>
        /// Fire error event with <see cref="SnykCodeScanEventArgs"/>.
        /// </summary>
        /// <param name="message">Error message</param>
        public void OnSnykCodeError(string message) => this.SnykCodeScanError?.Invoke(this, new SnykCodeScanEventArgs(message));

        /// <summary>
        /// Fire SnykCode disabled event with <see cref="SnykCodeScanEventArgs"/>.
        /// </summary>
        public void FireSnykCodeDisabledError() => this.SnykCodeDisabled?.Invoke(this, new SnykCodeScanEventArgs());

        /// <summary>
        /// Fire download started.
        /// </summary>
        protected internal void OnDownloadStarted()
            => this.DownloadStarted?.Invoke(this, new SnykCliDownloadEventArgs());

        /// <summary>
        /// Fire update download started.
        /// </summary>
        protected internal void OnUpdateDownloadStarted()
            => this.DownloadStarted?.Invoke(this, new SnykCliDownloadEventArgs());

        /// <summary>
        /// Fire download finished event.
        /// </summary>
        protected internal void OnDownloadFinished() => this.DownloadFinished?.Invoke(this, new SnykCliDownloadEventArgs());

        /// <summary>
        /// Fire download cancelled event.
        /// </summary>
        /// <param name="message">Cancel message.</param>
        protected internal void OnDownloadCancelled(string message) => this.DownloadCancelled?.Invoke(this, new SnykCliDownloadEventArgs(message));

        /// <summary>
        /// Fire download update (on download progress update) event.
        /// </summary>
        /// <param name="progress">Donwload progress form 0..100$.</param>
        protected internal void OnDownloadUpdate(int progress) => this.DownloadUpdate?.Invoke(this, new SnykCliDownloadEventArgs(progress));

        private void ScanOss()
        {
            if (!this.serviceProvider.Options.OssEnabled)
            {
                return;
            }

            if (this.IsOssScanRunning())
            {
                Logger.Information("There is already a task in progress");

                return;
            }

            this.cliScanTokenSource = new CancellationTokenSource();

            var progressWorker = new SnykProgressWorker
            {
                TasksService = this,
                TokenSource = this.cliScanTokenSource,
            };

            Logger.Information("Start scan task");

            this.ossScanTask = Task.Run(
                () =>
                {
                    this.FireCliScanningStartedEvent();

                    try
                    {
                        progressWorker.CancelIfCancellationRequested();

                        if (!this.serviceProvider.SolutionService.IsSolutionOpen)
                        {
                            this.FireOssError("No open solution");

                            Logger.Information("Solution not opened");

                            return;
                        }

                        progressWorker.CancelIfCancellationRequested();

                        var options = this.serviceProvider.Options;

                        this.cli = new SnykCli
                        {
                            Options = options,
                        };

                        Logger.Information($"Snyk Extension options");
                        Logger.Information($"API token = {options.ApiToken}");
                        Logger.Information($"Custom Endpoint = {options.CustomEndpoint}");
                        Logger.Information($"Organization = {options.Organization}");
                        Logger.Information($"Ignore Unknown CA = {options.IgnoreUnknownCA}");
                        Logger.Information($"Additional Options = {options.AdditionalOptions}");
                        Logger.Information($"Is Scan All Projects = {options.IsScanAllProjects}");

                        progressWorker.CancelIfCancellationRequested();

                        try
                        {
                            string solutionPath = this.serviceProvider.SolutionService.GetSolutionPath();

                            Logger.Information($"Solution path = {solutionPath}");
                            Logger.Information("Start scan");

                            CliResult cliResult = this.cli.Scan(solutionPath);

                            progressWorker.CancelIfCancellationRequested();

                            if (!cliResult.IsSuccessful())
                            {
                                Logger.Information("Scan is successful");

                                this.FireOssError(cliResult.Error);

                                return;
                            }
                            else
                            {
                                Logger.Information("Scan update");

                                this.FireScanningUpdateEvent(cliResult);
                            }

                            progressWorker.CancelIfCancellationRequested();

                            this.FireOssScanningFinishedEvent();

                            Logger.Information("Scan finished");
                        }
                        catch (Exception scanException)
                        {
                            this.FireOssError(scanException.Message);

                            this.cli = null;

                            return;
                        }

                        progressWorker.CancelIfCancellationRequested();

                        this.cli = null;
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(exception, string.Empty);

                        this.FireScanningCancelledEvent();

                        this.cli = null;
                    }
                }, progressWorker.TokenSource.Token);
        }

        private async Task ScanSnykCodeAsync()
        {
            if (!await this.serviceProvider.ApiService.IsSnykCodeEnabledAsync())
            {
                this.FireSnykCodeDisabledError();

                return;
            }

            var options = this.serviceProvider.Options;
            if (!options.SnykCodeQualityEnabled && !options.SnykCodeSecurityEnabled)
            {
                return;
            }

            if (this.IsSnykCodeScanRunning())
            {
                Logger.Information("There is already a task in progress for SnykCode scan.");

                return;
            }

            this.snykCodeScanTokenSource = new CancellationTokenSource();

            var progressWorker = new SnykProgressWorker
            {
                TasksService = this,
                TokenSource = this.snykCodeScanTokenSource,
            };

            Logger.Information("Start scan task");

            this.snykCodeScanTask = Task.Run(
                async () =>
                {
                    this.FireSnykCodeScanningStartedEvent();

                    try
                    {
                        progressWorker.CancelIfCancellationRequested();

                        if (!this.serviceProvider.SolutionService.IsSolutionOpen)
                        {
                            this.FireOssError("No open solution");

                            Logger.Information("Solution not opened");

                            return;
                        }

                        progressWorker.CancelIfCancellationRequested();

                        await Task.Run(async () =>
                        {
                            try
                            {
                                var filesProvider = this.serviceProvider.SolutionService.NewFileProvider();

                                var analysisResult = await this.serviceProvider.SnykCodeService.ScanAsync(filesProvider, progressWorker.TokenSource.Token);

                                this.FireScanningUpdateEvent(analysisResult);

                                this.FireSnykCodeScanningFinishedEvent();
                            }
                            catch (Exception e)
                            {
                                string errorMessage = this.serviceProvider.SnykCodeService.GetSnykCodeErrorMessage(e);

                                this.OnSnykCodeError(errorMessage);
                            }
                        });

                        progressWorker.CancelIfCancellationRequested();
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(exception, string.Empty);

                        this.FireScanningCancelledEvent();
                    }
                }, progressWorker.TokenSource.Token);
        }

        /// <summary>
        /// Fire Cli scanning started event.
        /// </summary>
        private void FireCliScanningStartedEvent() => this.CliScanningStarted?.Invoke(this, new SnykCliScanEventArgs());

        /// <summary>
        /// Fire SnykCode scanning started event.
        /// </summary>
        private void FireSnykCodeScanningStartedEvent() => this.SnykCodeScanningStarted?.Invoke(this, new SnykCodeScanEventArgs());

        /// <summary>
        /// Fire scanning update with <see cref="SnykCliScanEventArgs"/> object.
        /// </summary>
        /// <param name="cliResult"><see cref="CliResult"/> object with vulnerabilities.</param>
        private void FireScanningUpdateEvent(CliResult cliResult) => this.CliScanningUpdate?.Invoke(this, new SnykCliScanEventArgs(cliResult));

        /// <summary>
        /// Fire scanning update with <see cref="SnykCodeScanEventArgs"/> object.
        /// </summary>
        /// <param name="analysisResult"><see cref="AnalysisResult"/> object with vulnerabilities.</param>
        private void FireScanningUpdateEvent(AnalysisResult analysisResult) => this.SnykCodeScanningUpdate?.Invoke(this, new SnykCodeScanEventArgs(analysisResult));

        /// <summary>
        /// Fire OSS scanning finished event.
        /// </summary>
        private void FireOssScanningFinishedEvent()
            => this.OssScanningFinished?.Invoke(this, new SnykCliScanEventArgs { SnykCodeScanRunning = this.IsSnykCodeScanRunning() });

        /// <summary>
        /// Fire SnykCode scanning finished event.
        /// </summary>
        private void FireSnykCodeScanningFinishedEvent() 
            => this.SnykCodeScanningFinished?.Invoke(this, new SnykCodeScanEventArgs { OssScanRunning = this.IsOssScanRunning() });

        /// <summary>
        /// Fire scanning cancelled event.
        /// </summary>
        private void FireScanningCancelledEvent() => this.ScanningCancelled?.Invoke(this, new SnykCliScanEventArgs());

        private bool IsOssScanRunning() => this.ossScanTask != null && this.ossScanTask.Status == TaskStatus.Running;

        private bool IsSnykCodeScanRunning() => this.snykCodeScanTask != null && this.snykCodeScanTask.Status == TaskStatus.Running;

        private bool IsDownloadRunning() => this.downloadCliTask != null && this.downloadCliTask.Status == TaskStatus.Running;
    }
}