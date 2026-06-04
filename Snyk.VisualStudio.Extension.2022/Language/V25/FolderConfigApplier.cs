using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Snyk.VisualStudio.Extension.Language
{
    // Converts inbound LspFolderConfig (pflag-keyed Settings dict) into typed FolderConfig
    // and applies it to the in-memory Options.FolderConfigs list.
    // Overwrite-by-path semantics: LS payload is authoritative and complete.
    // No pruning — VS is single-solution; stale entries cause less harm than a missing entry.
    internal static class FolderConfigApplier
    {
        private static readonly ILogger Logger = LogManager.ForContext(typeof(FolderConfigApplier));

        public static List<FolderConfig> Apply(List<FolderConfig> existing, List<LspFolderConfig> incoming)
        {
            if (incoming == null || incoming.Count == 0)
                return existing ?? new List<FolderConfig>();

            var result = existing != null ? new List<FolderConfig>(existing) : new List<FolderConfig>();

            foreach (var inc in incoming)
            {
                if (inc == null || string.IsNullOrEmpty(inc.FolderPath))
                    continue;

                var typed = ToFolderConfig(inc);
                var idx = result.FindIndex(fc => PathsMatch(fc.FolderPath, inc.FolderPath));
                if (idx >= 0)
                    result[idx] = typed;
                else
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

        private static bool PathsMatch(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return false;
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
