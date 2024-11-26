using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service.Domain;

namespace Snyk.VisualStudio.Extension.Service;

public interface ISnykTasksService
{
    CancellationTokenSource SnykScanTokenSource { get; set; }
    bool IsOssScanning { get; set; }
    bool IsSnykCodeScanning { get; set; }
    bool IsIacScanning { get; set; }

    /// <summary>
    /// Cli scanning started event handler.
    /// </summary>
    event EventHandler<SnykOssScanEventArgs> OssScanningStarted;

    /// <summary>
    /// Cli Scanning update event handler.
    /// </summary>
    event EventHandler<SnykOssScanEventArgs> OssScanningUpdate;

    /// <summary>
    /// OSS Scanning Disabled event handler.
    /// </summary>
    event EventHandler<SnykOssScanEventArgs> OssScanningDisabled;

    /// <summary>
    /// Scanning OSS finished event handler.
    /// </summary>
    event EventHandler<SnykOssScanEventArgs> OssScanningFinished;

    /// <summary>
    /// Cli scan error event handler.
    /// </summary>
    event EventHandler<SnykOssScanEventArgs> OssScanError;

    /// <summary>
    /// IaC scanning started event handler.
    /// </summary>
    event EventHandler<SnykCodeScanEventArgs> IacScanningStarted;

    /// <summary>
    /// Iac scanning update event handler.
    /// </summary>
    event EventHandler<SnykCodeScanEventArgs> IacScanningUpdate;

    /// <summary>
    /// IaC Scanning Disabled event handler.
    /// </summary>
    event EventHandler<SnykCodeScanEventArgs> IacScanningDisabled;

    /// <summary>
    /// Scanning IaC finished event handler.
    /// </summary>
    event EventHandler<SnykCodeScanEventArgs> IacScanningFinished;

    /// <summary>
    /// Iac scan error event handler.
    /// </summary>
    event EventHandler<SnykCodeScanEventArgs> IacScanError;

    /// <summary>
    /// SnykCode scanning started event handler.
    /// </summary>
    event EventHandler<SnykCodeScanEventArgs> SnykCodeScanningStarted;

    /// <summary>
    /// Scanning SnykCode finished event handler.
    /// </summary>
    event EventHandler<SnykCodeScanEventArgs> SnykCodeScanningFinished;

    /// <summary>
    /// SnykCode scanning update event handler.
    /// </summary>
    event EventHandler<SnykCodeScanEventArgs> SnykCodeScanningUpdate;

    /// <summary>
    /// SnykCode scan error event handler.
    /// </summary>
    event EventHandler<SnykCodeScanEventArgs> SnykCodeScanError;

    /// <summary>
    /// SnykCode disabled event handler.
    /// </summary>
    event EventHandler<SnykCodeScanEventArgs> SnykCodeDisabled;

    /// <summary>
    /// Scanning cancelled event handler.
    /// </summary>
    event EventHandler<SnykOssScanEventArgs> ScanningCancelled;

    /// <summary>
    /// Download started event handler.
    /// </summary>
    event EventHandler<SnykCliDownloadEventArgs> DownloadStarted;

    /// <summary>
    /// Download finished event handler.
    /// </summary>
    event EventHandler<SnykCliDownloadEventArgs> DownloadFinished;

    /// <summary>
    /// Download update event handler.
    /// </summary>
    event EventHandler<SnykCliDownloadEventArgs> DownloadUpdate;

    /// <summary>
    /// Download cancelled event handler. Raised when the user cancels the download intentionally.
    /// </summary>
    event EventHandler<SnykCliDownloadEventArgs> DownloadCancelled;

    /// <summary>
    /// Download failed event handler. Raised when the download fails due to an error.
    /// </summary>
    event EventHandler<Exception> DownloadFailed;

    /// <summary>
    /// Task finished event.
    /// </summary>
    event EventHandler<EventArgs> TaskFinished;

