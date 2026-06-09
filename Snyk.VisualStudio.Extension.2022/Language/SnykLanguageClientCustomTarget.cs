// ABOUTME: This file implements custom JSON-RPC message handlers for the Snyk Language Client
// ABOUTME: It processes diagnostics, authentication, and scan results from the Language Server
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using Serilog;
using StreamJsonRpc;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Extension;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Language
{
    public class SnykLanguageClientCustomTarget
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykLanguageClientCustomTarget>();
        private readonly ISnykServiceProvider serviceProvider;
        private readonly ConcurrentDictionary<string, IEnumerable<Issue>> snykCodeIssueDictionary = new();
        private readonly ConcurrentDictionary<string, IEnumerable<Issue>> snykOssIssueDictionary = new();
        private readonly ConcurrentDictionary<string, IEnumerable<Issue>> snykIaCIssueDictionary = new();
        public SnykLanguageClientCustomTarget(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        [JsonRpcMethod(LsConstants.OnPublishDiagnostics316)]
        public async Task OnPublishDiagnostics316(JToken arg)
        {
            var uri = arg["uri"];
            var diagnosticsArray = (JArray)arg["diagnostics"];
            if (uri == null)
            {
                return;
            }

            var parsedUri = new Uri(uri.ToString());
            if (diagnosticsArray == null || diagnosticsArray.Count == 0)
            {
                snykCodeIssueDictionary.TryRemove(parsedUri.UncAwareAbsolutePath(), out _);
                snykOssIssueDictionary.TryRemove(parsedUri.UncAwareAbsolutePath(), out _);
                snykIaCIssueDictionary.TryRemove(parsedUri.UncAwareAbsolutePath(), out _);
                return;
            }

            if (diagnosticsArray[0]["source"] == null)
            {
                return;
            }

            var source = LspSourceToProduct(diagnosticsArray[0]["source"].ToString());
            var issueList = diagnosticsArray.Where(x => x["data"] != null)
                .Select(x =>
                {
                    var issue = x["data"].TryParse<Issue>();
                    if (issue.IsIgnored)
                    {
                        serviceProvider.Options.ConsistentIgnoresEnabled = true;
                    }
                    return issue;
                }).ToList();

            switch (source)
            {
                case "code":
                    snykCodeIssueDictionary.AddOrUpdate(parsedUri.UncAwareAbsolutePath(), issueList, (_, _) => issueList);
                    break;
                case "oss":
                    snykOssIssueDictionary.AddOrUpdate(parsedUri.UncAwareAbsolutePath(), issueList, (_, _) => issueList);
                    break;
                case "iac":
                    snykIaCIssueDictionary.AddOrUpdate(parsedUri.UncAwareAbsolutePath(), issueList, (_, _) => issueList);
                    break;
                default:
                    throw new InvalidProductTypeException();

            }
        }

        [JsonRpcMethod(LsConstants.SnykScan)]
        public async Task OnSnykScan(JToken arg)
        {
            if (serviceProvider.TasksService.SnykScanTokenSource?.IsCancellationRequested ?? false)
            {
                serviceProvider.TasksService.FireScanningCancelledEvent();
                return;
            }

            var lspAnalysisResult = arg.TryParse<LsAnalysisResult>();
            if (lspAnalysisResult == null) return;
            switch (lspAnalysisResult.Product)
            {
                case Product.Code:
                    await ProcessCodeScanAsync(lspAnalysisResult);
                    break;
                case Product.Oss:
                    await ProcessOssScanAsync(lspAnalysisResult);
                    break;
                case Product.Iac:
                    await ProcessIacScanAsync(lspAnalysisResult);
                    break;
            }
        }

        [JsonRpcMethod(LsConstants.ShowDocument)]
        public async Task OnShowDocument(JToken arg)
        {
            var showDocumentParams = arg.TryParse<ShowDocumentParams>();
            if (string.IsNullOrEmpty(showDocumentParams?.Uri)) return;

            var uri = new Uri(Uri.UnescapeDataString(showDocumentParams.Uri));
            var queryParams = ParseQueryString(uri.Query);
            queryParams.TryGetValue("action", out var action);

            // snyk:// detail-panel request (from a tree node click via snyk.navigateToRange):
            // populate the issue description panel.
            if (action == "showInDetailPanel")
            {
                if (!queryParams.TryGetValue("issueId", out var issueId) ||
                    !queryParams.TryGetValue("product", out var productRaw))
                {
                    return;
                }
                serviceProvider.ToolWindow.SelectedItemInTree(issueId, NormalizeProduct(productRaw));
                return;
            }

            // Plain file-open request: navigate the editor to the selected range. The HTML tree
            // triggers this via snyk.navigateToRange, which the LS turns into window/showDocument.
            // External requests (open-in-browser) are not editor navigations.
            if (showDocumentParams.External) return;
            var selection = showDocumentParams.Selection;
            if (selection?.Start == null || selection.End == null) return;

            VsCodeService.Instance.OpenAndNavigate(
                uri.LocalPath,
                selection.Start.Line,
                selection.Start.Character,
                selection.End.Line,
                selection.End.Character);
        }

        // The tree emits product codenames ("code"/"oss"/"iac") in the detail-panel URI, while
        // other producers send display names ("Snyk Code"). Map display names, pass codenames through.
        private string NormalizeProduct(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            var mapped = LspSourceToProduct(raw.Replace("+", " "));
            return string.IsNullOrEmpty(mapped) ? raw.ToLowerInvariant() : mapped;
        }

        static Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (query.StartsWith("?"))
                query = query.Substring(1);

            foreach (var pair in query.Split('&'))
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    var key = Uri.UnescapeDataString(parts[0]);
                    var value = Uri.UnescapeDataString(parts[1]);
                    result[key] = value;
                }
            }
            return result;
        }

        [JsonRpcMethod(LsConstants.SnykScanSummary)]
        public async Task OnScanSummary(JToken arg)
        {
            var html = arg;
            var scanSummaryParam = arg.TryParse<ScanSummaryParam>();
            if (scanSummaryParam?.ScanSummary == null || serviceProvider?.ToolWindow?.SummaryPanel == null) return;
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                serviceProvider.ToolWindow.SummaryPanel.SetContent(scanSummaryParam.ScanSummary, "summary");
            }).FireAndForget();
        }

        [JsonRpcMethod(LsConstants.SnykTreeView)]
        public async Task OnTreeView(JToken arg)
        {
            var treeViewParam = arg.TryParse<TreeViewParams>();
            if (treeViewParam?.TreeViewHtml == null || serviceProvider?.ToolWindow?.TreeHtmlPanel == null) return;
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                serviceProvider.ToolWindow.TreeHtmlPanel.TotalIssues = treeViewParam.TotalIssues;
                serviceProvider.ToolWindow.TreeHtmlPanel.SetContent(treeViewParam.TreeViewHtml);
            }).FireAndForget();
        }

        [JsonRpcMethod(LsConstants.SnykHasAuthenticated)]
        public async Task OnHasAuthenticated(JToken arg)
        {
            if (arg?["token"] == null)
            {
                await serviceProvider.AuthenticationFlowService.HandleFailedAuthenticationAsync("Authentication failed");
                return;
            }

            var token = arg["token"].ToString();
            var apiUrl = arg["apiUrl"]?.ToString();

            var oldToken = serviceProvider.Options.ApiToken?.ToString() ?? string.Empty;

            // Always notify the live HTML settings control (modal or Tools→Options page) so
            // the token field updates immediately after an OAuth round-trip.
            HtmlSettingsControl.Instance?.UpdateAuthToken(token, apiUrl);

            if (!string.IsNullOrEmpty(apiUrl))
            {
                serviceProvider.Options.CustomEndpoint = apiUrl;
            }

            serviceProvider.Options.ApiToken = new AuthenticationToken(serviceProvider.Options.AuthenticationMethod, token);

            // Persist without triggering SettingsChanged → DidChangeConfigurationAsync back to the LS.
            serviceProvider.SnykOptionsManager.Save(serviceProvider.Options, false);

            // Scan only when this is a new login (old token was blank).
            // Token refresh also has old token non-blank, so no scan.
            var isNewLogin = string.IsNullOrEmpty(oldToken) && !string.IsNullOrEmpty(token);
            if (!isNewLogin)
                return;

            await serviceProvider.AuthenticationFlowService.HandleAuthenticationSuccessAsync(token, apiUrl);

            if (!serviceProvider.Options.ApiToken.IsValid())
                return;

            serviceProvider.FeatureFlagService.RefreshAsync(SnykVSPackage.Instance.DisposalToken).FireAndForget();
            if (serviceProvider.Options.AutoScan)
            {
                serviceProvider.TasksService.ScanAsync().FireAndForget();
            }
        }

        [JsonRpcMethod(LsConstants.SnykConfiguration)]
        public async Task OnSnykConfiguration(JToken arg)
        {
            var param = arg.TryParse<LspConfigurationParam>();
            if (param == null) return;

            var options = serviceProvider.Options;
            GlobalSettingsApplier.Apply(param.Settings, options);
            options.FolderConfigs = FolderConfigApplier.Apply(options.FolderConfigs, param.FolderConfigs);
            // Persist without re-triggering DidChangeConfigurationAsync (avoids feedback loop).
            this.serviceProvider.SnykOptionsManager.Save(options, false);
            Logger.Debug("$/snyk.configuration applied: {KeyCount} global setting(s), {FolderCount} folder config(s) stored in-memory",
                param.Settings?.Count ?? 0, param.FolderConfigs?.Count ?? 0);

            // Trigger first scan now that folder configs have arrived.
            // AutoScan vs InternalAutoScan vs ScanningMode:
            // - AutoScan: persisted user preference
            // - InternalAutoScan: runtime gate; false until we are ready to scan (avoids scanning before LS is initialized)
            // $/snyk.configuration may arrive multiple times; the InternalAutoScan gate ensures we scan exactly once.
            if (options.AutoScan)
            {
                var isFolderTrusted = await this.serviceProvider.TasksService.IsFolderTrustedAsync();
                await TaskScheduler.Default;
                if (!isFolderTrusted)
                    return;

                if (!options.InternalAutoScan)
                {
                    options.InternalAutoScan = true;
                    await serviceProvider.LanguageClientManager.DidChangeConfigurationAsync(SnykVSPackage.Instance.DisposalToken);
                    serviceProvider.TasksService.ScanAsync().FireAndForget();
                }
            }
        }

        [JsonRpcMethod(LsConstants.SnykAddTrustedFolders)]
        public async Task OnAddTrustedFolders(JToken arg)
        {
            var trustedFolders = arg.TryParse<LsTrust>();
            if (trustedFolders == null) return;

            serviceProvider.Options.TrustedFolders = new HashSet<string>(trustedFolders.TrustedFolders);
            this.serviceProvider.SnykOptionsManager.Save(serviceProvider.Options, false);
            // Don't call DidChangeConfigurationAsync here as it creates an infinite loop
            // The Language Server already knows about the trusted folders changes
        }

        private async Task ProcessCodeScanAsync(LsAnalysisResult lsAnalysisResult)
        {
            if (lsAnalysisResult.Status == "inProgress")
            {
                var featuresSettings = await serviceProvider.TasksService.GetFeaturesSettingsAsync();
                serviceProvider.TasksService.FireSnykCodeScanningStartedEvent(featuresSettings);
                return;
            }
            if (lsAnalysisResult.Status == "error")
            {
                serviceProvider.TasksService.OnSnykCodeError(lsAnalysisResult.PresentableError);
                serviceProvider.TasksService.FireTaskFinished();
                return;
            }

            serviceProvider.TasksService.FireCodeScanningUpdateEvent(snykCodeIssueDictionary);
            serviceProvider.TasksService.FireSnykCodeScanningFinishedEvent();
            serviceProvider.TasksService.FireTaskFinished();
        }

        private async Task ProcessOssScanAsync(LsAnalysisResult lsAnalysisResult)
        {
            if (lsAnalysisResult.Status == "inProgress")
            {
                serviceProvider.TasksService.FireOssScanningStartedEvent();
                return;
            }
            if (lsAnalysisResult.Status == "error")
            {
                serviceProvider.TasksService.FireOssError(lsAnalysisResult.PresentableError);
                serviceProvider.TasksService.FireTaskFinished();
                return;
            }

            serviceProvider.TasksService.FireOssScanningUpdateEvent(snykOssIssueDictionary);
            serviceProvider.TasksService.FireOssScanningFinishedEvent();
            serviceProvider.TasksService.FireTaskFinished();
        }

        private async Task ProcessIacScanAsync(LsAnalysisResult lsAnalysisResult)
        {
            if (lsAnalysisResult.Status == "inProgress")
            {
                var featuresSettings = await serviceProvider.TasksService.GetFeaturesSettingsAsync();
                serviceProvider.TasksService.FireIacScanningStartedEvent(featuresSettings);
                return;
            }
            if (lsAnalysisResult.Status == "error")
            {
                serviceProvider.TasksService.OnIacError(lsAnalysisResult.PresentableError);
                serviceProvider.TasksService.FireTaskFinished();
                return;
            }

            serviceProvider.TasksService.FireIacScanningUpdateEvent(snykIaCIssueDictionary);
            serviceProvider.TasksService.FireIacScanningFinishedEvent();
            serviceProvider.TasksService.FireTaskFinished();
        }

        private string LspSourceToProduct(string source)
        {
            return source switch
            {
                "Snyk Code" => "code",
                "Snyk Open Source" => "oss",
                "Snyk IaC" => "iac",
                _ => ""
            };
        }

        // Only used in tests
        public ConcurrentDictionary<string, IEnumerable<Issue>> GetCodeDictionary()
        {
            return snykCodeIssueDictionary;
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
