namespace Snyk.VisualStudio.Extension.Shared.Service
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.VisualStudio.Shell;
    using Serilog;
    using Snyk.Analytics;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.CLI;
    using Snyk.VisualStudio.Extension.Shared.CLI.Download;
    using Snyk.VisualStudio.Extension.Shared.Service.Domain;
    using static Snyk.VisualStudio.Extension.Shared.CLI.Download.SnykCliDownloader;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Incapsulate logic with background tasks work.
    /// </summary>
    public class SnykTasksService
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykTasksService>();

        private static SnykTasksService instance;

        private CancellationTokenSource ossScanTokenSource;

        private CancellationTokenSource snykCodeScanTokenSource;

        private CancellationTokenSource downloadCliTokenSource;

        private bool isOssScanning;

        private bool isSnykCodeScanning;

        private bool isCliDownloading;

        private ISnykServiceProvider serviceProvider;

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
        public event EventHandler<SnykCliScanEventArgs> OssScanningUpdate;

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
        /// Task finished event.
        /// </summary>
        public event EventHandler<EventArgs> TaskFinished;

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
        public bool IsTaskRunning() => this.isOssScanning || this.isSnykCodeScanning || this.isCliDownloading;

        /// <summary>
        /// Cancel current task.
        /// </summary>
        public void CancelTasks()
        {
            try
            {
                this.isOssScanning = false;
                this.isSnykCodeScanning = false;
                this.isCliDownloading = false;

                this.CancelTask(this.ossScanTokenSource);
                this.CancelTask(this.snykCodeScanTokenSource);
                this.CancelTask(this.downloadCliTokenSource);

                this.serviceProvider.OssService.StopScan();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error on cancel tasks");
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
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ScanAsync()
        {
            Logger.Information("Enter Scan method");

            try
            {
                var selectedFeatures = await this.GetFeaturesSettingsAsync();

                if (!this.serviceProvider.SolutionService.IsSolutionOpen())
                {
                    this.FireOssError("No open solution", selectedFeatures);

                    Logger.Information("Solution not opened");

                    return;
                }

                this.serviceProvider.AnalyticsService.LogAnalysisIsTriggeredEvent(this.GetSelectedFeatures(selectedFeatures));

                var ossScanTask = this.ScanOssAsync(selectedFeatures);
                var snykCodeScanTask = this.ScanSnykCodeAsync(selectedFeatures);

                await Task.WhenAll(ossScanTask, snykCodeScanTask);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error on scan");
            }
        }

        /// <summary>
        /// Start download task in background thread.
        /// </summary>
        /// <param name="downloadFinishedCallback"><see cref="CliDownloadFinishedCallback"/> callback object.</param>
        public void Download(CliDownloadFinishedCallback downloadFinishedCallback = null)
        {
            Logger.Information("Enter Download method");

            try
            {
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

                _ = Task.Run(
                    async () =>
                    {
                        try
                        {
                            await this.DownloadAsync(downloadFinishedCallback, progressWorker);
                        }
                        catch (ChecksumVerificationException e)
                        {
                            Logger.Error(e, "Cli download failed. Checksum don't match. Try to download again...");

                            await this.RetryDownloadAsync(downloadFinishedCallback, progressWorker);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "Error on cli download");

                            this.OnDownloadCancelled(e.Message);
                        }
                        finally
                        {
                            if (progressWorker.IsWorkFinished)
                            {
                                this.isCliDownloading = false;

                                this.DisposeCancellationTokenSource(this.downloadCliTokenSource);

                                this.FireTaskFinished();
                            }
                        }
                    }, progressWorker.TokenSource.Token);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error on CLI download");
            }
        }

        /// <summary>
        /// Fire on task finished (oss scan or snykcode scan or cli download).
        /// </summary>
        /// <param name="message">Error message.</param>
        public void FireTaskFinished() => this.TaskFinished?.Invoke(this, new EventArgs());

        /// <summary>
        /// Fire error event. Create <see cref="CliError"/> instance.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="featuresSettings">Features settings.</param>
        public void FireOssError(string message, FeaturesSettings featuresSettings = null)
            => this.FireOssError(new CliError(message), featuresSettings);

        /// <summary>
        /// Fire error event with <see cref="SnykCliScanEventArgs"/>.
        /// </summary>
        /// <param name="error"><see cref="CliError"/> object.</param>
        /// <param name="featuresSettings">Features settings.</param>
        public void FireOssError(CliError error, FeaturesSettings featuresSettings = null)
            => this.OssScanError?.Invoke(this, new SnykCliScanEventArgs(error, featuresSettings));

        /// <summary>
        /// Fire error event with <see cref="SnykCodeScanEventArgs"/>.
        /// </summary>
        /// <param name="message">Error message</param>
        public void OnSnykCodeError(string message) => this.SnykCodeScanError?.Invoke(this, new SnykCodeScanEventArgs(message));

        /// <summary>
        /// Fire SnykCode disabled event with <see cref="SnykCodeScanEventArgs"/>.
        /// </summary>
        /// <param name="localCodeEngineEnabled">Send local code engine enabled/disabled on server in event.</param>
        public void FireSnykCodeDisabledError(bool localCodeEngineEnabled)
            => this.SnykCodeDisabled?.Invoke(this, new SnykCodeScanEventArgs { LocalCodeEngineEnabled = localCodeEngineEnabled, });

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

        private async Task ScanOssAsync(FeaturesSettings featuresSettings)
        {
            try
            {
                if (!featuresSettings.OssEnabled)
                {
                    return;
                }

                if (this.isOssScanning)
                {
                    Logger.Information("There is already a task in progress");

                    return;
                }

                this.ossScanTokenSource = new CancellationTokenSource();
                var token = this.ossScanTokenSource.Token;

                Logger.Information("Start scan task");

                await Task.Run(() => this.RunOssScanAsync(featuresSettings));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error on oss scan");
            }
        }

        private async Task RunOssScanAsync(FeaturesSettings featuresSettings)
        {
            this.isOssScanning = true;

            this.FireOssScanningStartedEvent();

            var ossService = this.serviceProvider.OssService;

            try
            {
                var token = this.ossScanTokenSource.Token;

                token.ThrowIfCancellationRequested();

                try
                {
                    var directoryPath = await this.serviceProvider.SolutionService.GetSolutionFolderAsync();

                    var cliResult = await ossService.ScanAsync(directoryPath, token);

                    this.FireOssScanningUpdateEvent(cliResult);

                    this.FireOssScanningFinishedEvent();

                    Logger.Information("Scan finished");
                }
                catch (OssScanException e)
                {
                    Logger.Error(e, "Oss scan exception");

                    this.FireOssError(e.Error, featuresSettings);
                }
                catch (Exception e)
                {
                    if (ossService.IsCurrentScanProcessCanceled() || this.IsTaskCancelled(e))
                    {
                        this.FireScanningCancelledEvent();

                        return;
                    }

                    this.FireOssError(e.Message, featuresSettings);
                }
                finally
                {
                    this.isOssScanning = false;

                    this.FireTaskFinished();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error on oss scan");

                this.FireScanningCancelledEvent();
            }
            finally
            {
                this.DisposeCancellationTokenSource(this.ossScanTokenSource);

                this.isOssScanning = false;

                this.FireTaskFinished();
            }
        }

        private async Task ScanSnykCodeAsync(FeaturesSettings featuresSettings)
        {
            try
            {
                if (!featuresSettings.SastOnServerEnabled)
                {
                    this.FireSnykCodeDisabledError(featuresSettings.LocalCodeEngineEnabled);

                    return;
                }

                if (!featuresSettings.CodeQualityEnabled && !featuresSettings.CodeSecurityEnabled)
                {
                    return;
                }

                if (this.isSnykCodeScanning)
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

                await Task.Run(() => this.RunSnykCodeScanAsync(progressWorker.TokenSource.Token));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error on SnykCode scan");
            }
        }

        private async Task RunSnykCodeScanAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.isSnykCodeScanning = true;

                this.FireSnykCodeScanningStartedEvent();

                var fileProvider = this.serviceProvider.SolutionService.FileProvider;

                var analysisResult = await this.serviceProvider.SnykCodeService.ScanAsync(fileProvider, cancellationToken);

                this.FireScanningUpdateEvent(analysisResult);

                this.FireSnykCodeScanningFinishedEvent();
            }
            catch (Exception e)
            {
                if (this.IsTaskCancelled(e))
                {
                    this.FireScanningCancelledEvent();

                    return;
                }

                Logger.Error(e, "Error on Run Snyk Code scan");

                string errorMessage = this.serviceProvider.SnykCodeService.GetSnykCodeErrorMessage(e);

                this.OnSnykCodeError(errorMessage);
            }
            finally
            {
                this.DisposeCancellationTokenSource(this.snykCodeScanTokenSource);

                this.isSnykCodeScanning = false;

                this.FireTaskFinished();
            }
        }

        private bool IsTaskCancelled(Exception sourceException)
        {
            if (sourceException is AggregateException)
            {
                var agException = (AggregateException)sourceException;

                foreach (var exception in agException.Flatten().InnerExceptions)
                {
                    if (this.IsTaskCancelled(exception))
                    {
                        return true;
                    }
                }
            }

            if (sourceException is OperationCanceledException)
            {
                var canceledException = (OperationCanceledException)sourceException;

                if (canceledException.CancellationToken.IsCancellationRequested)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Fire Cli scanning started event.
        /// </summary>
        private void FireOssScanningStartedEvent() => this.CliScanningStarted?.Invoke(this, new SnykCliScanEventArgs());

        /// <summary>
        /// Fire SnykCode scanning started event.
        /// </summary>
        private void FireSnykCodeScanningStartedEvent() => this.SnykCodeScanningStarted?.Invoke(this, new SnykCodeScanEventArgs());

        /// <summary>
        /// Fire scanning update with <see cref="SnykCliScanEventArgs"/> object.
        /// </summary>
        /// <param name="cliResult"><see cref="CliResult"/> object with vulnerabilities.</param>
        private void FireOssScanningUpdateEvent(CliResult cliResult) => this.OssScanningUpdate?.Invoke(this, new SnykCliScanEventArgs(cliResult));

        /// <summary>
        /// Fire scanning update with <see cref="SnykCodeScanEventArgs"/> object.
        /// </summary>
        /// <param name="analysisResult"><see cref="AnalysisResult"/> object with vulnerabilities.</param>
        private void FireScanningUpdateEvent(AnalysisResult analysisResult) => this.SnykCodeScanningUpdate?.Invoke(this, new SnykCodeScanEventArgs(analysisResult));

        /// <summary>
        /// Fire OSS scanning finished event.
        /// </summary>
        private void FireOssScanningFinishedEvent()
            => this.OssScanningFinished?.Invoke(this, new SnykCliScanEventArgs { SnykCodeScanRunning = this.isSnykCodeScanning });

        /// <summary>
        /// Fire SnykCode scanning finished event.
        /// </summary>
        private void FireSnykCodeScanningFinishedEvent()
            => this.SnykCodeScanningFinished?.Invoke(this, new SnykCodeScanEventArgs { OssScanRunning = this.isOssScanning });

        /// <summary>
        /// Fire scanning cancelled event.
        /// </summary>
        private void FireScanningCancelledEvent() => this.ScanningCancelled?.Invoke(this, new SnykCliScanEventArgs());

        private async Task<FeaturesSettings> GetFeaturesSettingsAsync()
        {
            var options = this.serviceProvider.Options;

            var sastSettings = await this.serviceProvider.ApiService.GetSastSettingsAsync();

            bool snykCodeEnabled = sastSettings?.SnykCodeEnabled ?? false;

            return new FeaturesSettings
            {
                OssEnabled = options.OssEnabled,
                SastOnServerEnabled = snykCodeEnabled,
                CodeSecurityEnabled = snykCodeEnabled && options.SnykCodeSecurityEnabled,
                CodeQualityEnabled = snykCodeEnabled && options.SnykCodeQualityEnabled,
                LocalCodeEngineEnabled = sastSettings?.LocalCodeEngineEnabled ?? false,
            };
        }

        private IList<AnalysisType> GetSelectedFeatures(FeaturesSettings featuresSettings)
        {
            var selectedProducts = new List<AnalysisType>();

            if (featuresSettings.OssEnabled)
            {
                selectedProducts.Add(AnalysisType.SnykOpenSource);
            }

            if (featuresSettings.SastOnServerEnabled)
            {
                if (featuresSettings.CodeSecurityEnabled)
                {
                    selectedProducts.Add(AnalysisType.SnykCodeSecurity);
                }

                if (featuresSettings.CodeQualityEnabled)
                {
                    selectedProducts.Add(AnalysisType.SnykCodeQuality);
                }
            }

            return selectedProducts;
        }

        private void CancelTask(CancellationTokenSource tokenSource)
        {
            try
            {
                if (tokenSource != null)
                {
                    Logger.Information("Cancel task");

                    tokenSource.Cancel();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Try to cancel task");
            }
            finally
            {
                tokenSource = null;
            }
        }

        private void DisposeCancellationTokenSource(CancellationTokenSource tokenSource)
        {
            try
            {
                tokenSource?.Dispose();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Try to dispose token source.");
            }
            finally
            {
                tokenSource = null;
            }
        }

        private async Task DownloadAsync(CliDownloadFinishedCallback downloadFinishedCallback, ISnykProgressWorker progressWorker)
        {
            var userSettingsStorageService = this.serviceProvider.UserStorageSettingsService;
            if (!userSettingsStorageService.CliAutoUpdate)
            {
                Logger.Information("CLI auto-update is disabled, CLI download is skipped.");
                return;
            }

            this.isCliDownloading = true;
            string currentCliVersion = userSettingsStorageService.GetCurrentCliVersion();

            DateTime lastCliReleaseDate = userSettingsStorageService.GetCliReleaseLastCheckDate();

            var cliDownloader = new SnykCliDownloader(currentCliVersion);

            List<CliDownloadFinishedCallback> downloadFinishedCallbacks = new List<CliDownloadFinishedCallback>();

            if (downloadFinishedCallback != null)
            {
                downloadFinishedCallbacks.Add(downloadFinishedCallback);
            }

            downloadFinishedCallbacks.Add(() =>
            {
                userSettingsStorageService.SaveCurrentCliVersion(cliDownloader.GetLatestReleaseInfo().Name);
                userSettingsStorageService.SaveCliReleaseLastCheckDate(DateTime.UtcNow);

                this.isCliDownloading = false;

                this.DisposeCancellationTokenSource(this.downloadCliTokenSource);
            });

            await cliDownloader.AutoUpdateCliAsync(
                progressWorker,
                lastCliReleaseDate,
                downloadFinishedCallbacks: downloadFinishedCallbacks);
        }

        private async Task RetryDownloadAsync(CliDownloadFinishedCallback downloadFinishedCallback, SnykProgressWorker progressWorker)
        {
            try
            {
                await this.DownloadAsync(downloadFinishedCallback, progressWorker);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Cli retry download failed");

                this.OnDownloadCancelled($"The download of the Snyk CLI was not successful. The integrity check failed ({e.Message})");
            }
            finally
            {
                if (progressWorker.IsWorkFinished)
                {
                    this.isCliDownloading = false;

                    this.DisposeCancellationTokenSource(this.downloadCliTokenSource);
                }
            }
        }
    }
}