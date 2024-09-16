using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.Common;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service.Domain;
using static Snyk.VisualStudio.Extension.Download.SnykCliDownloader;
using Task = System.Threading.Tasks.Task;

namespace Snyk.VisualStudio.Extension.Service
{
    /// <summary>
    /// Incapsulate logic with background tasks work.
    /// </summary>
    public class SnykTasksService
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykTasksService>();

        private static SnykTasksService instance;

        public CancellationTokenSource SnykScanTokenSource { get; set; } = new CancellationTokenSource();

        private CancellationTokenSource downloadCliTokenSource;

        public bool IsOssScanning { get; set; }

        public bool IsSnykCodeScanning { get; set; }
        public bool IsIacScanning { get; set; }

        private bool isCliDownloading;

        private ISnykServiceProvider serviceProvider;

        private readonly object cancelTasksLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykTasksService"/> class.
        /// </summary>
        private SnykTasksService()
        {
        }

        /// <summary>
        /// Cli scanning started event handler.
        /// </summary>
        public event EventHandler<SnykOssScanEventArgs> OssScanningStarted;

        /// <summary>
        /// Cli Scanning update event handler.
        /// </summary>
        public event EventHandler<SnykOssScanEventArgs> OssScanningUpdate;

        /// <summary>
        /// OSS Scanning Disabled event handler.
        /// </summary>
        public event EventHandler<SnykOssScanEventArgs> OssScanningDisabled;

        /// <summary>
        /// Scanning OSS finished event handler.
        /// </summary>
        public event EventHandler<SnykOssScanEventArgs> OssScanningFinished;


        /// <summary>
        /// Cli scan error event handler.
        /// </summary>
        public event EventHandler<SnykOssScanEventArgs> OssScanError;

        /// <summary>
        /// IaC scanning started event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> IacScanningStarted;

        /// <summary>
        /// Iac scanning update event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> IacScanningUpdate;

        /// <summary>
        /// IaC Scanning Disabled event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> IacScanningDisabled;

        /// <summary>
        /// Scanning IaC finished event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> IacScanningFinished;

        /// <summary>
        /// Iac scan error event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> IacScanError;

        /// <summary>
        /// SnykCode scanning started event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> SnykCodeScanningStarted;

        /// <summary>
        /// Scanning SnykCode finished event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> SnykCodeScanningFinished;

        /// <summary>
        /// SnykCode scanning update event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> SnykCodeScanningUpdate;

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
        public event EventHandler<SnykOssScanEventArgs> ScanningCancelled;

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
        /// Download cancelled event handler. Raised when the user cancels the download intentionally.
        /// </summary>
        public event EventHandler<SnykCliDownloadEventArgs> DownloadCancelled;

        /// <summary>
        /// Download failed event handler. Raised when the download fails due to an error.
        /// </summary>
        public event EventHandler<Exception> DownloadFailed;

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
        public bool IsTaskRunning() => this.IsOssScanning || this.IsSnykCodeScanning || this.isCliDownloading;

        /// <summary>
        /// Cancel current task.
        /// </summary>
        public void CancelTasks()
        {
            lock (this.cancelTasksLock)
            {
                try
                {
                    this.IsOssScanning = false;
                    this.IsSnykCodeScanning = false;
                    this.isCliDownloading = false;

                    this.CancelTask(this.SnykScanTokenSource);
                    this.CancelTask(this.downloadCliTokenSource);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error on cancel tasks");
                }
            }
        }

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

                var isFolderTrusted = await this.IsFolderTrustedAsync();
                if (!isFolderTrusted)
                {
                    Logger.Information("Workspace folder was not trusted for scanning.");
                    return;
                }

                if (!selectedFeatures.OssEnabled)
                {
                    FireOssScanningDisabledEvent();
                }

                if (!selectedFeatures.SastOnServerEnabled)
                {
                    FireSnykCodeDisabledError(selectedFeatures.LocalCodeEngineEnabled);
                }

                if (!selectedFeatures.IacEnabled)
                {
                    FireSnykIacDisabledError(selectedFeatures.IacEnabled);
                }

                var componentModel = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;

                Assumes.Present(componentModel);
                var languageServerClientManager = componentModel.GetService<ILanguageClientManager>();
                var progressWorker = new SnykProgressWorker
                {
                    TasksService = this,
                    TokenSource = this.SnykScanTokenSource,
                };

