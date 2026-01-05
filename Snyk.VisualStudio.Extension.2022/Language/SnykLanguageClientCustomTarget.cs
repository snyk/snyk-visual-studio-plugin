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
using StreamJsonRpc;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Extension;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Language
{
    public class SnykLanguageClientCustomTarget
    {
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
            var lspAnalysisResult = arg.TryParse<ShowDocumentParams>();
            if (lspAnalysisResult == null) return;
            var uri = new Uri(Uri.UnescapeDataString(lspAnalysisResult.Uri));

            // Manually parse query parameters
            var queryParams = ParseQueryString(uri.Query);
            var issueId = queryParams["issueId"];
            var product = LspSourceToProduct(queryParams["product"].Replace("+", " "));
            var action = queryParams["action"];
            if (action != "showInDetailPanel")
            {
                return;
            }
            serviceProvider.ToolWindow.SelectedItemInTree(issueId, product);
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

        [JsonRpcMethod(LsConstants.SnykFolderConfig)]
        public async Task OnFolderConfig(JToken arg)
        {
            var folderConfigs = arg.TryParse<FolderConfigsParam>();
            if (folderConfigs == null) return;

            serviceProvider.Options.FolderConfigs = folderConfigs.FolderConfigs;

            // Get current solution folder path
            var currentSolutionPath = await serviceProvider.SolutionService.GetSolutionFolderAsync();
            var matchingFolderConfig = FindMatchingFolderConfig(folderConfigs.FolderConfigs, currentSolutionPath);

            if (matchingFolderConfig != null)
            {
                // Extract auto-determined organization from matching folder config
                // Language Server is authoritative - always use its data
                if (!string.IsNullOrEmpty(matchingFolderConfig.AutoDeterminedOrg))
                {
                    // Save as auto-determined organization (from Language Server)
                    await serviceProvider.SnykOptionsManager.SaveAutoDeterminedOrgAsync(matchingFolderConfig.AutoDeterminedOrg);
                }

                // Extract preferred organization from matching folder config
                // Language Server is authoritative - always use its data, even if empty
                await serviceProvider.SnykOptionsManager.SavePreferredOrgAsync(matchingFolderConfig.PreferredOrg);

                // Extract orgSetByUser flag from Language Server
                await serviceProvider.SnykOptionsManager.SaveOrgSetByUserAsync(matchingFolderConfig.OrgSetByUser);

                // Do NOT update global organization when receiving folder configs from Language Server.
                // The global organization should remain as the user set it and be used as a fallback.
                // The Language Server handles the fallback logic: project-specific → global → web account preferred.

                // Extract additional parameters from matching folder config
                // Language Server is authoritative - always use its data
                if (matchingFolderConfig.AdditionalParameters != null && matchingFolderConfig.AdditionalParameters.Count > 0)
                {
                    var additionalParams = string.Join(" ", matchingFolderConfig.AdditionalParameters);
                    await serviceProvider.SnykOptionsManager.SaveAdditionalOptionsAsync(additionalParams);
                }
            }

            serviceProvider.SnykOptionsManager.Save(serviceProvider.Options, false);

            if (serviceProvider.Options.AutoScan)
            {
                var isFolderTrusted = await this.serviceProvider.TasksService.IsFolderTrustedAsync();
                await TaskScheduler.Default;
                if (!isFolderTrusted)
                    return;

                if (!serviceProvider.Options.InternalAutoScan)
                {
                    serviceProvider.Options.InternalAutoScan = true;
                    await serviceProvider.LanguageClientManager.DidChangeConfigurationAsync(SnykVSPackage.Instance.DisposalToken);
                    serviceProvider.TasksService.ScanAsync().FireAndForget();
                }
            }
        }

        private FolderConfig FindMatchingFolderConfig(List<FolderConfig> folderConfigs, string currentSolutionPath)
        {
            if (folderConfigs == null || string.IsNullOrEmpty(currentSolutionPath)) return null;

            foreach (var folderConfig in folderConfigs)
            {
                if (string.IsNullOrEmpty(folderConfig.FolderPath)) continue;

                // Check for exact match
                if (folderConfig.FolderPath.Equals(currentSolutionPath, StringComparison.OrdinalIgnoreCase))
                {
                    return folderConfig;
                }

                // Check if current path is within the config path (subfolder)
                // Note: This is a defensive check. In normal operation, the Language Server sends folder configs
                // that match workspace folders exactly, so exact matches should be sufficient. This subfolder
                // check handles edge cases where paths might not align perfectly (e.g., path normalization differences).
                var trimmedConfigPath = folderConfig.FolderPath.TrimEnd('\\', '/');
                if (currentSolutionPath.StartsWith(trimmedConfigPath + "\\", StringComparison.OrdinalIgnoreCase) ||
                    currentSolutionPath.StartsWith(trimmedConfigPath + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return folderConfig;
                }
            }

            return null;
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

        [JsonRpcMethod(LsConstants.SnykHasAuthenticated)]
        public async Task OnHasAuthenticated(JToken arg)
        {
            if (arg?["token"] == null)
            {
                await serviceProvider.GeneralOptionsDialogPage.HandleFailedAuthentication("Authentication failed");
                return;
            }

            var token = arg["token"].ToString();

            var apiUrl = arg["apiUrl"]?.ToString();
            if (!string.IsNullOrEmpty(apiUrl))
            {
                serviceProvider.Options.CustomEndpoint = apiUrl;
            }

            serviceProvider.Options.ApiToken = new AuthenticationToken(serviceProvider.Options.AuthenticationMethod, token);
            serviceProvider.SnykOptionsManager.Save(serviceProvider.Options);

            await serviceProvider.GeneralOptionsDialogPage.HandleAuthenticationSuccess(token, apiUrl);

            // Notify HTML settings window of auth token change
            HtmlSettingsWindow.Instance?.UpdateAuthToken(token);

            if (!serviceProvider.Options.ApiToken.IsValid())
                return;

            serviceProvider.FeatureFlagService.RefreshAsync(SnykVSPackage.Instance.DisposalToken).FireAndForget();
            if (serviceProvider.Options.AutoScan)
            {
                serviceProvider.TasksService.ScanAsync().FireAndForget();
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
