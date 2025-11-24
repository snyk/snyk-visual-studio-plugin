using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Commands;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.Theme;
using Snyk.VisualStudio.Extension.UI.Notifications;
using Snyk.VisualStudio.Extension.UI.Tree;
using Task = System.Threading.Tasks.Task;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Interaction logic for SnykToolWindowControl.
    /// </summary>
    public partial class SnykToolWindowControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykToolWindowControl>();

        private SnykToolWindow toolWindow;

        private ISnykServiceProvider serviceProvider;

        private ToolWindowContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykToolWindowControl"/> class.
        /// </summary>
        /// /// <param name="toolWindow">Tool window instance..</param>
        public SnykToolWindowControl(SnykToolWindow toolWindow)
        {
            this.toolWindow = toolWindow;

            this.InitializeComponent();

            this.context = new ToolWindowContext(this);

            this.DescriptionPanel.Init();

            this.SummaryPanel.Init();
            this.messagePanel.Context = this.context;
        }

        /// <summary>
        /// Gets a value indicating whether VulnerabilitiesTree.
        /// </summary>
        public SnykFilterableTree VulnerabilitiesTree => this.resultsTree;

        /// <summary>
        /// Gets a value indicating whether tree content not empty.
        /// </summary>
        /// <returns>True if result tree not empty.</returns>
        public bool IsTreeContentNotEmpty() => this.resultsTree.OssRootNode.HasContent
            || this.resultsTree.CodeSecurityRootNode.HasContent;

        /// <summary>
        /// Initialize event listeners for UI.
        /// </summary>
        /// <param name="serviceProvider">Service provider implementation.</param>
        public void InitializeEventListeners(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.messagePanel.ServiceProvider = serviceProvider;

            Logger.Information("Enter InitializeEventListenersAsync() method.");

            Logger.Information("Initialize Solultion Event Listeners");

            var solutionService = serviceProvider.SolutionService as SnykSolutionService;

            solutionService.SolutionEvents.AfterCloseSolution += this.OnAfterCloseSolution;

            Logger.Information("Initialize CLI Event Listeners");

            var tasksService = serviceProvider.TasksService;

            tasksService.SnykCodeScanningStarted += this.OnSnykCodeScanningStarted;
            tasksService.SnykCodeScanError += this.OnSnykCodeDisplayError;
            // SnykCodeDisabled event handler removed - SAST checks are no longer performed
            tasksService.SnykCodeScanningUpdate += this.OnSnykCodeScanningUpdate;
            tasksService.SnykCodeScanningFinished += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(this.OnSnykCodeScanningFinishedAsync);
            
            tasksService.OssScanningStarted += this.OnOssScanningStarted;
            tasksService.OssScanError += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(() => this.OnOssDisplayErrorAsync(sender, args));
            tasksService.OssScanningDisabled += this.OnOssScanningDisabled;
            tasksService.OssScanningUpdate += this.OnOssScanningUpdate;
            tasksService.OssScanningFinished += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(this.OnOssScanningFinishedAsync);

            tasksService.IacScanningStarted += OnIacScanningStarted;
            tasksService.IacScanError += OnIacScanError;
            tasksService.IacScanningDisabled += OnIacScanningDisabled;
            tasksService.IacScanningUpdate += OnIacScanningUpdate;
            tasksService.IacScanningFinished += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(this.OnIacScanningFinishedAsync);


            tasksService.ScanningCancelled += this.OnScanningCancelled;
            tasksService.TaskFinished += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(this.OnTaskFinishedAsync);

            Logger.Information("Initialize Download Event Listeners");

            tasksService.DownloadStarted += (sender, args) =>
            {
                if (LanguageClientHelper.IsLanguageServerReady())
                    ThreadHelper.JoinableTaskFactory.RunAsync(serviceProvider.LanguageClientManager.StopServerAsync).FireAndForget();
                this.OnDownloadStarted(sender, args);
            };
            tasksService.DownloadFinished += (sender, args) =>
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async ()=> await serviceProvider.LanguageClientManager.StartServerAsync(true)).FireAndForget();
                this.OnDownloadFinished(sender, args);
            };
            tasksService.DownloadUpdate += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(() => this.OnDownloadUpdateAsync(sender, args));
            tasksService.DownloadCancelled += this.OnDownloadCancelled;
            tasksService.DownloadFailed += this.OnDownloadFailed;
            LanguageClientHelper.LanguageClientManager().OnLanguageServerReadyAsync += OnOnLanguageServerReadyAsync;
            this.Loaded += (sender, args) => tasksService.Download();

            serviceProvider.Options.SettingsChanged += this.OnSettingsChanged;

            SnykScanCommand.Instance.UpdateControlsStateCallback = (isEnabled) => ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.messagePanel.runScanButton.IsEnabled = isEnabled;

                if (!LanguageClientHelper.IsLanguageServerReady())
                {
                    this.context.TransitionTo(InitializingState.Instance);
                    return;
                }
                if (!isEnabled)
                {
                    this.DetermineInitScreen();
                }
            });

            Logger.Information("Leave InitializeEventListenersAsync() method.");
        }

        private async Task OnOnLanguageServerReadyAsync(object sender, SnykLanguageServerEventArgs args)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            this.DetermineInitScreen();
        }

        private void OnIacScanError(object sender, SnykCodeScanEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.resultsTree.IacRootNode.State = RootTreeNodeState.Error;
                this.resultsTree.IacRootNode.PresentableError = e.PresentableError;
                this.resultsTree.IacRootNode.Clean();

                if (e.PresentableError?.ShowNotification ?? true)
                {
                    NotificationService.Instance.ShowErrorInfoBar(e.PresentableError?.ErrorMessage);
                }

                if (!this.serviceProvider.Options.OssEnabled)
                {
                    this.context.TransitionTo(RunScanState.Instance);
                }

                await this.UpdateActionsStateAsync();
            });
        }

        private void OnIacScanningStarted(object sender, SnykCodeScanEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.messagePanel.ShowScanningMessage();
                this.mainGrid.Visibility = Visibility.Visible;

                this.resultsTree.IacRootNode.State = RootTreeNodeState.Scanning;
                resultsTree.IacRootNode.Clean();
                await this.UpdateActionsStateAsync();
            });
        }

        public void SelectedItemInTree(string issueId, string product)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var rootNodes = new List<TreeNode>();

                switch (product)
                {
                    case Product.Code:
                        rootNodes.Add(this.resultsTree.CodeSecurityRootNode);
                        break;
                    case Product.Oss:
                        rootNodes.Add(this.resultsTree.OssRootNode);
                        break;
                    case Product.Iac:
                        rootNodes.Add(this.resultsTree.IacRootNode);
                        break;
                }

                foreach (var rootNode in rootNodes)
                {
                    var foundAndSelected = TrySelectIssueInRootNode(rootNode, issueId);
                    if (foundAndSelected)
                    {
                        break;
                    }
                }
            });
        }

        /// <summary>
        /// Searches the given root node (e.g. CodeSecurityRootNode) for a child CodeTreeNode
        /// whose Issue.Id matches <paramref name="issueId"/>. If found, selects it.
        /// </summary>
        private bool TrySelectIssueInRootNode(TreeNode rootNode, string issueId)
        {
            foreach (var treeItem in rootNode.Items)
            {
                if (treeItem is FileTreeNode fileTreeNode)
                {
                    foreach (var childNode in fileTreeNode.Items)
                    {
                        if (childNode is IssueTreeNode issueTreeNode && issueTreeNode.Issue.Id == issueId)
                        {
                            issueTreeNode.IsSelected = true;

                            this.resultsTree.CurrentTreeNode = issueTreeNode;

                            var tvItem = FindContainerForItem(resultsTree.vulnerabilitiesTree, issueTreeNode);
                            if (tvItem == null)
                            {
                                continue;
                            }
                            tvItem.BringIntoView();
                            if (tvItem.IsSelected && resultsTree.vulnerabilitiesTree.SelectedItem == issueTreeNode)
                            {
                                RaiseSelectedItemChanged(resultsTree.vulnerabilitiesTree, resultsTree.vulnerabilitiesTree.SelectedItem, issueTreeNode);
                                return true;
                            }

                            tvItem.IsSelected = true;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static TreeViewItem FindContainerForItem(
            ItemsControl parent,
            object targetItem,
            bool autoExpandParents = false)
        {
            if (parent.ItemContainerGenerator.ContainerFromItem(targetItem) is TreeViewItem container) return container;

            foreach (var childItem in parent.Items)
            {
                if (parent.ItemContainerGenerator.ContainerFromItem(childItem) is not TreeViewItem parentContainer)
                    continue;
                if (autoExpandParents)
                {
                    // Expand to force generation of deeper levels
                    parentContainer.IsExpanded = true;
                    parent.UpdateLayout();
                }

                // Recurse
                var descendantContainer = FindContainerForItem(
                    parentContainer,
                    targetItem,
                    autoExpandParents);

                if (descendantContainer != null)
                    return descendantContainer;
            }
            return null;
        }
        private void RaiseSelectedItemChanged(TreeView tree, object oldItem, object newItem)
        {
            // Build the args needed for the SelectedItemChanged event
            var args = new RoutedPropertyChangedEventArgs<object>(
                oldItem,
                newItem
            )
            {
                RoutedEvent = TreeView.SelectedItemChangedEvent,
                Source = tree
            };

            // Manually raise the event on the TreeView
            tree.RaiseEvent(args);
        }
        private void OnIacScanningDisabled(object sender, SnykCodeScanEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();


                this.resultsTree.IacRootNode.State = RootTreeNodeState.Disabled;
                this.resultsTree.IacRootNode.Clean();
            });
        }

        private void OnOssScanningDisabled(object sender, SnykOssScanEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();


                this.resultsTree.OssRootNode.State = RootTreeNodeState.Disabled;
                this.resultsTree.OssRootNode.Clean();
            });
        }


        /// <summary>
        /// AfterBackgroundSolutionLoadComplete event handler. Switch context to RunScanState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnAfterBackgroundSolutionLoadComplete(object sender, EventArgs eventArgs)
        {
        }

        /// <summary>
        /// AfterCloseSolution event handler. Switch context to RunScanState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnAfterCloseSolution(object sender, EventArgs eventArgs) => this.Clean();

        /// <summary>
        /// Scanning update event handler. Append Code results to tree.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnOssScanningUpdate(object sender, SnykOssScanEventArgs eventArgs) => this.AppendOssResultToTree(eventArgs.Result);

        /// <summary>
        /// Scanning update event handler. Append Code results to tree.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnSnykCodeScanningUpdate(object sender, SnykCodeScanEventArgs eventArgs) => this.AppendSnykCodeResultToTree(eventArgs.Result);

        /// <summary>
        /// Scanning update event handler. Append IaC results to tree.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnIacScanningUpdate(object sender, SnykCodeScanEventArgs eventArgs) => this.AppendSnykIacResultToTree(eventArgs.Result);


        /// <summary>
        /// Cli ScanningStarted event handler..
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnOssScanningStarted(object sender, SnykOssScanEventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.messagePanel.ShowScanningMessage();
            this.mainGrid.Visibility = Visibility.Visible;

            this.resultsTree.OssRootNode.State = RootTreeNodeState.Scanning;
            resultsTree.OssRootNode.Clean();
            await this.UpdateActionsStateAsync();
        });

        /// <summary>
        /// SnykCode ScanningStarted event handler. Switch context to ScanningState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnSnykCodeScanningStarted(object sender, SnykCodeScanEventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.messagePanel.ShowScanningMessage();

            SetScanNodeState(resultsTree.CodeSecurityRootNode, eventArgs.CodeScanEnabled);
            resultsTree.CodeSecurityRootNode.Clean();
            await this.UpdateActionsStateAsync();
        });

        private void SetScanNodeState(RootTreeNode node, bool isEnabled)
        {
            if (node == null) 
                return;
            if (!isEnabled)
            {
                node.State = RootTreeNodeState.Disabled;
                return;
            }
            node.State = RootTreeNodeState.Scanning;
        }

        /// <summary>
        /// ScanningFinished event handler. Switch context to ScanResultsState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnScanningFinished(object sender, SnykOssScanEventArgs eventArgs) => this.context.TransitionTo(ScanResultsState.Instance);

        /// <summary>
        /// Handle Cli error.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnOssDisplayErrorAsync(object sender, SnykOssScanEventArgs eventArgs)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.resultsTree.OssRootNode.State = RootTreeNodeState.Error;
            this.resultsTree.OssRootNode.PresentableError = eventArgs.PresentableError;

            if (eventArgs.PresentableError?.ShowNotification ?? true)
            {
                NotificationService.Instance.ShowErrorInfoBar(eventArgs.PresentableError?.ErrorMessage);
            }

            if (eventArgs.FeaturesSettings != null && !eventArgs.FeaturesSettings.CodeSecurityEnabled)
            {
                this.context.TransitionTo(RunScanState.Instance);
            }

            await this.UpdateActionsStateAsync();
        }

        /// <summary>
        /// Initialize tool window control.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider instance.</param>
        public void Initialize(ISnykServiceProvider serviceProvider) => this.UpdateTreeNodeItemsState();

        /// <summary>
        /// Handle SnykCode error.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnSnykCodeDisplayError(object sender, SnykCodeScanEventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.resultsTree.CodeSecurityRootNode.State = RootTreeNodeState.Error;
            this.resultsTree.CodeSecurityRootNode.PresentableError = eventArgs.PresentableError;
            this.resultsTree.CodeSecurityRootNode.Clean();

            if (eventArgs.PresentableError?.ShowNotification ?? true)
            {
                NotificationService.Instance.ShowErrorInfoBar(eventArgs.PresentableError?.ErrorMessage);
            }

            if (!this.serviceProvider.Options.OssEnabled)
            {
                this.context.TransitionTo(RunScanState.Instance);
            }

            await this.UpdateActionsStateAsync();
        });

        // OnSnykCodeDisabledHandler removed - SAST checks are no longer performed

        /// <summary>
        /// ScanningCancelled event handler. Switch context to ScanResultsState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnScanningCancelled(object sender, SnykOssScanEventArgs eventArgs)
        {
            this.context.TransitionTo(RunScanState.Instance);

            this.UpdateTreeNodeItemsState();
        }

        /// <summary>
        /// DownloadStarted event handler. Switch context to DownloadState.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnDownloadStarted(object sender, SnykCliDownloadEventArgs eventArgs)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (eventArgs.IsUpdateDownload)
                {
                    this.context.TransitionTo(UpdateDownloadState.Instance);
                }
                else
                {
                    this.context.TransitionTo(DownloadState.Instance);
                }

                this.Show();
            });
        }

        /// <summary>
        /// DownloadFinished event handler. Call SetInitialState() method.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnDownloadFinished(object sender, SnykCliDownloadEventArgs eventArgs) => this.DetermineInitScreen();

        /// <summary>
        /// DownloadUpdate event handler. Call UpdateDonwloadProgress() method.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnDownloadUpdateAsync(object sender, SnykCliDownloadEventArgs eventArgs) => await this.UpdateDownloadProgressAsync(eventArgs.Progress);

        /// <summary>
        /// DownloadCancelled event handler. Call SetInitialState() method.
        /// </summary>
        /// <param name="sender">Source object.</param>
        /// <param name="eventArgs">Event args.</param>
        public void OnDownloadCancelled(object sender, SnykCliDownloadEventArgs eventArgs)
        {
            if (SnykCli.IsCliFileFound(serviceProvider.Options.CliCustomPath))
            {
                if (LanguageClientHelper.LanguageClientManager() != null)
                    ThreadHelper.JoinableTaskFactory.RunAsync(async () => await LanguageClientHelper.LanguageClientManager().RestartServerAsync()).FireAndForget();
                
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    this.DetermineInitScreen();
                });
            }
            else
            {
                this.messagePanel.Text = "Snyk CLI not found. You can specify a path to a Snyk CLI executable from the settings.";
            }
        }

        private void OnDownloadFailed(object sender, Exception e)
        {
            if (SnykCli.IsCliFileFound(serviceProvider.Options.CliCustomPath))
            {
                if (LanguageClientHelper.LanguageClientManager() != null)
                    ThreadHelper.JoinableTaskFactory.RunAsync(async () => await LanguageClientHelper.LanguageClientManager().RestartServerAsync()).FireAndForget();
                this.DetermineInitScreen();
            }
            else
            {
                this.messagePanel.Text =
                "Failed to download Snyk CLI. You can specify a path to a Snyk CLI executable from the settings.";
            }
        }

        /// <summary>
        /// Show tool window.
        /// </summary>
        public void Show() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsWindowFrame windowFrame = (IVsWindowFrame)this.toolWindow.Frame;

            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        });

        /// <summary>
        /// Cancel current task by user request.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        public void CancelIfCancellationRequested(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }
        }

        /// <summary>
        /// Display main message.
        /// </summary>
        /// <param name="text">Main message text.</param>
        public void DisplayMainMessage(string text) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.messagePanel.Text = text;

            this.messagePanel.Visibility = Visibility.Visible;
            this.DescriptionPanel.Visibility = Visibility.Collapsed;
        });

        /// <summary>
        /// Hide main message.
        /// </summary>
        public void HideMainMessage() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.messagePanel.Text = string.Empty;

            this.messagePanel.Visibility = Visibility.Collapsed;
        });

        /// <summary>
        /// Clean vulnerability tree and transition state to RunScanState.
        /// </summary>
        public void Clean() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.resultsTree.Clear();

            this.UpdateTreeNodeItemsState();
            this.DetermineInitScreen();
        });

        /// <summary>
        /// Switch to main thread and update state of toolbar (commands).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task UpdateActionsStateAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await Task.WhenAll(
                SnykScanCommand.Instance.UpdateStateAsync(),
                SnykStopCurrentTaskCommand.Instance.UpdateStateAsync(),
                SnykCleanPanelCommand.Instance.UpdateStateAsync(),
                SnykOpenSettingsCommand.Instance.UpdateStateAsync());
        }

        public async Task UpdateScreenStateAsync()
        {
            await Task.Delay(200);
            this.DetermineInitScreen();
        }

        private async Task OnOssScanningFinishedAsync() => await this.UpdateActionsStateAsync();

        private async Task OnSnykCodeScanningFinishedAsync() => await this.UpdateActionsStateAsync();
        private async Task OnIacScanningFinishedAsync() => await this.UpdateActionsStateAsync();

        private void OnSettingsChanged(object sender, SnykSettingsChangedEventArgs e) => this.UpdateTreeNodeItemsState();

        private async Task OnTaskFinishedAsync() => await this.UpdateActionsStateAsync();

        private void UpdateTreeNodeItemsState() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var options = this.serviceProvider.Options;
            
            this.resultsTree.OssRootNode.State = options.ApiToken.IsValid() && options.OssEnabled ? RootTreeNodeState.Enabled : RootTreeNodeState.Disabled;
            this.resultsTree.IacRootNode.State = options.ApiToken.IsValid() && options.IacEnabled ? RootTreeNodeState.Enabled : RootTreeNodeState.Disabled;
            HandleBranchSelectorNode(serviceProvider, this.resultsTree.OssRootNode);
            HandleBranchSelectorNode(serviceProvider, this.resultsTree.CodeSecurityRootNode);
            HandleBranchSelectorNode(serviceProvider, this.resultsTree.IacRootNode);
            // SAST enablement check removed - state is based only on user's checkbox setting
            this.resultsTree.CodeSecurityRootNode.State = options.SnykCodeSecurityEnabled ? RootTreeNodeState.Enabled : RootTreeNodeState.Disabled;
        });

        private void HandleBranchSelectorNode(ISnykServiceProvider serviceProvider, RootTreeNode rootTreeNode)
        {
            var currentFolder = ThreadHelper.JoinableTaskFactory.Run(async () =>
                await serviceProvider.SolutionService.GetSolutionFolderAsync()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            var folderConfig = serviceProvider.Options?.FolderConfigs?.SingleOrDefault(x => x.FolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) == currentFolder);
            if (folderConfig == null)
                return;

            var isDeltaEnabled = this.serviceProvider.Options?.EnableDeltaFindings ?? false;
            var baseBranchTreeNode = rootTreeNode.Items.SingleOrDefault(x => x is BaseBranchTreeNode);
            if (isDeltaEnabled)
            {
                if (baseBranchTreeNode == null)
                {
                    rootTreeNode.Items.Insert(0, new BaseBranchTreeNode (rootTreeNode) { Title = $"Base branch: {folderConfig.BaseBranch}" });
                }
                else
                {
                    baseBranchTreeNode.Title = $"Base branch: {folderConfig.BaseBranch}";
                }
                    
            }
            else
            {
                if (baseBranchTreeNode != null)
                    rootTreeNode.Items.Remove(baseBranchTreeNode);
            }
        }

        // GetSastRootNodeState removed - SAST checks are no longer performed

        /// <summary>
        /// Append CLI results to tree.
        /// </summary>
        /// <param name="cliResult">CLI result.</param>
        private void AppendOssResultToTree(IDictionary<string, IEnumerable<Issue>> cliResult)
        {
            this.context.TransitionTo(ScanResultsState.Instance);

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.resultsTree.OssResult = cliResult;
                this.resultsTree.SetCurrentSelectedNode();
            });
        }

        private void AppendSnykCodeResultToTree(IDictionary<string, IEnumerable<Issue>> analysisResult)
        {
            this.context.TransitionTo(ScanResultsState.Instance);

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (analysisResult != null)
                {
                    this.resultsTree.AnalysisResults = analysisResult;
                    this.resultsTree.SetCurrentSelectedNode();
                }
                else
                {
                    this.resultsTree.CodeSecurityRootNode.State = RootTreeNodeState.NoFilesForSnykCodeScan;
                }
            });
        }

        private void AppendSnykIacResultToTree(IDictionary<string, IEnumerable<Issue>> analysisResult)
        {
            this.context.TransitionTo(ScanResultsState.Instance);

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (analysisResult != null)
                {
                    this.resultsTree.IacResults = analysisResult;
                    this.resultsTree.SetCurrentSelectedNode();
                }
                else
                {
                    this.resultsTree.IacRootNode.State = RootTreeNodeState.NoFilesForSnykCodeScan;
                }
            });
        }


        /// <summary>
        /// Update progress bar.
        /// </summary>
        /// <param name="value">Progress bar value.</param>
        private async Task UpdateDownloadProgressAsync(int value)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.progressBar.Value = value;

            this.messagePanel.Text = $"Downloading latest Snyk CLI release {value}%...";

            this.messagePanel.Visibility = Visibility.Visible;
            this.DescriptionPanel.Visibility = Visibility.Collapsed;
        }

        private void VulnerabilitiesTree_SelectetVulnerabilityChanged(object sender, RoutedEventArgs args)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.messagePanel.Visibility = Visibility.Collapsed;

                if (this.resultsTree.SelectedItem is OssTreeNode)
                {
                    this.HandleOssTreeNodeSelected();

                    return;
                }

                if (this.resultsTree.SelectedItem is CodeTreeNode)
                {
                    await this.HandleSnykCodeTreeNodeSelectedAsync();

                    return;
                }

                if (this.resultsTree.SelectedItem is IacTreeNode)
                {
                    await this.HandleIacTreeNodeSelectedAsync();

                    return;
                }

                var baseBranchTreeNode = this.resultsTree.SelectedItem as BaseBranchTreeNode;
                if (baseBranchTreeNode != null && !BranchSelectorDialogWindow.IsOpen)
                {
                    try
                    {
                        baseBranchTreeNode.IsSelected = false;
                        baseBranchTreeNode.Parent.IsSelected = true;
                        new BranchSelectorDialogWindow(serviceProvider).ShowDialog();
                        return;
                    }
                    catch (Exception)
                    {

                    }
                }

                this.HandleRootTreeNodeSelected();
            });
        }

        private void HandleRootTreeNodeSelected()
        {
            var selectedItem = this.resultsTree.SelectedItem;

            // Check if root node has an error and display error HTML
            if (selectedItem is RootTreeNode rootTreeNode &&
                rootTreeNode.State == RootTreeNodeState.Error &&
                rootTreeNode.PresentableError != null &&
                !string.IsNullOrEmpty(rootTreeNode.PresentableError.ErrorMessage))
            {
                this.DescriptionPanel.Visibility = Visibility.Visible;
                this.messagePanel.Visibility = Visibility.Collapsed;

                // Generate error HTML content
                var errorHtml = $@"
<!DOCTYPE html>
<html>
<head>
  <meta http-equiv='Content-Type' content='text/html; charset=unicode' />
  <meta http-equiv='X-UA-Compatible' content='IE=edge' />
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <meta http-equiv=""Content-Security-Policy""
        content=""style-src 'self' 'nonce-${{nonce}}' 'nonce-ideNonce' https://fonts.googleapis.com;
        script-src 'nonce-${{nonce}}' https://fonts.googleapis.com;
        font-src 'self' https://fonts.gstatic.com;"" />
  <style nonce=${{nonce}}>
    @import url('https://fonts.googleapis.com/css2?family=Inter&display=swap');

    :root {{
      font-size: var(--main-font-size);
      --default-font: ""SF Pro Text"", ""Segoe UI"", ""Ubuntu"", Geneva, Verdana, Tahoma, sans-serif;
      --ide-background-color: var(--vscode-sideBar-background);
      --text-color: var(--vscode-foreground);
      --input-border: var(--vscode-input-border);
    }}

    body {{
      background-color: var(--ide-background-color);
      color: var(--text-color);
      font-family: var(--default-font);
      padding: 1.2rem;
      margin: 0;
    }}

    .snx-h1 {{
      font-size: 1.6rem;
      font-weight: 400;
      margin: .4rem 0 1.2rem 0;
      color: #d32f2f;
    }}

    .error-details {{
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }}

    .error-field {{
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
    }}

    .error-label {{
      font-weight: 600;
      font-size: 0.9rem;
      opacity: 0.7;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }}

    .error-value {{
      font-size: 1rem;
      line-height: 1.5;
      word-wrap: break-word;
    }}

    code {{
      background-color: var(--code-background-color);
      padding: 0.2rem 0.4rem;
      border-radius: 3px;
      font-family: 'Courier New', monospace;
      font-size: 0.9rem;
    }}
  </style>
  ${{ideStyle}}
</head>
<body>
  <h1 class=""snx-h1"">⚠️ Scan Error</h1>
  <div class=""error-details"">
    <div class=""error-field"">
      <div class=""error-label"">Message</div>
      <div class=""error-value"">{WebUtility.HtmlEncode(rootTreeNode.PresentableError.ErrorMessage)}</div>
    </div>";

                if (!string.IsNullOrEmpty(rootTreeNode.PresentableError.Path))
                {
                    errorHtml += $@"
    <div class=""error-field"">
      <div class=""error-label"">Path</div>
      <div class=""error-value"">{WebUtility.HtmlEncode(rootTreeNode.PresentableError.Path)}</div>
    </div>";
                }

                if (!string.IsNullOrEmpty(rootTreeNode.PresentableError.Command))
                {
                    errorHtml += $@"
    <div class=""error-field"">
      <div class=""error-label"">Command</div>
      <div class=""error-value""><code>{WebUtility.HtmlEncode(rootTreeNode.PresentableError.Command)}</code></div>
    </div>";
                }

                if (rootTreeNode.PresentableError.Code > 0)
                {
                    errorHtml += $@"
    <div class=""error-field"">
      <div class=""error-label"">Error Code</div>
      <div class=""error-value"">{rootTreeNode.PresentableError.Code}</div>
    </div>";
                }

                errorHtml += @"
  </div>
</body>
</html>";

                this.DescriptionPanel.SetContent(errorHtml, "static");
                return;
            }

            this.DescriptionPanel.Visibility = Visibility.Collapsed;
            this.messagePanel.Visibility = Visibility.Visible;

            this.messagePanel.ShowSelectIssueMessage();
        }

        private void HandleOssTreeNodeSelected()
        {
            var ossTreeNode = this.resultsTree.SelectedItem as OssTreeNode;

            var issue = ossTreeNode?.Issue;
            this.resultsTree.CurrentTreeNode = ossTreeNode;
            if (issue != null)
            {
                this.DescriptionPanel.Visibility = Visibility.Visible;
                FillHtmlPanel(issue.Id, Product.Oss, issue.AdditionalData?.Details);

                VsCodeService.Instance.OpenAndNavigate(
                    issue.FilePath,
                    issue.Range.Start.Line,
                    issue.Range.Start.Character,
                    issue.Range.End.Line,
                    issue.Range.End.Character);
            }
            else
            {
                this.DescriptionPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void FillHtmlPanel(string issueId, string product, string html)
        {
            var languageClientManager = LanguageClientHelper.LanguageClientManager();
            if (languageClientManager == null) return;
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                if (string.IsNullOrEmpty(html))
                {
                    try
                    {
                        html = await languageClientManager.InvokeGenerateIssueDescriptionAsync(issueId,
                            SnykVSPackage.Instance.DisposalToken);
                    }
                    catch
                    {
                        Logger.Error("couldn't load html for issue {0}", issueId);
                        html = string.Empty;
                    }
                }

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.DescriptionPanel.SetContent(html, product);
            });
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task HandleSnykCodeTreeNodeSelectedAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            this.DescriptionPanel.Visibility = Visibility.Visible;

            var snykCodeTreeNode = this.resultsTree.SelectedItem as CodeTreeNode;
            this.resultsTree.CurrentTreeNode = snykCodeTreeNode;
            if (snykCodeTreeNode == null) return;

            var issue = snykCodeTreeNode.Issue;
            FillHtmlPanel(issue.Id, Product.Code, issue.AdditionalData?.Details);


            VsCodeService.Instance.OpenAndNavigate(
                issue.FilePath,
                issue.Range.Start.Line,
                issue.Range.Start.Character,
                issue.Range.End.Line,
                issue.Range.End.Character);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task HandleIacTreeNodeSelectedAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            this.DescriptionPanel.Visibility = Visibility.Visible;

            var iacTreeNode = this.resultsTree.SelectedItem as IacTreeNode;
            this.resultsTree.CurrentTreeNode = iacTreeNode;
            if (iacTreeNode == null) return;
            
            var issue = iacTreeNode.Issue;
            FillHtmlPanel(issue.Id, Product.Iac, issue.AdditionalData?.CustomUIContent);


            VsCodeService.Instance.OpenAndNavigate(
                issue.FilePath,
                issue.Range.Start.Line,
                issue.Range.Start.Character,
                issue.Range.End.Line,
                issue.Range.End.Character);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e) => SnykTasksService.Instance.CancelTasks();

        private void CleanButton_Click(object sender, RoutedEventArgs e) => this.context.TransitionTo(RunScanState.Instance);

        private void SnykToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.context.IsEmptyState())
            {
                this.DetermineInitScreen();
            }

            this.UpdateTreeNodeItemsState();
        }

        /// <summary>
        /// If api token is valid it will show run scan screen. If api token is invalid it will show Welcome screen.
        /// </summary>
        private void DetermineInitScreen()
        {
            var options = this.serviceProvider.Options;

            if (!LanguageClientHelper.IsLanguageServerReady())
            {
                this.context.TransitionTo(InitializingState.Instance);
                return;
            }

            if (SnykTasksService.Instance.IsTaskRunning())
            {
                this.messagePanel.ShowScanningMessage();
                return;
            }

            var isFolderTrusted = ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                var solutionFolderPath = await this.serviceProvider.SolutionService.GetSolutionFolderAsync();
                var isFolderTrusted = this.serviceProvider.WorkspaceTrustService.IsFolderTrusted(solutionFolderPath);
                return isFolderTrusted;
            });

            if (options.ApiToken.IsValid() && isFolderTrusted)
            {
                this.context.TransitionTo(RunScanState.Instance);
                return;
            }

            this.context.TransitionTo(OverviewState.Instance);
        }
    }
}