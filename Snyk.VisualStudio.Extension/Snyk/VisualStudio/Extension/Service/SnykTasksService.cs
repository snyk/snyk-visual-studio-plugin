namespace Snyk.VisualStudio.Extension.Service
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.VisualStudio.Shell;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.VisualStudio.Extension.CLI;
    using static Snyk.VisualStudio.Extension.CLI.SnykCliDownloader;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Incapsulate logic with background tasks work.
    /// </summary>
    public class SnykTasksService
    {
        private static SnykTasksService instance;

        private CancellationTokenSource tokenSource;

        private Task currentTask;

        private ISnykServiceProvider serviceProvider;

        private SnykCli cli;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykTasksService"/> class.
        /// </summary>
        private SnykTasksService()
        {
        }

        /// <summary>
        /// Scanning started event handler.
        /// </summary>
        public event EventHandler<SnykCliScanEventArgs> ScanningStarted;

        /// <summary>
        /// Scanning finished event handler.
        /// </summary>
        public event EventHandler<SnykCliScanEventArgs> ScanningFinished;

        /// <summary>
        /// Cli Scanning update event handler.
        /// </summary>
        public event EventHandler<SnykCliScanEventArgs> CliScanningUpdate;

        /// <summary>
        /// SnykCode scanning update event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> SnykCodeScanningUpdate;

        /// <summary>
        /// Scan error event handler.
        /// </summary>
        public event EventHandler<SnykCliScanEventArgs> ScanError;

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
        /// Gets a value indicating whether VS logger.
        /// </summary>
        private SnykActivityLogger Logger => this.serviceProvider.ActivityLogger;

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

            instance.Logger.LogInformation("SnykTasksService initialized");
        }

        /// <summary>
        /// Cancel current task.
        /// </summary>
        public void CancelCurrentTask()
        {
            if (this.tokenSource != null)
            {
                this.Logger.LogInformation("Cancel current task");

                this.tokenSource.Cancel();

                this.cli?.ConsoleRunner?.Stop();
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
            this.Logger.LogInformation("Enter Scan method");

            if (this.currentTask != null && this.currentTask.Status == TaskStatus.Running)
            {
                this.Logger.LogInformation("There is already a task in progress");

                return;
            }

            this.tokenSource = new CancellationTokenSource();

            var progressWorker = new SnykProgressWorker
            {
                TasksService = this,
                TokenSource = this.tokenSource,
            };

            this.Logger.LogInformation("Start scan task");

            this.currentTask = Task.Run(
                () =>
                {
                    this.OnScanningStarted();

                    try
                    {
                        progressWorker.CancelIfCancellationRequested();

                        if (!this.serviceProvider.SolutionService.IsSolutionOpen)
                        {
                            this.OnError("No open solution.");

                            this.Logger.LogInformation("Solution not opened");

                            return;
                        }

                        progressWorker.CancelIfCancellationRequested();

                        var options = this.serviceProvider.Options;

                        this.cli = new SnykCli
                        {
                            Options = options,
                            Logger = this.Logger,
                        };

                        this.Logger.LogInformation($"Snyk Extension options");
                        this.Logger.LogInformation($"API token = {options.ApiToken}");
                        this.Logger.LogInformation($"Custom Endpoint = {options.CustomEndpoint}");
                        this.Logger.LogInformation($"Organization = {options.Organization}");
                        this.Logger.LogInformation($"Ignore Unknown CA = {options.IgnoreUnknownCA}");
                        this.Logger.LogInformation($"Additional Options = {options.AdditionalOptions}");
                        this.Logger.LogInformation($"Is Scan All Projects = {options.IsScanAllProjects}");

                        progressWorker.CancelIfCancellationRequested();

                        try
                        {
                            string solutionPath = this.serviceProvider.SolutionService.GetSolutionPath();

                            this.Logger.LogInformation($"Solution path = {solutionPath}");
                            this.Logger.LogInformation("Start scan");

                            var cliResult = this.cli.Scan(solutionPath);

                            var solutionFiles = this.serviceProvider.SolutionService.GetSolutionFiles();

                            var analysisResult = this.serviceProvider.SnykCodeService.ScanAsync(solutionFiles, solutionPath).Result;

                            progressWorker.CancelIfCancellationRequested();

                            if (!cliResult.IsSuccessful() && analysisResult == null)
                            {
                                this.Logger.LogInformation("Scan is successful");

                                this.OnError(cliResult.Error);

                                return;
                            }
                            else
                            {
                                this.Logger.LogInformation("Scan update");

                                this.OnScanningUpdate(cliResult);
                                this.OnScanningUpdate(analysisResult);
                            }

                            progressWorker.CancelIfCancellationRequested();

                            this.Logger.LogInformation("Scan finished");
                        }
                        catch (Exception scanException)
                        {
                            this.Logger.LogError(scanException.Message);

                            if ((bool)this.cli?.ConsoleRunner?.IsStopped)
                            {
                                this.OnScanningCancelled();
                            }
                            else
                            {
                                this.OnError(scanException.Message);
                            }

                            this.cli = null;

                            return;
                        }

                        progressWorker.CancelIfCancellationRequested();

                        this.OnScanningFinished();

                        this.cli = null;
                    }
                    catch (Exception exception)
                    {
                        this.Logger.LogError(exception.Message);

                        this.OnScanningCancelled();

                        this.cli = null;
                    }
                }, progressWorker.TokenSource.Token);

            this.serviceProvider.AnalyticsService.LogUserTriggersAnAnalysisEvent();
        }

        /// <summary>
        /// Start download task in background thread.
        /// </summary>
        /// <param name="downloadFinishedCallback"><see cref="CliDownloadFinishedCallback"/> callback object.</param>
        public void Download(CliDownloadFinishedCallback downloadFinishedCallback = null)
        {
            this.Logger.LogInformation("Enter Download method");

            if (this.currentTask != null && this.currentTask.Status == TaskStatus.Running)
            {
                this.Logger.LogInformation("There is already a task in progress");

                return;
            }

            this.tokenSource = new CancellationTokenSource();

            var progressWorker = new SnykProgressWorker
            {
                TasksService = this,
                TokenSource = this.tokenSource,
            };

            this.Logger.LogInformation("Start run task");

            this.currentTask = Task.Run(
                () =>
                {
                    try
                    {
                        var userStorageService = this.serviceProvider.UserStorageSettingsService;

                        string currentCliVersion = userStorageService.GetCurrentCliVersion();

                        DateTime lastCliReleaseDate = userStorageService.GetCliReleaseLastCheckDate();

                        var cliDownloader = new SnykCliDownloader(currentCliVersion, this.serviceProvider.ActivityLogger);

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
                        this.Logger.LogInformation(exception.Message);

                        this.OnDownloadCancelled(exception.Message);
                    }
                }, progressWorker.TokenSource.Token);
        }

        /// <summary>
        /// Fire error event. Create <see cref="CliError"/> instance.
        /// </summary>
        /// <param name="message">Error message.</param>
        public void OnError(string message) => this.OnError(new CliError(message));

        /// <summary>
        /// Fire error event with <see cref="SnykCliScanEventArgs"/>.
        /// </summary>
        /// <param name="error"><see cref="CliError"/> object.</param>
        public void OnError(CliError error) => this.ScanError?.Invoke(this, new SnykCliScanEventArgs(error));

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

        /// <summary>
        /// Fire scanning started event.
        /// </summary>
        private void OnScanningStarted() => this.ScanningStarted?.Invoke(this, new SnykCliScanEventArgs());

        /// <summary>
        /// Fire scanning update with <see cref="SnykCliScanEventArgs"/> object.
        /// </summary>
        /// <param name="cliResult"><see cref="CliResult"/> object with vulnerabilities.</param>
        private void OnScanningUpdate(CliResult cliResult) => this.CliScanningUpdate?.Invoke(this, new SnykCliScanEventArgs(cliResult));

        /// <summary>
        /// Fire scanning update with <see cref="SnykCodeScanEventArgs"/> object.
        /// </summary>
        /// <param name="analysisResult"><see cref="AnalysisResult"/> object with vulnerabilities.</param>
        private void OnScanningUpdate(AnalysisResult analysisResult) => this.SnykCodeScanningUpdate?.Invoke(this, new SnykCodeScanEventArgs(analysisResult));

        /// <summary>
        /// Fire scanning finished event.
        /// </summary>
        private void OnScanningFinished() => this.ScanningFinished?.Invoke(this, new SnykCliScanEventArgs());

        /// <summary>
        /// Fire scanning cancelled event.
        /// </summary>
        private void OnScanningCancelled() => this.ScanningCancelled?.Invoke(this, new SnykCliScanEventArgs());
    }
}