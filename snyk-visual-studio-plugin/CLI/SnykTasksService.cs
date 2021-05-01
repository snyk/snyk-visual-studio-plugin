﻿using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Service;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static Snyk.VisualStudio.Extension.CLI.SnykCliDownloader;
using Task = System.Threading.Tasks.Task;

namespace Snyk.VisualStudio.Extension.CLI
{   
    public class SnykTasksService
    {
        private static SnykTasksService instance;        

        public event EventHandler<SnykCliScanEventArgs> ScanningStarted;

        public event EventHandler<SnykCliScanEventArgs> ScanningFinished;

        public event EventHandler<SnykCliScanEventArgs> ScanningUpdate;

        public event EventHandler<SnykCliScanEventArgs> ScanError;

        public event EventHandler<SnykCliScanEventArgs> ScanningCancelled;

        public event EventHandler<SnykCliDownloadEventArgs> DownloadStarted;

        public event EventHandler<SnykCliDownloadEventArgs> DownloadFinished;

        public event EventHandler<SnykCliDownloadEventArgs> DownloadUpdate;

        public event EventHandler<SnykCliDownloadEventArgs> DownloadCancelled;

        private CancellationTokenSource tokenSource;

        private Task currentTask;

        private ISnykServiceProvider serviceProvider;

        private SnykCli cli;

        private SnykTasksService() { }

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

        public static async Task InitializeAsync(ISnykServiceProvider serviceProvider)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            instance = new SnykTasksService();

            instance.serviceProvider = serviceProvider;

            instance.Logger.LogInformation("SnykTasksService initialized");
        }        

        public void CancelCurrentTask()
        {
            if (tokenSource != null)
            {
                Logger.LogInformation("Cancel current task");

                tokenSource.Cancel();

                cli?.ConsoleRunner?.Stop();
            }         
        }

        public void OnUiLoaded(object sender, RoutedEventArgs eventArgs) => Download();

        public void Scan()
        {
            Logger.LogInformation("Enter Scan method");

            if (currentTask != null && currentTask.Status == TaskStatus.Running)
            {
                Logger.LogInformation("There is already a task in progress");

                return;
            }

            tokenSource = new CancellationTokenSource();

            var progressWorker = new SnykProgressWorker
            {
                TasksService = this,
                TokenSource = tokenSource
            };

            Logger.LogInformation("Start scan task");

            currentTask = Task.Run(() =>
            {
                OnScanningStarted();

                try
                {
                    progressWorker.CancelIfCancellationRequested();

                    if (!serviceProvider.SolutionService.IsSolutionOpen)
                    {
                        OnError("No open solution.");

                        Logger.LogInformation("Solution not opened");

                        return;
                    }

                    progressWorker.CancelIfCancellationRequested();

                    var options = serviceProvider.Options;

                    cli = new SnykCli
                    {
                        Options = options,
                        Logger = Logger
                    };

                    Logger.LogInformation($"Snyk Extension options");
                    Logger.LogInformation($"API token = {options.ApiToken}");
                    Logger.LogInformation($"Custom Endpoint = {options.CustomEndpoint}");
                    Logger.LogInformation($"Organization = {options.Organization}");
                    Logger.LogInformation($"Ignore Unknown CA = {options.IgnoreUnknownCA}");
                    Logger.LogInformation($"Additional Options = {options.AdditionalOptions}");
                    Logger.LogInformation($"Is Scan All Projects = {options.IsScanAllProjects}");

                    progressWorker.CancelIfCancellationRequested();

                    try
                    {
                        string solutionPath = serviceProvider.SolutionService.GetSolutionPath();

                        Logger.LogInformation($"Solution path = {solutionPath}");
                        Logger.LogInformation("Start scan");

                        CliResult cliResult = cli.Scan(solutionPath);

                        progressWorker.CancelIfCancellationRequested();

                        if (!cliResult.IsSuccessful())
                        {
                            Logger.LogInformation("Scan is successful");

                            OnError(cliResult.Error);

                            return;
                        }
                        else
                        {
                            Logger.LogInformation("Scan update");

                            OnScanningUpdate(cliResult);
                        }                        

                        progressWorker.CancelIfCancellationRequested();

                        Logger.LogInformation("Scan finished");
                    }
                    catch (Exception scanException)
                    {
                        Logger.LogError(scanException.Message);

                        if ((bool)(cli?.ConsoleRunner?.IsStopped))
                        {
                            OnScanningCancelled();
                        } 
                        else
                        {
                            OnError(scanException.Message);
                        }
                        
                        cli = null;

                        return;
                    }

                    progressWorker.CancelIfCancellationRequested();

                    OnScanningFinished();

                    cli = null;
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception.Message);

                    OnScanningCancelled();

                    cli = null;
                }
            }, progressWorker.TokenSource.Token);   


