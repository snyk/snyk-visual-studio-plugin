using System;
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

        [JsonRpcMethod("$/snyk.publishDiagnostics316")]
        public void OnPublishDiagnostics316(JToken arg)
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

        [JsonRpcMethod("$/snyk.scan")]
        public async Task OnSnykScan(JToken arg)
        {
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
                return;
            }

            serviceProvider.TasksService.FireCodeScanningUpdateEvent(snykCodeIssueDictionary);
            serviceProvider.TasksService.FireSnykCodeScanningFinishedEvent();
        }

        private async Task ProcessOssScanAsnyc(LspAnalysisResult lspAnalysisResult)
        {
            //if (lspAnalysisResult.Status == "inProgress")
            //{
            //    serviceProvider.TasksService.FireOssScanningStartedEvent();
            //    return;
            //}
            //if (lspAnalysisResult.Status == "error")
            //{
            //    serviceProvider.TasksService.FireOssError(lspAnalysisResult.ErrorMessage);
            //    return;
            //}

            //serviceProvider.TasksService.FireOssScanningUpdateEvent(snykCodeIssueDictionary);
            //serviceProvider.TasksService.FireOssScanningFinishedEvent();
        }

        private async Task ProcessIacScanAsnyc(LspAnalysisResult lspAnalysisResult)
        {
          
        }

        [JsonRpcMethod("$/snyk.getFeatureFlagStatus")]
        public void OnSnykGetFeatureFlagStatus(JToken arg)
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
    }
}
