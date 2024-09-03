﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;
using Snyk.Common;
using Snyk.VisualStudio.Extension.Service;
using StreamJsonRpc;

namespace Snyk.VisualStudio.Extension.Language
{
    public class SnykLanguageClientCustomTarget
    {
        private readonly ISnykServiceProvider serviceProvider;
        private readonly ConcurrentDictionary<string, IEnumerable<Issue>> snykCodeIssueDictionary = new();
        private readonly ConcurrentDictionary<string, IEnumerable<Issue>> snykOssIssueDictionary = new();
        private readonly ConcurrentDictionary<string, IEnumerable<Issue>> snykIaCIssueDictionary = new();
        private static readonly ILogger _logger = LogManager.ForContext<SnykLanguageClientCustomTarget>();
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
                snykCodeIssueDictionary.TryRemove(parsedUri.AbsolutePath, out _);
                snykOssIssueDictionary.TryRemove(parsedUri.AbsolutePath, out _);
                snykIaCIssueDictionary.TryRemove(parsedUri.AbsolutePath, out _);
                return;
            }

            if (diagnosticsArray[0]["source"] == null)
            {
                return;
            }

            // TODO: handle the case when source is: 'Snyk Error'

            var source = LspSourceToProduct(diagnosticsArray[0]["source"].ToString());
            var dataList = diagnosticsArray.Where(x => x["data"] != null)
                .Select(x =>
                {
                    var issue = x["data"].TryParse<Issue>();
                    issue.Product = LspSourceToProduct(source);
                    return issue;
                });


            switch (source)
            {
                case "code":
                    snykCodeIssueDictionary.TryAdd(parsedUri.AbsolutePath, dataList);
                    break;
                case "oss":
                     snykOssIssueDictionary.TryAdd(parsedUri.AbsolutePath, dataList);
                    break;
                case "iac":
                    snykIaCIssueDictionary.TryAdd(parsedUri.AbsolutePath, dataList);
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

            var lspAnalysisResult = arg.TryParse<LspAnalysisResult>();
            if (lspAnalysisResult == null) return;
            switch (lspAnalysisResult.Product)
            {
                case Product.Code:
                    await ProcessCodeScanAsnyc(lspAnalysisResult);
                    break;
                case Product.Oss:
                    await ProcessOssScanAsnyc(lspAnalysisResult);
                    break;
                case Product.Iac:
                    await ProcessIacScanAsnyc(lspAnalysisResult);
                    break;
            }
        }

        [JsonRpcMethod(LsConstants.SnykFolderConfig)]
        public async Task OnFolderConfig(JToken arg)
        {

        }

        [JsonRpcMethod(LsConstants.SnykHasAuthenticated)]
        public async Task OnHasAuthenticated(JToken arg)
        {
            if (arg == null || arg["token"] == null)
            {
                return;
            }
            var token = arg["token"].ToString();
            serviceProvider.Options.SetApiToken(token);
        }

        [JsonRpcMethod(LsConstants.SnykAddTrustedFolders)]
        public async Task OnAddTrustedFolders(JToken arg)
        {

        }

        private async Task ProcessCodeScanAsnyc(LspAnalysisResult lspAnalysisResult)
        {
            if (lspAnalysisResult.Status == "inProgress")
            {
                var featuresSettings = await serviceProvider.TasksService.GetFeaturesSettingsAsync();
                serviceProvider.TasksService.FireSnykCodeScanningStartedEvent(featuresSettings);
                return;
            }
            if (lspAnalysisResult.Status == "error")
            {
                serviceProvider.TasksService.OnSnykCodeError(lspAnalysisResult.ErrorMessage);
                serviceProvider.TasksService.FireTaskFinished();
                return;
            }

            serviceProvider.TasksService.FireCodeScanningUpdateEvent(snykCodeIssueDictionary);
            serviceProvider.TasksService.FireSnykCodeScanningFinishedEvent();
            serviceProvider.TasksService.FireTaskFinished();
        }

        private async Task ProcessOssScanAsnyc(LspAnalysisResult lspAnalysisResult)
        {
            if (lspAnalysisResult.Status == "inProgress")
            {
                serviceProvider.TasksService.FireOssScanningStartedEvent();
                return;
            }
            if (lspAnalysisResult.Status == "error")
            {
                serviceProvider.TasksService.FireOssError(lspAnalysisResult.ErrorMessage);
                serviceProvider.TasksService.FireTaskFinished();
                return;
            }

            serviceProvider.TasksService.FireOssScanningUpdateEvent(snykOssIssueDictionary);
            serviceProvider.TasksService.FireOssScanningFinishedEvent();
            serviceProvider.TasksService.FireTaskFinished();
        }

        private async Task ProcessIacScanAsnyc(LspAnalysisResult lspAnalysisResult)
        {
          
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
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
