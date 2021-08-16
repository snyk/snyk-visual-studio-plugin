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
    using Snyk.VisualStudio.Extension.UI;
    using static Snyk.VisualStudio.Extension.CLI.SnykCliDownloader;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Incapsulate logic with background tasks work.
    /// </summary>
    public class SnykTasksService
    {
        private static SnykTasksService instance;

        private CancellationTokenSource cliScanTokenSource;

        private CancellationTokenSource snykCodeScanTokenSource;

        private Task cliScanTask;

        private Task snykCodeScanTask;

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
        /// Sli scan error event handler.
        /// </summary>
        public event EventHandler<SnykCliScanEventArgs> CliScanError;

        /// <summary>
        /// SnykCode scan error event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> SnykCodeScanError;

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
            if (this.cliScanTokenSource != null)
            {
                this.Logger.LogInformation("Cancel current task");

                this.cliScanTokenSource.Cancel();

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

            var options = this.serviceProvider.Options;

            if (options.OssEnabled)
            {
                this.ScanCli();
            }

            if (options.SnykCodeQualityEnabled || options.SnykCodeSecurityEnabled)
            {
                this.ScanSnykCode();
            }

            this.serviceProvider.AnalyticsService.LogUserTriggersAnAnalysisEvent();
        }

        /// <summary>
        /// Start download task in background thread.
        /// </summary>
        /// <param name="downloadFinishedCallback"><see cref="CliDownloadFinishedCallback"/> callback object.</param>
        public void Download(CliDownloadFinishedCallback downloadFinishedCallback = null)
        {
            this.Logger.LogInformation("Enter Download method");

            if (this.cliScanTask != null && this.cliScanTask.Status == TaskStatus.Running)
            {
                this.Logger.LogInformation("There is already a task in progress");

                return;
            }

            this.cliScanTokenSource = new CancellationTokenSource();

            var progressWorker = new SnykProgressWorker
            {
                TasksService = this,
                TokenSource = this.cliScanTokenSource,
            };

            this.Logger.LogInformation("Start run task");

            this.cliScanTask = Task.Run(
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
        public void OnCliError(string message) => this.OnCliError(new CliError(message));

        /// <summary>
        /// Fire error event with <see cref="SnykCliScanEventArgs"/>.
        /// </summary>
        /// <param name="error"><see cref="CliError"/> object.</param>
        public void OnCliError(CliError error) => this.CliScanError?.Invoke(this, new SnykCliScanEventArgs(error));

        /// <summary>
        /// Fire error event with <see cref="SnykCodeScanEventArgs"/>.
        /// </summary>
        public void OnSnykCodeError() => this.SnykCodeScanError?.Invoke(this, new SnykCodeScanEventArgs());

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

        private void ScanCli()
        {
            if (this.cliScanTask != null && this.cliScanTask.Status == TaskStatus.Running)
            {
                this.Logger.LogInformation("There is already a task in progress");

                return;
            }

            VsStatusBar.Instance.DisplayMessage("Cli scan started.");

            this.cliScanTokenSource = new CancellationTokenSource();

            var progressWorker = new SnykProgressWorker
            {
                TasksService = this,
                TokenSource = this.cliScanTokenSource,
            };

            this.Logger.LogInformation("Start scan task");

            this.cliScanTask = Task.Run(
                () =>
                {
                    this.FireCliScanningStartedEvent();

                    try
                    {
                        progressWorker.CancelIfCancellationRequested();

                        if (!this.serviceProvider.SolutionService.IsSolutionOpen)
                        {
                            MessageBox.Show("No open solution", "Snyk");

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

                            VsStatusBar.Instance.DisplayMessage("Cli scanтing...");

                            CliResult cliResult = this.cli.Scan(solutionPath);

                            progressWorker.CancelIfCancellationRequested();

                            if (!cliResult.IsSuccessful())
                            {
                                this.Logger.LogInformation("Scan is successful");

                                this.OnCliError(cliResult.Error);

                                return;
                            }
                            else
                            {
                                this.Logger.LogInformation("Scan update");

                                this.FireScanningUpdateEvent(cliResult);
                            }

                            progressWorker.CancelIfCancellationRequested();

                            this.Logger.LogInformation("Scan finished");
                        }
                        catch (Exception scanException)
                        {
                            var exception = scanException;

                            if (scanException is AggregateException)
                            {
                                exception = scanException.InnerException;
                            }

                            this.Logger.LogError(exception.Message);

                            if ((bool)this.cli?.ConsoleRunner?.IsStopped)
                            {
                                this.FireScanningCancelledEvent();
                            }
                            else
                            {
                                this.OnCliError(exception.Message);
                            }

                            VsStatusBar.Instance.ShowMessageBoxAsync("Snyk CLI", exception.Message);

                            VsStatusBar.Instance.DisplayMessage("Scan finished");

                            this.cli = null;

                            return;
                        }

                        progressWorker.CancelIfCancellationRequested();

                        this.FireScanningFinishedEvent();

                        VsStatusBar.Instance.DisplayMessage("Cli scan finished.");

                        this.cli = null;
                    }
                    catch (Exception exception)
                    {
                        this.Logger.LogError(exception.Message);

                        this.FireScanningCancelledEvent();

                        this.cli = null;
                    }
                }, progressWorker.TokenSource.Token);
        }

        private void ScanSnykCode()
        {
            if (this.snykCodeScanTask != null && this.snykCodeScanTask.Status == TaskStatus.Running)
            {
                this.Logger.LogInformation("There is already a task in progress for SnykCode scan.");

                return;
            }

            VsStatusBar.Instance.DisplayMessage("Scan started.");

            this.snykCodeScanTokenSource = new CancellationTokenSource();

            var progressWorker = new SnykProgressWorker
            {
                TasksService = this,
                TokenSource = this.snykCodeScanTokenSource,
            };

            this.Logger.LogInformation("Start scan task");

            this.snykCodeScanTask = Task.Run(
                () =>
                {
                    this.FireSnykCodeScanningStartedEvent();

                    try
                    {
                        progressWorker.CancelIfCancellationRequested();

                        if (!this.serviceProvider.SolutionService.IsSolutionOpen)
                        {
                            MessageBox.Show("No open solution", "Snyk");

                            this.Logger.LogInformation("Solution not opened");

                            return;
                        }

                        progressWorker.CancelIfCancellationRequested();

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var filesProvider = this.serviceProvider.SolutionService.NewFileProvider();

                                var analysisResult = await this.serviceProvider.SnykCodeService.ScanAsync(filesProvider);

                                this.FireScanningUpdateEvent(analysisResult);
                            }
                            catch (Exception exception)
                            {
                                this.OnSnykCodeError();
                            }
                        });

                        progressWorker.CancelIfCancellationRequested();
                    }
                    catch (Exception exception)
                    {
                        this.Logger.LogError(exception.Message);

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
        /// Fire scanning finished event.
        /// </summary>
        private void FireScanningFinishedEvent() => this.ScanningFinished?.Invoke(this, new SnykCliScanEventArgs());

        /// <summary>
        /// Fire scanning cancelled event.
        /// </summary>
        private void FireScanningCancelledEvent() => this.ScanningCancelled?.Invoke(this, new SnykCliScanEventArgs());
    }
}