                await languageServerClientManager.InvokeWorkspaceScanAsync(progressWorker.TokenSource.Token);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error on scan");
            }
        }

        /// <summary>
        /// Checks if opened solution folder is trusted. If not, prompts a user with trust permission.
        /// </summary>
        /// <returns>Folder is trusted or not.</returns>
        public async Task<bool> IsFolderTrustedAsync()
        {
            var solutionFolderPath = await this.serviceProvider.SolutionService.GetSolutionFolderAsync();
            var isFolderTrusted = this.serviceProvider.WorkspaceTrustService.IsFolderTrusted(solutionFolderPath);

            if (string.IsNullOrEmpty(solutionFolderPath) || isFolderTrusted)
            {
                return true;
            }

            var trustDialog = new TrustDialogWindow(solutionFolderPath);
            var trusted = trustDialog.ShowModal();

            if (trusted != true)
            {
                return false;
            }

            try
            {
                this.serviceProvider.WorkspaceTrustService.AddFolderToTrusted(solutionFolderPath);
                Logger.Information("Workspace folder was trusted: {SolutionFolderPath}", solutionFolderPath);
                return true;
            }
            catch (ArgumentException e)
            {
                Logger.Error(e, "Failed to add folder to trusted list.");
                throw;
            }
        }

        /// <summary>
        /// Start a CLI download task in background thread. Will only download the CLI if it's missing or outdated.
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
                ThreadHelper.JoinableTaskFactory.RunAsync(
                    async () =>
                    {
                        try
                        {
                            await this.DownloadAsync(downloadFinishedCallback, progressWorker);
                        }
                        catch (ChecksumVerificationException e)
                        {
                            Logger.Error(
                                e,
                                "Cli download failed due to failed checksum. {Expected} (expected) != {Actual}",
                                e.ExpectedHash,
                                e.ActualHash);

                            this.OnDownloadFailed(e);
                        }
                        catch (OperationCanceledException e)
                        {
                            Logger.Information("CLI Download cancelled");
                            this.OnDownloadCancelled(e.Message);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "Error on cli download");

                            this.OnDownloadFailed(e);
                        }
                        finally
                        {
                            this.isCliDownloading = false;

                            if (progressWorker.IsWorkFinished)
                            {
                                DisposeCancellationTokenSource(this.downloadCliTokenSource);

                                this.FireTaskFinished();
                            }
                        }
                    }).FireAndForget();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error on CLI download");
            }
        }

        public async Task DownloadAsync(CliDownloadFinishedCallback downloadFinishedCallback = null)
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

            try
            {
                await this.DownloadAsync(downloadFinishedCallback, progressWorker);
            }
            catch (ChecksumVerificationException e)
            {
                Logger.Error(
                    e,
                    "Cli download failed due to failed checksum. {Expected} (expected) != {Actual}",
                    e.ExpectedHash,
                    e.ActualHash);

                this.OnDownloadFailed(e);
            }
            catch (OperationCanceledException e)
            {
                Logger.Information("CLI Download cancelled");
                this.OnDownloadCancelled(e.Message);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error on cli download");

                this.OnDownloadFailed(e);
            }
            finally
            {
                this.isCliDownloading = false;

                if (progressWorker.IsWorkFinished)
                {
                    DisposeCancellationTokenSource(this.downloadCliTokenSource);

                    this.FireTaskFinished();
                }
            }
        }

        /// <summary>
        /// Fire on task finished (oss scan or snykcode scan or cli download).
        /// </summary>
        /// <param name="message">Error message.</param>
        public void FireTaskFinished() => this.TaskFinished?.Invoke(this, new EventArgs());

        /// <summary>
        /// Fire error event. Create <see cref="OssError"/> instance.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="featuresSettings">Features settings.</param>
        public void FireOssError(string message, FeaturesSettings featuresSettings = null)
        {
            this.IsOssScanning = false;
            this.FireOssError(new OssError(message), featuresSettings);
        }

        /// <summary>
        /// Fire error event with <see cref="SnykOssScanEventArgs"/>.
        /// </summary>
        /// <param name="error"><see cref="OssError"/> object.</param>
        /// <param name="featuresSettings">Features settings.</param>
        public void FireOssError(OssError error, FeaturesSettings featuresSettings = null)
            => this.OssScanError?.Invoke(this, new SnykOssScanEventArgs(error, featuresSettings));

        /// <summary>
        /// Fire error event with <see cref="SnykCodeScanEventArgs"/>.
        /// </summary>
        /// <param name="message">Error message</param>
        public void OnSnykCodeError(string message)
        {
            this.IsSnykCodeScanning = false;
            this.SnykCodeScanError?.Invoke(this, new SnykCodeScanEventArgs(message));
        }

        public void OnIacError(string message)
        {
            this.IsIacScanning = false;
            this.IacScanError?.Invoke(this, new SnykCodeScanEventArgs(message));
        }

        /// <summary>
        /// Fire SnykCode disabled event with <see cref="SnykCodeScanEventArgs"/>.
        /// </summary>
        /// <param name="localCodeEngineEnabled">Send local code engine enabled/disabled on server in event.</param>
        public void FireSnykCodeDisabledError(bool localCodeEngineEnabled)
            => this.SnykCodeDisabled?.Invoke(this,
                new SnykCodeScanEventArgs { LocalCodeEngineEnabled = localCodeEngineEnabled, });

        public void FireSnykIacDisabledError(bool localCodeEngineEnabled)
            => this.IacScanningDisabled?.Invoke(this,
                new SnykCodeScanEventArgs { LocalCodeEngineEnabled = localCodeEngineEnabled, });


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
        protected internal void OnDownloadFinished() =>
            this.DownloadFinished?.Invoke(this, new SnykCliDownloadEventArgs());

        /// <summary>
        /// Fire download cancelled event.
        /// </summary>
        /// <param name="message">Cancel message.</param>
        protected internal void OnDownloadCancelled(string message) =>
            this.DownloadCancelled?.Invoke(this, new SnykCliDownloadEventArgs(message));

        /// <summary>
        /// Fire download cancelled event.
        /// </summary>
        /// <param name="exception">The exception that caused the download to fail.</param>
        protected internal void OnDownloadFailed(Exception exception) => this.DownloadFailed?.Invoke(this, exception);

        /// <summary>
        /// Fire download update (on download progress update) event.
        /// </summary>
        /// <param name="progress">Donwload progress form 0..100$.</param>
        protected internal void OnDownloadUpdate(int progress) =>
            this.DownloadUpdate?.Invoke(this, new SnykCliDownloadEventArgs(progress));

        private static void DisposeCancellationTokenSource(CancellationTokenSource tokenSource)
        {
            try
            {
                tokenSource?.Dispose();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error when trying to dispose cancellation token source");
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
        public void FireOssScanningStartedEvent()
        {
            this.IsOssScanning = true;
            this.OssScanningStarted?.Invoke(this, new SnykOssScanEventArgs());
        }


        /// <summary>
        /// Fire OSS scanning disabled event.
        /// </summary>
        private void FireOssScanningDisabledEvent() => this.OssScanningDisabled?.Invoke(this, new SnykOssScanEventArgs());


        /// <summary>
        /// Fire SnykCode scanning started event.
        /// </summary>
        public void FireSnykCodeScanningStartedEvent(FeaturesSettings featuresSettings)
        {
            this.IsSnykCodeScanning = true;
            this.SnykCodeScanningStarted?.Invoke(this,
                new SnykCodeScanEventArgs
                {
                    QualityScanEnabled = featuresSettings.CodeQualityEnabled,
                    CodeScanEnabled = featuresSettings.CodeSecurityEnabled
                });
        }

        public void FireIacScanningStartedEvent(FeaturesSettings featuresSettings)
        {
            this.IsIacScanning = true;
            this.IacScanningStarted?.Invoke(this,
                new SnykCodeScanEventArgs
                {
                    IacEnabled = featuresSettings.IacEnabled
                });
        }

        /// <summary>
        /// Fire scanning update with <see cref="SnykOssScanEventArgs"/> object.
        /// </summary>
        public void FireOssScanningUpdateEvent(IDictionary<string, IEnumerable<Issue>> scanResult)
        {
            this.IsOssScanning = true;
            this.OssScanningUpdate?.Invoke(this, new SnykOssScanEventArgs(scanResult));
        }

        /// <summary>
        /// Fire scanning update with <see cref="SnykCodeScanEventArgs"/> object.
        /// </summary>
        /// <param name="analysisResult"><see cref="AnalysisResult"/> object with vulnerabilities.</param>
        public void FireCodeScanningUpdateEvent(IDictionary<string, IEnumerable<Issue>> analysisResult)
        {
            this.IsSnykCodeScanning = true;
            this.SnykCodeScanningUpdate?.Invoke(this, new SnykCodeScanEventArgs(analysisResult));
        }

        public void FireIacScanningUpdateEvent(IDictionary<string, IEnumerable<Issue>> analysisResult)
        {
            this.IsIacScanning = true;
            this.IacScanningUpdate?.Invoke(this, new SnykCodeScanEventArgs(analysisResult));
        }


        /// <summary>
        /// Fire OSS scanning finished event.
        /// </summary>
        public void FireOssScanningFinishedEvent()
        {
            this.IsOssScanning = false;
            this.OssScanningFinished?.Invoke(this,
                new SnykOssScanEventArgs { SnykCodeScanRunning = this.IsSnykCodeScanning });
        }

        /// <summary>
        /// Fire SnykCode scanning finished event.
        /// </summary>
        public void FireSnykCodeScanningFinishedEvent()
        {
            this.IsSnykCodeScanning = false;
            this.SnykCodeScanningFinished?.Invoke(this,
                new SnykCodeScanEventArgs { OssScanRunning = this.IsOssScanning });
        }


        public void FireIacScanningFinishedEvent()
        {
            this.IsIacScanning = false;
            this.IacScanningFinished?.Invoke(this,
                new SnykCodeScanEventArgs { OssScanRunning = this.IsOssScanning });
        }

        /// <summary>
        /// Fire scanning cancelled event.
        /// </summary>
        public void FireScanningCancelledEvent() => this.ScanningCancelled?.Invoke(this, new SnykOssScanEventArgs());

        public async Task<FeaturesSettings> GetFeaturesSettingsAsync()
        {
            var options = this.serviceProvider.Options;

            SastSettings sastSettings = null;
            if (LanguageClientHelper.IsLanguageServerReady())
            {
                sastSettings = await this.serviceProvider.Package.LanguageClientManager.InvokeGetSastEnabled(CancellationToken.None);
            }

            bool snykCodeEnabled = sastSettings?.SnykCodeEnabled ?? false;

            return new FeaturesSettings
            {
                OssEnabled = options.OssEnabled,
                SastOnServerEnabled = snykCodeEnabled,
                CodeSecurityEnabled = snykCodeEnabled && options.SnykCodeSecurityEnabled,
                CodeQualityEnabled = snykCodeEnabled && options.SnykCodeQualityEnabled,
                LocalCodeEngineEnabled = sastSettings?.LocalCodeEngineEnabled ?? false,
                IacEnabled = options.IacEnabled
            };
        }

        public void CancelDownloadTask()
        {
            this.CancelTask(downloadCliTokenSource);
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
                Logger.Information(e, "Try to cancel task");
            }
            finally
            {
                tokenSource = null;
            }
        }

        public bool ShouldDownloadCli()
        {
            var userSettingsStorageService = this.serviceProvider.UserStorageSettingsService;
            if (!userSettingsStorageService.BinariesAutoUpdate)
            {
                return false;
            }
            try
            {
                var currentCliVersion = userSettingsStorageService.GetCurrentCliVersion();

                var options = this.serviceProvider.Options;
                var cliDownloader = new SnykCliDownloader(options, currentCliVersion);

                var downloadPath = options.CliCustomPath;
                var fileDestinationPath = GetCliFilePath(downloadPath);

                return cliDownloader.IsCliDownloadNeeded(fileDestinationPath);

            }
            catch (Exception)
            {
                return true;
            }
        }


        private async Task DownloadAsync(CliDownloadFinishedCallback downloadFinishedCallback,
            ISnykProgressWorker progressWorker)
        {
            var userSettingsStorageService = this.serviceProvider.UserStorageSettingsService;
            if (!userSettingsStorageService.BinariesAutoUpdate)
            {
                Logger.Information("CLI auto-update is disabled, CLI download is skipped.");
                this.DownloadCancelled?.Invoke(this, new SnykCliDownloadEventArgs());
                return;
            }

            this.isCliDownloading = true;
            try
            {
                var currentCliVersion = userSettingsStorageService.GetCurrentCliVersion();

                var lastCliReleaseDate = userSettingsStorageService.GetCliReleaseLastCheckDate();

                var cliDownloader = new SnykCliDownloader(this.serviceProvider.Options, currentCliVersion);

                var downloadFinishedCallbacks = new List<CliDownloadFinishedCallback>();

                if (downloadFinishedCallback != null)
                {
                    downloadFinishedCallbacks.Add(downloadFinishedCallback);
                }

                downloadFinishedCallbacks.Add(() =>
                {
                    userSettingsStorageService.SaveCurrentCliVersion(cliDownloader.GetLatestReleaseInfo().Name);
                    userSettingsStorageService.SaveCliReleaseLastCheckDate(DateTime.UtcNow);
                    DisposeCancellationTokenSource(this.downloadCliTokenSource);
                });

                var downloadPath = this.serviceProvider.Options.CliCustomPath;
                await cliDownloader.AutoUpdateCliAsync(
                    progressWorker,
                    downloadPath,
                    downloadFinishedCallbacks: downloadFinishedCallbacks);
            }
            finally
            {
                this.isCliDownloading = false;
            }
        }
    }
}