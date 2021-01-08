using Snyk.VisualStudio.Extension.CLI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.UI
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

        private SnykVSPackage package;

        private SnykTasksService() { }

        public static SnykTasksService Instance()
        {
            if (instance == null)
            {
                instance = new SnykTasksService();
            }
                
            return instance;
        }

        public static void Initialize(SnykVSPackage vsPackage)
        {
            instance = new SnykTasksService();

            instance.package = vsPackage;
        }        

        public void CancelCurrentTask()
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }            
        }

        public void Scan()
        {            
            if (currentTask != null && currentTask.Status == TaskStatus.Running)
            {
                return;
            }

            tokenSource = new CancellationTokenSource();

            var progressWorker = new ProgressWorker
            {
                TasksService = this,
                TokenSource = tokenSource
            };

            currentTask = Task.Run(() =>
            {
                OnScanningStarted();

                try
                {
                    progressWorker.CancelIfCancellationRequested();

                    if (!package.SolutionService.IsSolutionOpen())
                    {
                        OnScanError("No open solution.");

                        return;
                    }

                    progressWorker.CancelIfCancellationRequested();                    

                    var cli = new SnykCli
                    {
                        Options = package.Options
                    };

                    progressWorker.CancelIfCancellationRequested();

                    try
                    {
                        string solutionPath = package.SolutionService.GetSolutionPath();

                        CliResult cliResult = cli.Scan(solutionPath);

                        progressWorker.CancelIfCancellationRequested();

                        if (!cliResult.IsSuccessful())
                        {
                            OnScanError(cliResult.Error);
                        }
                        else
                        {
                            OnScanningUpdate(cliResult);
                        }

                        progressWorker.CancelIfCancellationRequested();
                    }
                    catch (OperationCanceledException e)
                    {
                        OnScanningCancelled();
                    }
                    catch (Exception scanException)
                    {
                        OnScanError(scanException.Message);
                    }

                    OnScanningFinished();
                }
                catch (OperationCanceledException e)
                {
                    OnScanningCancelled();
                }
                catch (Exception exception)
                {
                    OnScanningCancelled();                    
                }
            }, progressWorker.TokenSource.Token);   
        }
        
        public void Download()
        {
            if (currentTask != null && currentTask.Status == TaskStatus.Running)
            {
                return;
            }

            tokenSource = new CancellationTokenSource();

            var progressWorker = new ProgressWorker
            {
                TasksService = this,
                TokenSource = tokenSource
            };

            currentTask = Task.Run(() =>
            {                
                try
                {                   
                    SnykCliDownloader.NewInstance().Download(progressWorker: progressWorker);
                }
                catch (Exception exception)
                {
                    OnDownloadCancelled(exception.Message);
                }
            }, progressWorker.TokenSource.Token);
        }

        protected internal void OnDownloadStarted() => DownloadStarted?.Invoke(this, new SnykCliDownloadEventArgs());

        protected internal void OnDownloadFinished() => DownloadFinished?.Invoke(this, new SnykCliDownloadEventArgs());

        protected internal void OnDownloadCancelled(string message) => DownloadCancelled?.Invoke(this, new SnykCliDownloadEventArgs(message));

        protected internal void OnDownloadUpdate(int progress) => DownloadUpdate?.Invoke(this, new SnykCliDownloadEventArgs(progress));

        private void OnScanningStarted() => ScanningStarted?.Invoke(this, new SnykCliScanEventArgs());

        private void OnScanningUpdate(CliResult cliResult) => ScanningUpdate?.Invoke(this, new SnykCliScanEventArgs(cliResult));

        private void OnScanningFinished() => ScanningFinished?.Invoke(this, new SnykCliScanEventArgs());

        private void OnScanningCancelled() => ScanningCancelled?.Invoke(this, new SnykCliScanEventArgs());

        private void OnScanError(string message) => OnScanError(new CliError(message));

        private void OnScanError(CliError error) => ScanError?.Invoke(this, new SnykCliScanEventArgs(error));
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

    public interface IProgressWorker
    {
        void DownloadStarted();

        void UpdateProgress(int progress);

        void DownloadFinished();

        void CancelIfCancellationRequested();

        void DownloadCancelled(string message);
    }

   class ProgressWorker : IProgressWorker
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
