// ABOUTME: This file implements custom JSON-RPC message handlers for the Snyk Language Client
// ABOUTME: It processes diagnostics, authentication, and scan results from the Language Server
using System;
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
        public SnykLanguageClientCustomTarget(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        [JsonRpcMethod(LsConstants.OnPublishDiagnostics316)]
        public async Task OnPublishDiagnostics316(JToken arg)
        {
            // Issue rendering is driven entirely by the LS HTML tree ($/snyk.treeView); the IDE no
            // longer caches diagnostics per file. We still inspect them for the one piece of state
            // the IDE derives locally: whether any issue is ignored, which enables the Consistent
            // Ignores UI.
            var diagnosticsArray = arg?["diagnostics"] as JArray;
            if (arg?["uri"] == null || diagnosticsArray == null || diagnosticsArray.Count == 0)
            {
                return;
            }

            if (diagnosticsArray[0]["source"] == null)
            {
                return;
            }

            foreach (var diagnostic in diagnosticsArray)
            {
                var data = diagnostic["data"];
                if (data == null) continue;

                var issue = data.TryParse<Issue>();
                if (issue == null) continue;
                if (issue.IsIgnored)
                {
                    serviceProvider.Options.ConsistentIgnoresEnabled = true;
                }
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
            var showDocumentParams = arg.TryParse<ShowDocumentParams>();
            if (string.IsNullOrEmpty(showDocumentParams?.Uri)) return;

            // Build the Uri from the raw string. Do NOT pre-unescape it: ParseQueryString already
            // unescapes each query value once, and pre-unescaping double-decodes any value with a
            // percent sequence and can produce a string that makes new Uri(...) throw. Guard the
            // parse so a malformed URI can't escape this JSON-RPC handler.
            Uri uri;
            try
            {
                uri = new Uri(showDocumentParams.Uri);
            }
            catch (UriFormatException ex)
            {
                Logger.Warning(ex, "Ignoring showDocument request with malformed URI");
                return;
            }

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
                serviceProvider?.ToolWindow?.SelectedItemInTree(issueId, NormalizeProduct(productRaw));
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
                // Split on the FIRST '=' only: a value can legitimately contain '=' (e.g. base64
                // issue IDs ending in '='/'=='), and splitting on every '=' would drop those pairs.
                var separatorIndex = pair.IndexOf('=');
                if (separatorIndex <= 0) continue;

                var key = Uri.UnescapeDataString(pair.Substring(0, separatorIndex));
                var value = Uri.UnescapeDataString(pair.Substring(separatorIndex + 1));
                result[key] = value;
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
            var treePanel = serviceProvider?.ToolWindow?.TreeHtmlPanel;
            if (treeViewParam?.TreeViewHtml == null || treePanel == null) return;

            // SetContent marshals to the UI thread itself, so we don't switch here — this keeps the
            // JSON-RPC dispatch thread unblocked, matching OnScanSummary's non-blocking pattern. HTML
            // and count are applied together so the "Clean" command's enabled state can never
            // disagree with the rendered tree (see TreeHtmlPanel.SetContent).
            treePanel.SetContent(treeViewParam.TreeViewHtml, treeViewParam.TotalIssues);
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

            // LS-delivered auth result (OAuth flow completed by the LS): persist the token but
            // do NOT trigger SettingsChanged (would send DidChangeConfigurationAsync back to the LS),
            // and do NOT update the override tracker — recording an LS-delivered token as a user
            // override would corrupt ChangedConfigKeys, especially during early-startup auth before
            // Load() has seeded the tracker (snapshot would be empty → persisted set overwritten null).
            serviceProvider.SnykOptionsManager.Save(
                serviceProvider.Options,
                triggerSettingsChangedEvent: false,
                updateOverrideTracker: false);

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
            // updateOverrideTracker:false — LS-pushed values (org, LDX flags) must NEVER be recorded
            // as user overrides; doing so would defeat the purpose of IDE-2152 by re-sending them
            // with changed:true and clobbering the very org defaults this feature preserves.
            this.serviceProvider.SnykOptionsManager.Save(options, triggerSettingsChangedEvent: false, updateOverrideTracker: false);
            Logger.Debug("$/snyk.configuration applied: {KeyCount} global setting(s), {FolderCount} folder config(s) stored in-memory",
                param.Settings?.Count ?? 0, param.FolderConfigs?.Count ?? 0);

            // Trigger first scan now that folder configs have arrived.
            // AutoScan: persisted user preference (also sent to the LS as scan_automatic).
            // InternalAutoScan: IDE-side runtime gate only (NOT sent to the LS); $/snyk.configuration
            // may arrive multiple times, so the gate ensures we trigger the IDE-side scan exactly once.
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
            // updateOverrideTracker:false — LS is pushing the resolved trusted-folder set back;
            // this is not a user override action and must not pollute ChangedConfigKeys.
            this.serviceProvider.SnykOptionsManager.Save(serviceProvider.Options, triggerSettingsChangedEvent: false, updateOverrideTracker: false);
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
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
