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
            var action= queryParams["action"];
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
            serviceProvider.SnykOptionsManager.Save(serviceProvider.Options);

            var shouldEnableAutoScan = serviceProvider.Options.AutoScan && !serviceProvider.Options.InternalAutoScan;

            if (shouldEnableAutoScan)
            {
                var isFolderTrusted = await this.serviceProvider.TasksService.IsFolderTrustedAsync();
                await TaskScheduler.Default;
                if (!isFolderTrusted)
                    return;
                var currentInternalAutoScanEnabled = serviceProvider.Options.InternalAutoScan;
                serviceProvider.Options.InternalAutoScan = true;
                await serviceProvider.LanguageClientManager.DidChangeConfigurationAsync(SnykVSPackage.Instance.DisposalToken);
                if (!currentInternalAutoScanEnabled)
                    serviceProvider.TasksService.ScanAsync().FireAndForget();
            }
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
            await serviceProvider.LanguageClientManager.DidChangeConfigurationAsync(SnykVSPackage.Instance.DisposalToken);
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
                serviceProvider.TasksService.OnSnykCodeError(lsAnalysisResult.ErrorMessage);
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
                serviceProvider.TasksService.FireOssError(lsAnalysisResult.ErrorMessage);
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
                serviceProvider.TasksService.OnIacError(lsAnalysisResult.ErrorMessage);
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
