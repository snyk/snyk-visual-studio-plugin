using System;
using System.IO;
using System.Text;
using Snyk.VisualStudio.Extension.Utils;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Load and save Snyk settings.
    /// </summary>
    public class SnykSettingsLoader
    {
        // No static Logger field: LogManager.ForContext() depends on SnykDirectory (via the
        // Lazy<Logger> in LogManager). A static readonly field initialised at class-load time
        // risks re-entrancy if this class is loaded during that Lazy's construction.
        // Acquire the logger inline at each call site — identical to SnykDirectory and
        // SettingsLocationMigrator.

        private readonly string settingsFilePath;

        /// <summary>
        /// The settings file path this loader reads/writes. Exposed so the owning
        /// <see cref="SnykOptionsManager"/> can back up a present-but-unreadable file to a sidecar
        /// before the first overwrite (IDE-1483 × IDE-2152 FIX-D2).
        /// </summary>
        public string SettingsFilePath => this.settingsFilePath;

        private SnykSettings snykSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykSettingsLoader"/> class.
        /// </summary>
        /// <param name="settingsPath">File path to settings.</param>
        public SnykSettingsLoader(string settingsPath)
        {
            this.settingsFilePath = settingsPath;
        }

        /// <summary>
        /// Load <see cref="SnykSettings"/> instance.
        /// Delegates to <see cref="Load(out bool)"/>; use that overload when the caller
        /// needs to distinguish a genuinely absent file from a present-but-unreadable one.
        /// </summary>
        /// <returns>SnykSettings object, or null when the file is absent or unreadable.</returns>
        public SnykSettings Load()
        {
            return Load(out _);
        }

        /// <summary>
        /// Load <see cref="SnykSettings"/> instance using a single read attempt so the
        /// absent-vs-corrupt decision is derived from ONE filesystem operation (no TOCTOU
        /// window between a probe and the read — IDE-1483 FIX-D1).
        /// </summary>
        /// <param name="fileWasAbsent">
        /// Set to <c>true</c> when the file was genuinely not present
        /// (<see cref="FileNotFoundException"/> / <see cref="DirectoryNotFoundException"/>).
        /// Set to <c>false</c> when the file exists (even if it could not be read or
        /// deserialised).  The caller uses this flag to decide whether writing defaults
        /// is safe (absent = safe; present-but-unreadable = do NOT overwrite).
        /// </param>
        /// <returns>SnykSettings object, or null when the file is absent or unreadable.</returns>
        public SnykSettings Load(out bool fileWasAbsent)
        {
            fileWasAbsent = false;

            if (snykSettings != null)
                return snykSettings;

            string rawJson;
            try
            {
                // Single read: determines absent-vs-exists AND provides the content.
                // FileNotFoundException / DirectoryNotFoundException => genuinely absent.
                // Any other exception => file exists but is unreadable (IO error, locked, etc.).
                rawJson = File.ReadAllText(this.settingsFilePath, Encoding.UTF8);
            }
            catch (FileNotFoundException)
            {
                fileWasAbsent = true;
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                fileWasAbsent = true;
                return null;
            }
            catch (Exception e)
            {
                LogManager.ForContext(typeof(SnykSettingsLoader)).Error(e, "Settings read error on load.");
                return null;
            }

            // An empty or whitespace-only file is not corrupt — File.ReadAllText returns "" (no throw)
            // and Json.Deserialize("") returns null (no throw), which would otherwise be misclassified
            // as present-but-unreadable (fileWasAbsent stays false) and never repaired, leaving a
            // useless empty .bak on the first Save. Treat it as ABSENT so the caller's fresh-install
            // path writes defaults and self-heals. A genuinely truncated file with partial JSON still
            // throws on deserialize below and remains correctly classified as corrupt/preserved.
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                fileWasAbsent = true;
                return null;
            }

            try
            {
                // Visibility for support: the legacy per-solution store (solutionSettingsDict) was
                // retired in IDE-1651. Its entries are now migrated into folder configs lazily, as
                // each solution is opened (see SnykOptionsManager.MigrateLegacySolutionSettings); the
                // section shrinks as solutions are reopened. Log when an upgrading user's settings.json
                // still carries a non-empty legacy section so support can correlate.
                WarnIfLegacySolutionSettingsPresent(rawJson);

                snykSettings = Json.Deserialize<SnykSettings>(rawJson);
                return snykSettings;
            }
            catch (Exception e)
            {
                LogManager.ForContext(typeof(SnykSettingsLoader)).Error(e, "Settings deserialize error on load.");
                return null;
            }
        }

        private static void WarnIfLegacySolutionSettingsPresent(string rawJson)
        {
            try
            {
                var token = Newtonsoft.Json.Linq.JObject.Parse(rawJson)["solutionSettingsDict"]
                    as Newtonsoft.Json.Linq.JObject;
                if (token != null && token.Count > 0)
                {
                    LogManager.ForContext(typeof(SnykSettingsLoader)).Information(
                        "settings.json contains a legacy 'solutionSettingsDict' with {Count} entr(ies). " +
                        "These are migrated into folder configs as each solution is opened, and the section " +
                        "is removed once empty.",
                        token.Count);
                }
            }
            catch (Exception e)
            {
                // Best-effort diagnostics only — never block load on a malformed legacy section.
                LogManager.ForContext(typeof(SnykSettingsLoader)).Debug(e, "Could not inspect settings.json for a legacy solutionSettingsDict section.");
            }
        }

        /// <summary>
        /// Save <see cref="SnykSettings"/> to file.
        /// </summary>
        /// <param name="settings">Updated settings.</param>
        public void Save(SnykSettings settings)
        {
            try
            {
                File.WriteAllText(this.settingsFilePath, Json.Serialize(settings), Encoding.UTF8);
            }
            catch (Exception e)
            {
                LogManager.ForContext(typeof(SnykSettingsLoader)).Error(e, "Settings serialize error on save.");
            }
        }
    }
}