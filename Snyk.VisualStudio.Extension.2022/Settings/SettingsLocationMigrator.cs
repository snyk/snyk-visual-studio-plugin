// ABOUTME: One-time migration helper for IDE-1483: moves settings.json from the old
// VSIX install-directory location to the stable per-user AppData location.
// Pure file I/O, no VS dependencies, fully unit-testable.
using System;
using System.IO;
using Serilog;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Performs a one-time best-effort migration of <c>settings.json</c> from the old
    /// VSIX install-directory location to the stable per-user <c>%LocalAppData%\Snyk</c>
    /// location introduced in IDE-1483.
    /// </summary>
    /// <remarks>
    /// Rules:
    /// <list type="bullet">
    ///   <item>When <paramref name="newPath"/> already exists AND is non-empty, the new location
    ///         is authoritative.  If <paramref name="oldPath"/> is also present it is best-effort
    ///         deleted so the plaintext auth token is not left stranded in the world-readable
    ///         VSIX install directory.</item>
    ///   <item>When <paramref name="newPath"/> exists but is empty or zero-byte (e.g. created by
    ///         an AV scanner or a prior crashed VS write), it is treated as not-yet-migrated.
    ///         If <paramref name="oldPath"/> holds valid settings it is copied into
    ///         <paramref name="newPath"/> (overwrite: true) to repair it, then
    ///         <paramref name="oldPath"/> is best-effort deleted.  This prevents
    ///         SnykSettingsLoader from silently overwriting an empty newPath with defaults and
    ///         losing the auth token on the next launch.</item>
    ///   <item>No-op when <paramref name="oldPath"/> does not exist (fresh install, nothing to migrate).</item>
    ///   <item>Copies first; only deletes the old file after a successful copy (best-effort — a delete failure is logged but does not throw).</item>
    ///   <item>Catches and logs any I/O exception — never throws.</item>
    /// </list>
    /// </remarks>
    public static class SettingsLocationMigrator
    {
        private static readonly ILogger Logger = LogManager.ForContext(typeof(SettingsLocationMigrator));

        /// <summary>
        /// Returns the byte-length of <paramref name="path"/>, or -1 if the file cannot be
        /// stat-ed (e.g. a transient AV lock or TOCTOU race between <see cref="File.Exists"/>
        /// and this call).  A -1 result means the file's content is unknown and it must NOT
        /// be overwritten — it may contain valid data we simply cannot read right now.
        /// </summary>
        private static long TryGetFileLength(string path)
        {
            try
            {
                return new FileInfo(path).Length;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// Migrates <paramref name="oldPath"/> to <paramref name="newPath"/> exactly once.
        /// </summary>
        /// <param name="oldPath">Full path to the settings file at the old install-directory location.</param>
        /// <param name="newPath">Full path to the settings file at the stable AppData location.</param>
        public static void MigrateIfNeeded(string oldPath, string newPath)
        {
            try
            {
                if (File.Exists(newPath))
                {
                    long newLen = TryGetFileLength(newPath);
                    bool oldPathExists = File.Exists(oldPath);

                    if (newLen > 0)
                    {
                        // newPath is authoritative — just clean up a stranded oldPath if present.
                        if (oldPathExists)
                        {
                            try
                            {
                                File.Delete(oldPath);
                            }
                            catch (Exception deleteEx)
                            {
                                Logger.Warning(
                                    deleteEx,
                                    "Could not delete stranded old settings file '{OldPath}' — it may still contain credentials.",
                                    oldPath);
                            }
                        }
                        return;
                    }

                    if (newLen == 0)
                    {
                        // newPath is CONFIRMED zero-byte (empty).
                        // Repair from oldPath only when oldPath holds valid content.
                        if (oldPathExists && TryGetFileLength(oldPath) > 0)
                        {
                            File.Copy(oldPath, newPath, overwrite: true);

                            Logger.Information(
                                "Repaired empty new settings file '{NewPath}' from old settings file '{OldPath}'.",
                                newPath,
                                oldPath);

                            // Best-effort delete of old file after successful repair.
                            try
                            {
                                File.Delete(oldPath);
                            }
                            catch (Exception deleteEx)
                            {
                                Logger.Warning(
                                    deleteEx,
                                    "Could not delete old settings file '{OldPath}' after repair — it may still contain credentials.",
                                    oldPath);
                            }
                        }
                        else
                        {
                            // oldPath absent, zero-byte, or unreadable — nothing valid to recover.
                            Logger.Warning(
                                "New settings file '{NewPath}' is empty and old settings file '{OldPath}' is absent or holds no valid content — nothing to recover.",
                                newPath,
                                oldPath);
                        }
                        return;
                    }

                    // newLen == -1: newPath exists but its size cannot be determined (e.g. a
                    // transient AV lock or a permissions edge case).  The file may contain valid
                    // settings we cannot read — do NOT overwrite it and do NOT delete oldPath.
                    Logger.Warning(
                        "Settings file '{NewPath}' could not be assessed (size unreadable) — skipping migration to avoid clobbering potentially valid settings.",
                        newPath);
                    return;
                }
                else
                {
                    // newPath absent — standard first-time migration.
                    if (!File.Exists(oldPath))
                        return;

                    File.Copy(oldPath, newPath, overwrite: false);

                    Logger.Information(
                        "Migrated settings from old install-dir location '{OldPath}' to stable AppData location '{NewPath}'.",
                        oldPath,
                        newPath);

                    // Best-effort delete of the old file so the plaintext auth token is not
                    // left in the VSIX install directory, which may be world-readable on
                    // machine-wide VS installations.  A delete failure never loses data
                    // (the copy already succeeded) and must not throw.
                    try
                    {
                        File.Delete(oldPath);
                    }
                    catch (Exception deleteEx)
                    {
                        Logger.Warning(
                            deleteEx,
                            "Could not delete old settings file '{OldPath}' after migration — it may still contain credentials.",
                            oldPath);
                    }
                }
            }
            catch (Exception ex)
            {
                // Best-effort: log and continue — never block startup.
                // The dominant benign trigger is a TOCTOU race: a second VS window already
                // created newPath between our File.Exists check and the File.Copy call, so
                // File.Copy(overwrite:false) throws IOException even though migration succeeded.
                //
                // Distinguish log level by whether newPath ended up with valid content:
                //   - Non-empty newPath → likely concurrent migration; Information level + clean up oldPath.
                //   - Empty/unreadable newPath → migration did not complete; Warning level.
                // Use TryGetFileLength (exception-safe) instead of File.Exists to avoid a
                // secondary IOException on network/UNC-redirected %LocalAppData% paths — that
                // would escape the catch block and violate the never-throws contract.
                if (TryGetFileLength(newPath) > 0)
                {
                    Logger.Information(
                        ex,
                        "Settings already present at new path '{NewPath}' — likely a concurrent migration, no action needed.",
                        newPath);

                    // Credential hygiene: also remove the stranded old file from the install dir
                    // now that newPath is confirmed non-empty.
                    try
                    {
                        File.Delete(oldPath);
                    }
                    catch (Exception deleteEx)
                    {
                        Logger.Warning(
                            deleteEx,
                            "Could not delete stranded old settings file '{OldPath}' after concurrent migration — it may still contain credentials.",
                            oldPath);
                    }
                }
                else
                {
                    Logger.Warning(
                        ex,
                        "Migration/repair of settings from '{OldPath}' did not complete — '{NewPath}' is absent or empty. User may need to re-authenticate.",
                        oldPath,
                        newPath);
                }
            }
        }
    }
}
