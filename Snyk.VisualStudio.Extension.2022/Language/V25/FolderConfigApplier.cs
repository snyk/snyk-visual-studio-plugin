using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Snyk.VisualStudio.Extension.Language
{
    // Converts inbound LspFolderConfig (pflag-keyed Settings dict) into typed FolderConfig
    // and applies it to the in-memory Options.FolderConfigs list.
    // The LS is the source of truth: it sends the complete folder-config set for the current
    // workspace, keyed by the paths it registered. We replace the stored list wholesale so stale
    // entries (e.g. from a previously-opened solution in the same VS session) don't linger and
    // can't be selected by mistake. The list is left unchanged only when the payload carries no
    // folder configs (e.g. a global-only configuration update), so a partial notification doesn't
    // wipe folder state.
    internal static class FolderConfigApplier
    {
        private static readonly ILogger Logger = LogManager.ForContext(typeof(FolderConfigApplier));

        public static List<FolderConfig> Apply(List<FolderConfig> existing, List<LspFolderConfig> incoming)
        {
            if (incoming == null || incoming.Count == 0)
                return existing ?? new List<FolderConfig>();

            var result = new List<FolderConfig>(incoming.Count);

            foreach (var inc in incoming)
            {
                if (inc == null || string.IsNullOrEmpty(inc.FolderPath))
                    continue;

                var typed = ToFolderConfig(inc);

                // Carry over extension-local state the LS neither sends nor persists — specifically
                // ReferenceFolderPath (set via the Branch Selector, used for delta findings). The
                // typed config is built fresh from the LS payload, so without this every config
                // push would permanently wipe it.
                var prior = existing?.FirstOrDefault(fc =>
                    fc != null && string.Equals(fc.FolderPath, inc.FolderPath, StringComparison.OrdinalIgnoreCase));
                if (prior != null)
                    typed.ReferenceFolderPath = prior.ReferenceFolderPath;

                result.Add(typed);
            }

            return result;
        }

        public static FolderConfig ToFolderConfig(LspFolderConfig src)
        {
            var fc = new FolderConfig { FolderPath = src.FolderPath };

            if (src.Settings == null)
                return fc;

            foreach (var kvp in src.Settings)
            {
                if (kvp.Value?.Value == null)
                    continue;

                try
                {
                    var val = kvp.Value.Value is JToken jt ? jt : JToken.FromObject(kvp.Value.Value);

                    switch (kvp.Key)
                    {
                        case PflagKeys.AdditionalParameters:
                            fc.AdditionalParameters = val.ToObject<List<string>>();
                            break;
                        case PflagKeys.AdditionalEnvironment:
                            fc.AdditionalEnv = val.Value<string>();
                            break;
                        case PflagKeys.PreferredOrg:
                            fc.PreferredOrg = val.Value<string>();
                            break;
                        case PflagKeys.AutoDeterminedOrg:
                            fc.AutoDeterminedOrg = val.Value<string>();
                            break;
                        case PflagKeys.OrgSetByUser:
                            fc.OrgSetByUser = val.Value<bool>();
                            break;
                        case PflagKeys.BaseBranch:
                            fc.BaseBranch = val.Value<string>();
                            break;
                        case PflagKeys.LocalBranches:
                            fc.LocalBranches = val.ToObject<List<string>>();
                            break;
                        case PflagKeys.ScanCommandConfig:
                            fc.ScanCommandConfig = val.ToObject<Dictionary<string, ScanCommandConfig>>();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "FolderConfigApplier: failed to apply key '{Key}' for path '{Path}', skipping", kvp.Key, src.FolderPath);
                }
            }

            return fc;
        }
    }
}
