﻿using Snyk.VisualStudio.Extension.CLI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.UI
{
    public class SnykCliScanEventArgs : EventArgs
    {
        public CliError Error { get; set; }
    }    

    public class SnykTaskExecuteService
    {
        private static SnykTaskExecuteService instance;

        private CancellationTokenSource tokenSource;
        private SnykVSPackage package;

        private SnykTaskExecuteService() { }

        public static SnykTaskExecuteService Instance()
        {
            if (instance == null)
            {
                instance = new SnykTaskExecuteService();
            }
                
            return instance;
        }

        public static void Initialize(SnykVSPackage vsPackage)
        {
            instance = new SnykTaskExecuteService();

            instance.package = vsPackage;
        }

        public event EventHandler<SnykCliScanEventArgs> ScanningStarted;

        public event EventHandler<SnykCliScanEventArgs> ScanningFinished;

        public event EventHandler<SnykCliScanEventArgs> ScanError;

        public event EventHandler<SnykCliScanEventArgs> ScanningCancelled;






        protected virtual void OnScanningCancelled(string message)
        {
            SnykCliScanEventArgs eventArgs = new SnykCliScanEventArgs
            {
                Error = new CliError
                {
                    Message = message
                }
            };

            ScanError?.Invoke(this, eventArgs);
        }






        public void CancelCurrentTask()
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();

                tokenSource = null;
            }            
        }        

        public void Scan()
        {
            tokenSource = new CancellationTokenSource();

            var tokenChecker = new CancellationTokenChecker(tokenSource.Token);

            Task.Run(() =>
            {
                var toolWindow = package.GetToolWindow();

                try
                {
                    tokenChecker.CancelIfCancellationRequested();

                    EnvDTE.Projects projects = package.SolutionService.GetProjects();

                    if (projects.Count == 0)
                    {
                        OnScanningCancelled("No open solution.");

                        return;
                    }

                    tokenChecker.CancelIfCancellationRequested();

                    toolWindow.HideError();
                    toolWindow.ShowIndeterminateProgressBar("Scanning...");

                    toolWindow.ClearDataGrid();

                    tokenChecker.CancelIfCancellationRequested();

                    var cli = new SnykCli
                    {
                        Options = package.Options
                    };

                    tokenChecker.CancelIfCancellationRequested();

                    for (int index = 1; index <= projects.Count; index++)
                    {
                        tokenChecker.CancelIfCancellationRequested();

                        try
                        {
                            EnvDTE.Project project = projects.Item(index);

                            string projectPath = project.Properties.Item("LocalPath").Value.ToString();

                            CliResult cliResult = cli.Scan(projectPath);

                            if (!cliResult.IsSuccessful())
                            {
                                toolWindow.DisplayError(cliResult.Error);
                            }
                            else
                            {
                                toolWindow.DisplayDataGrid();

                                toolWindow.AddCliResultToDataGrid(cliResult);
                            }
                        } catch(Exception scanException)
                        {                           
                            OnScanningCancelled(scanException.Message);
                        }                       
                    }

                    toolWindow.HideProgressBar();

                    tokenSource = null;
                }
                catch (Exception exception)
                {
                    toolWindow.HideAllControls();

                    tokenSource = null;
                }
            }, tokenChecker.Token);            
        }
        
        public void Download()
        {
            tokenSource = new CancellationTokenSource();

            var tokenChecker = new CancellationTokenChecker(tokenSource.Token);

            Task.Run(() =>
            {
                var toolWindow = package.GetToolWindow();

                try
                {
                    SnykCliDownloader.NewInstance().Download(progressManager: toolWindow, tokenChecker: tokenChecker);
                }
                catch (Exception exception)
                {
                    toolWindow.HideAllControls();

                    tokenSource = null;
                }
            }, tokenChecker.Token);
        }
    }

    public class CancellationTokenChecker
    {
        private CancellationToken token;

        public CancellationTokenChecker(CancellationToken sourceToken)
        {
            this.token = sourceToken;
        }

        public CancellationToken Token
        {
            get
            {
                return token;
            }
        }

        public void CancelIfCancellationRequested()
        {
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }
        }
    }
}
