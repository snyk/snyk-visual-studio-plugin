// ABOUTME: One-time migration helper for IDE-1483: moves settings.json from the old
// VSIX install-directory location to the stable per-user AppData location.
// Pure file I/O, no VS dependencies, fully unit-testable.
using System;
using System.IO;

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
        // FINDING-A: no static ILogger field here.  LogManager.ForContext() depends on
        // SnykDirectory.GetSnykAppDataDirectoryPath() (via the Lazy<Logger> in LogManager).
        // A static readonly field initialised at class-load time risks entering that Lazy
        // while it is still constructing (re-entrancy) and causing a startup crash.
        // Instead, call LogManager.ForContext() inline at each log site — identical to the
        // fix applied to SnykDirectory.cs for the same latent issue.

        /// <summary>
        /// Returns the byte-length of <paramref name="path"/>:
        /// <list type="bullet">
        ///   <item>≥ 0 — file exists and was stat-able (0 means genuinely zero-byte).</item>
        ///   <item>-1 — file exists but its length could not be determined due to a transient
        ///              lock or permissions problem; the caller must NOT overwrite it because
        ///              it may hold valid data that is simply unreadable right now.</item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// FINDING-B: the original implementation caught all exceptions and returned -1,
        /// collapsing "file not found" (absent) and "file locked/unreadable" into the same
        /// sentinel.  This led to the caller treating a file that vanished between
        /// <see cref="File.Exists"/> and this call as "unreadable" (do not overwrite),
        /// silently skipping migration and leaving the auth token stranded at the old path.
        ///
        /// The fix: <see cref="FileNotFoundException"/> and <see cref="DirectoryNotFoundException"/>
        /// are caught separately and return 0 (absent), so the caller falls through to the
        /// correct "zero-byte / absent — safe to repair" branch rather than the "unreadable —
        /// do not clobber" guard.
        /// </remarks>
        private static long TryGetFileLength(string path)
        {
            try
            {
                return new FileInfo(path).Length;
            }
            catch (FileNotFoundException)
            {
                // File was deleted between File.Exists and this call (TOCTOU).
                // Treat as absent (0), not as unreadable (-1).
                return 0;
            }
            catch (DirectoryNotFoundException)
            {
                // Parent directory vanished — treat as absent (0).
                return 0;
            }
            catch (Exception)
            {
                // Genuine unreadable (locked by AV, permission denied, etc.).
                // Return -1 so the caller does NOT overwrite potentially-valid content.
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
                                LogManager.ForContext(typeof(SettingsLocationMigrator)).Warning(
                                    deleteEx,
                                    "Could not delete stranded old settings file '{OldPath}' — it may still contain credentials.",
                                    oldPath);
                            }
                        }
                        return;
                    }

                    if (newLen == 0)
                    {
                        // newPath is CONFIRMED zero-byte (empty) or was found absent after
                        // File.Exists returned true (TOCTOU — see TryGetFileLength remarks).
                        // Repair from oldPath only when oldPath holds valid content.
                        if (oldPathExists && TryGetFileLength(oldPath) > 0)
                        {
                            File.Copy(oldPath, newPath, overwrite: true);

                            LogManager.ForContext(typeof(SettingsLocationMigrator)).Information(
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
                                LogManager.ForContext(typeof(SettingsLocationMigrator)).Warning(
                                    deleteEx,
                                    "Could not delete old settings file '{OldPath}' after repair — it may still contain credentials.",
                                    oldPath);
                            }
                        }
                        else
                        {
                            // oldPath absent, zero-byte, or unreadable — nothing valid to recover.
                            LogManager.ForContext(typeof(SettingsLocationMigrator)).Warning(
                                "New settings file '{NewPath}' is empty and old settings file '{OldPath}' is absent or holds no valid content — nothing to recover.",
                                newPath,
                                oldPath);
                        }
                        return;
                    }

                    // newLen == -1: newPath exists but its size cannot be determined due to a
                    // transient lock or permissions edge case.  The file may contain valid
                    // settings we cannot read right now — do NOT overwrite it, do NOT delete oldPath.
                    LogManager.ForContext(typeof(SettingsLocationMigrator)).Warning(
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

                    LogManager.ForContext(typeof(SettingsLocationMigrator)).Information(
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
                        LogManager.ForContext(typeof(SettingsLocationMigrator)).Warning(
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
                //   - Empty/absent newPath → migration did not complete; Warning level.
                // Use TryGetFileLength (exception-safe) instead of File.Exists to avoid a
                // secondary IOException on network/UNC-redirected %LocalAppData% paths — that
                // would escape the catch block and violate the never-throws contract.
                if (TryGetFileLength(newPath) > 0)
                {
                    LogManager.ForContext(typeof(SettingsLocationMigrator)).Information(
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
                        LogManager.ForContext(typeof(SettingsLocationMigrator)).Warning(
                            deleteEx,
                            "Could not delete stranded old settings file '{OldPath}' after concurrent migration — it may still contain credentials.",
                            oldPath);
                    }
                }
                else
                {
                    LogManager.ForContext(typeof(SettingsLocationMigrator)).Warning(
                        ex,
                        "Migration/repair of settings from '{OldPath}' did not complete — '{NewPath}' is absent or empty. User may need to re-authenticate.",
                        oldPath,
                        newPath);
                }
            }
        }
    }
}