    /// <summary>
    /// Check is Scan running (oss or snykcode) or CLI download.
    /// </summary>
    /// <returns>True if Oss or SnykCode scan running.</returns>
    bool IsTaskRunning();

    /// <summary>
    /// Cancel current task.
    /// </summary>
    void CancelTasks();

    /// <summary>
    /// Start scan in background task.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ScanAsync();

    /// <summary>
    /// Checks if opened solution folder is trusted. If not, prompts a user with trust permission.
    /// </summary>
    /// <returns>Folder is trusted or not.</returns>
    Task<bool> IsFolderTrustedAsync();

    /// <summary>
    /// Start a CLI download task in background thread. Will only download the CLI if it's missing or outdated.
    /// </summary>
    /// <param name="downloadFinishedCallback"><see cref="CliDownloadFinishedCallback"/> callback object.</param>
    void Download(SnykCliDownloader.CliDownloadFinishedCallback downloadFinishedCallback = null);

    Task DownloadAsync(SnykCliDownloader.CliDownloadFinishedCallback downloadFinishedCallback = null);

    /// <summary>
    /// Fire on task finished (oss scan or snykcode scan or cli download).
    /// </summary>
    /// <param name="message">Error message.</param>
    void FireTaskFinished();

    /// <summary>
    /// Fire error event. Create <see cref="OssError"/> instance.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="featuresSettings">Features settings.</param>
    void FireOssError(string message, FeaturesSettings featuresSettings = null);

    /// <summary>
    /// Fire error event with <see cref="SnykOssScanEventArgs"/>.
    /// </summary>
    /// <param name="error"><see cref="OssError"/> object.</param>
    /// <param name="featuresSettings">Features settings.</param>
    void FireOssError(OssError error, FeaturesSettings featuresSettings = null);

    /// <summary>
    /// Fire error event with <see cref="SnykCodeScanEventArgs"/>.
    /// </summary>
    /// <param name="message">Error message</param>
    void OnSnykCodeError(string message);

    void OnIacError(string message);

    /// <summary>
    /// Fire SnykCode disabled event with <see cref="SnykCodeScanEventArgs"/>.
    /// </summary>
    /// <param name="localCodeEngineEnabled">Send local code engine enabled/disabled on server in event.</param>
    void FireSnykCodeDisabledError(bool localCodeEngineEnabled);

    void FireSnykIacDisabledError(bool localCodeEngineEnabled);

    /// <summary>
    /// Fire Cli scanning started event.
    /// </summary>
    void FireOssScanningStartedEvent();

    /// <summary>
    /// Fire SnykCode scanning started event.
    /// </summary>
    void FireSnykCodeScanningStartedEvent(FeaturesSettings featuresSettings);

    void FireIacScanningStartedEvent(FeaturesSettings featuresSettings);

    /// <summary>
    /// Fire scanning update with <see cref="SnykOssScanEventArgs"/> object.
    /// </summary>
    void FireOssScanningUpdateEvent(IDictionary<string, IEnumerable<Issue>> scanResult);

    /// <summary>
    /// Fire scanning update with <see cref="SnykCodeScanEventArgs"/> object.
    /// </summary>
    /// <param name="analysisResult"><see cref="AnalysisResult"/> object with vulnerabilities.</param>
    void FireCodeScanningUpdateEvent(IDictionary<string, IEnumerable<Issue>> analysisResult);

    void FireIacScanningUpdateEvent(IDictionary<string, IEnumerable<Issue>> analysisResult);

    /// <summary>
    /// Fire OSS scanning finished event.
    /// </summary>
    void FireOssScanningFinishedEvent();

    /// <summary>
    /// Fire SnykCode scanning finished event.
    /// </summary>
    void FireSnykCodeScanningFinishedEvent();

    void FireIacScanningFinishedEvent();

    /// <summary>
    /// Fire scanning cancelled event.
    /// </summary>
    void FireScanningCancelledEvent();

    Task<FeaturesSettings> GetFeaturesSettingsAsync();
    void CancelDownloadTask();
    bool ShouldDownloadCli();
}