            serviceProvider.AnalyticsService.LogUserTriggersAnAnalysisEvent();
        }
        
        public void Download(CliDownloadFinishedCallback downloadFinishedCallback = null)
        {
            Logger.LogInformation("Enter Download method");

            if (currentTask != null && currentTask.Status == TaskStatus.Running)
            {
                Logger.LogInformation("There is already a task in progress");

                return;
            }

            tokenSource = new CancellationTokenSource();

            var progressWorker = new SnykProgressWorker
            {
                TasksService = this,
                TokenSource = tokenSource
            };

            Logger.LogInformation("Start run task");

            currentTask = Task.Run(() =>
            {                
                try
                {
                    var cliDownloader = new SnykCliDownloader(serviceProvider.ActivityLogger);

                    cliDownloader.Download(progressWorker: progressWorker, downloadFinishedCallback: downloadFinishedCallback);
                }
                catch (Exception exception)
                {
                    Logger.LogInformation(exception.Message);

                    OnDownloadCancelled(exception.Message);                    
                }
            }, progressWorker.TokenSource.Token);
        }

        public void OnError(string message) => OnError(new CliError(message));

        public void OnError(CliError error) => ScanError?.Invoke(this, new SnykCliScanEventArgs(error));

        protected internal void OnDownloadStarted() => DownloadStarted?.Invoke(this, new SnykCliDownloadEventArgs());

        protected internal void OnDownloadFinished() => DownloadFinished?.Invoke(this, new SnykCliDownloadEventArgs());

        protected internal void OnDownloadCancelled(string message) => DownloadCancelled?.Invoke(this, new SnykCliDownloadEventArgs(message));

        protected internal void OnDownloadUpdate(int progress) => DownloadUpdate?.Invoke(this, new SnykCliDownloadEventArgs(progress));

        private void OnScanningStarted() => ScanningStarted?.Invoke(this, new SnykCliScanEventArgs());

        private void OnScanningUpdate(CliResult cliResult) => ScanningUpdate?.Invoke(this, new SnykCliScanEventArgs(cliResult));

        private void OnScanningFinished() => ScanningFinished?.Invoke(this, new SnykCliScanEventArgs());

        private void OnScanningCancelled() => ScanningCancelled?.Invoke(this, new SnykCliScanEventArgs());        

        private SnykActivityLogger Logger
        {
            get
            {
                return serviceProvider.ActivityLogger;
            }
        }        
    }

    public class SnykCliScanEventArgs : EventArgs
    {
        public SnykCliScanEventArgs() { }

        public SnykCliScanEventArgs(CliError cliError)
        {
            this.Error = cliError;
        }

        public SnykCliScanEventArgs(CliResult cliResult)
        {
            this.Result = cliResult;
        }

        public CliError Error { get; set; }
        public CliResult Result { get; set; }
    }

    public class SnykCliDownloadEventArgs : EventArgs
    {
        public SnykCliDownloadEventArgs() { }

        public SnykCliDownloadEventArgs(int progress)
        {
            this.Progress = progress;
        }

        public SnykCliDownloadEventArgs(string message)
        {
            this.Message = message;
        }

        public int Progress { get; set; }

        public string Message { get; set; }
    }

    public interface ISnykProgressWorker
    {
        void DownloadStarted();

        void UpdateProgress(int progress);

        void DownloadFinished();

        void CancelIfCancellationRequested();

        void DownloadCancelled(string message);
    }

   class SnykProgressWorker : ISnykProgressWorker
    {
        public CancellationTokenSource TokenSource { get; set; }        

        public SnykTasksService TasksService { get; set; }

        public void UpdateProgress(int progress) => TasksService.OnDownloadUpdate(progress);

        public void DownloadFinished() => TasksService.OnDownloadFinished();

        public void CancelIfCancellationRequested()
        {
            if (TokenSource.Token.IsCancellationRequested)
            {
                TokenSource.Token.ThrowIfCancellationRequested();
            }
        }

        public void DownloadStarted() => TasksService.OnDownloadStarted();

        public void DownloadCancelled(string message) => TasksService.OnDownloadCancelled(message);
    }
}
