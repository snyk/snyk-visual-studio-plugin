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
        private readonly Action _reloadHtmlSettings;
        private readonly ConcurrentDictionary<string, IEnumerable<Issue>> snykCodeIssueDictionary = new();
        private readonly ConcurrentDictionary<string, IEnumerable<Issue>> snykOssIssueDictionary = new();
        private readonly ConcurrentDictionary<string, IEnumerable<Issue>> snykIaCIssueDictionary = new();
        private readonly ConcurrentDictionary<string, IEnumerable<Issue>> snykSecretsIssueDictionary = new();

        public SnykLanguageClientCustomTarget(ISnykServiceProvider serviceProvider)
            : this(serviceProvider, HtmlSettingsControl.RequestReload)
        {
        }

        internal SnykLanguageClientCustomTarget(ISnykServiceProvider serviceProvider, Action reloadHtmlSettings)
        {
            this.serviceProvider = serviceProvider;
            _reloadHtmlSettings = reloadHtmlSettings;
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
                snykSecretsIssueDictionary.TryRemove(parsedUri.UncAwareAbsolutePath(), out _);
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
                case "secrets":
                    snykSecretsIssueDictionary.AddOrUpdate(parsedUri.UncAwareAbsolutePath(), issueList, (_, _) => issueList);
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
                case Product.Secrets:
                    await ProcessSecretsScanAsync(lspAnalysisResult);
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
                await serviceProvider.AuthenticationFlowService.HandleFailedAuthenticationAsync("Authentication failed");
                return;
            }

            var token = arg["token"].ToString();
            var apiUrl = arg["apiUrl"]?.ToString();

            var oldToken = serviceProvider.Options.ApiToken?.ToString() ?? string.Empty;

            // Queue the token for the HTML settings page so the token field updates after an OAuth
            // round-trip. Queuing (rather than a direct push to a live instance) guarantees delivery
            // even when no settings page is open yet or its HTML hasn't finished loading — the next
            // control to become page-ready flushes it.
            HtmlSettingsControl.QueueAuthToken(token, apiUrl);

            // Validate before persisting: only accept an absolute http/https URL so a malformed or
            // unexpected apiUrl can't repoint the API host. Same guard as the JS snyk.login path.
            if (!string.IsNullOrEmpty(apiUrl))
            {
                if (UriExtensions.IsValidWebUrl(apiUrl))
                {
                    serviceProvider.Options.CustomEndpoint = apiUrl;
                }
                else
                {
                    Logger.Warning("Ignoring authenticated apiUrl that is not an absolute http/https URL");
                }
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

            // Re-render the settings page if it is open so the user sees updated config.
            _reloadHtmlSettings();

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

        private async Task ProcessSecretsScanAsync(LsAnalysisResult lsAnalysisResult)
        {
            if (lsAnalysisResult.Status == "inProgress")
            {
                serviceProvider.TasksService.FireSecretsScanningStartedEvent();
                return;
            }
            if (lsAnalysisResult.Status == "error")
            {
                serviceProvider.TasksService.OnSecretsError(lsAnalysisResult.PresentableError);
                serviceProvider.TasksService.FireTaskFinished();
                return;
            }

            serviceProvider.TasksService.FireSecretsScanningUpdateEvent(snykSecretsIssueDictionary);
            serviceProvider.TasksService.FireSecretsScanningFinishedEvent();
            serviceProvider.TasksService.FireTaskFinished();
        }

        private string LspSourceToProduct(string source)
        {
            return source switch
            {
                "Snyk Code" => "code",
                "Snyk Open Source" => "oss",
                "Snyk IaC" => "iac",
                "Snyk Secrets" => "secrets",
                _ => ""
            };
        }

        // Only used in tests
        public ConcurrentDictionary<string, IEnumerable<Issue>> GetCodeDictionary()
        {
            return snykCodeIssueDictionary;
        }

        // Only used in tests
        public ConcurrentDictionary<string, IEnumerable<Issue>> GetSecretsDictionary()
        {
            return snykSecretsIssueDictionary;
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
