using System;
using System.Collections.Generic;

namespace Snyk.VisualStudio.Extension.Language
{
    // Converts inbound LspFolderConfig into the in-memory FolderConfig and applies it to
    // Options.FolderConfigs.
    // The LS is the source of truth: it sends the complete folder-config set for the current
    // workspace, keyed by the paths it registered. The folder settings are an opaque pflag-keyed
    // map that we round-trip verbatim (matching vscode/eclipse), so this is a straight copy — no
    // per-field switch. We replace the stored list wholesale so stale entries (e.g. from a
    // previously-opened solution in the same VS session) don't linger and can't be selected by
    // mistake. The list is left unchanged only when the payload carries no folder configs (e.g. a
    // global-only configuration update), so a partial notification doesn't wipe folder state.
    internal static class FolderConfigApplier
    {
        public static List<FolderConfig> Apply(List<FolderConfig> existing, List<LspFolderConfig> incoming)
        {
            if (incoming == null || incoming.Count == 0)
                return existing ?? new List<FolderConfig>();

            var result = new List<FolderConfig>(incoming.Count);

            foreach (var inc in incoming)
            {
                if (inc == null || string.IsNullOrEmpty(inc.FolderPath))
                    continue;

                result.Add(ToFolderConfig(inc));
            }

            return result;
        }

        public static FolderConfig ToFolderConfig(LspFolderConfig src)
        {
            return new FolderConfig
            {
                FolderPath = src.FolderPath,
                // Copy the map so later IDE-side edits don't mutate the inbound payload, and so an
                // absent Settings map normalizes to empty rather than null.
                Settings = src.Settings != null
                    ? new Dictionary<string, ConfigSetting>(src.Settings, StringComparer.Ordinal)
                    : new Dictionary<string, ConfigSetting>(),
            };
        }
    }
}
