using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Language
{
    public class LsConfiguration
    {
        private readonly ISnykServiceProvider serviceProvider;

        public LsConfiguration(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public SnykLsInitializationOptions GetInitializationOptions()
        {
            if (this.serviceProvider == null)
            {
                return null;
            }
            var options = this.serviceProvider.Options;
            var initializationOptions = new SnykLsInitializationOptions
            {
                ActivateSnykCode = options.SnykCodeSecurityEnabled.ToString(),
                ActivateSnykCodeSecurity = options.SnykCodeSecurityEnabled.ToString(),
                ActivateSnykCodeQuality = options.SnykCodeQualityEnabled.ToString(),
                ActivateSnykOpenSource = options.OssEnabled.ToString(),
                ActivateSnykIac = options.IacEnabled.ToString(),
                SendErrorReports = "true",
                ManageBinariesAutomatically = options.BinariesAutoUpdate.ToString(),
                EnableTrustedFoldersFeature = "false",
                TrustedFolders = options.TrustedFolders.ToList(),
                IntegrationName = this.GetIntegrationName(options),
                IntegrationVersion = this.GetIntegrationVersion(options),
                FilterSeverity = new FilterSeverityOptions
                {
                    Critical = false,
                    High = false,
                    Low = false,
                    Medium = false,
                },
                ScanningMode = options.AutoScan ? "auto" : "manual",
#pragma warning disable VSTHRD104
                AdditionalParams = ThreadHelper.JoinableTaskFactory.Run(() => options.GetAdditionalOptionsAsync()),
#pragma warning restore VSTHRD104
                AuthenticationMethod = options.AuthenticationMethod == AuthenticationType.OAuth ? "oauth" : "token",
                CliPath = SnykCli.GetCliFilePath(options.CliCustomPath),
                Organization = options.Organization,
                Token = options.ApiToken.ToString(),
                AutomaticAuthentication = "false",
                Endpoint = options.CustomEndpoint,
                Insecure = options.IgnoreUnknownCA.ToString(),
                RequiredProtocolVersion = LsConstants.ProtocolVersion,
                HoverVerbosity = 1,
                OutputFormat = "plain",
                DeviceId = options.DeviceId,
                EnableDeltaFindings = options.EnableDeltaFindings.ToString(),
                FolderConfigs = options.FolderConfigs
            };
            return initializationOptions;
        }

        private string GetIntegrationName(ISnykOptions options)
        {
            var compositeValue = $"{options.IntegrationEnvironment}@@{options.IntegrationName}";
            return compositeValue;
        }
        private string GetIntegrationVersion(ISnykOptions options)
        {
            var compositeValue = $"{options.IntegrationEnvironmentVersion}@@{options.IntegrationVersion}";
            return compositeValue;
        }
    }
}
