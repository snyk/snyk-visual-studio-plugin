// ABOUTME: Thread-safety test pinning the invariant that SnykOptionsManager.Load() reads the shared
// ABOUTME: snykSettings object while holding persistGate, so a concurrent Save() cannot mutate those
// ABOUTME: fields mid-read and yield a torn/mixed options object (PR #515 reviewer finding, IDE-2152).
// Uses a real SnykOptionsManager + real SnykSettingsLoader on a temp file, with an overridable
// read-region hook (protected virtual OnLoadReadRegionEntered) as a deterministic interleave seam
// instead of a flaky sleep-based race.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Moq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Settings
{
    public class LoadReadUnderPersistGateTests
    {
        // Build a fully-defaulted options mock so Save() never null-refs (Token/TrustedFolders/params).
        private static Mock<ISnykOptions> BuildOptions()
        {
            var optMock = new Mock<ISnykOptions>();
            optMock.SetupAllProperties();
            optMock.Object.OssEnabled = true;
            optMock.Object.SnykCodeSecurityEnabled = true;
            optMock.Object.IacEnabled = true;
            optMock.Object.SecretsEnabled = false;
            optMock.Object.AutoScan = true;
            optMock.Object.EnableDeltaFindings = false;
            optMock.Object.FilterCritical = true;
            optMock.Object.FilterHigh = true;
            optMock.Object.FilterMedium = true;
            optMock.Object.FilterLow = true;
            optMock.Object.OpenIssuesEnabled = true;
            optMock.Object.IgnoredIssuesEnabled = false;
            optMock.Object.IgnoreUnknownCA = false;
            optMock.Object.BinariesAutoUpdate = true;
            optMock.Object.CliCustomPath = string.Empty;
            optMock.Object.CliReleaseChannel = SnykCliDownloader.DefaultReleaseChannel;
            optMock.Object.CliBaseDownloadURL = SnykCliDownloader.DefaultBaseDownloadUrl;
            optMock.Object.AdditionalEnv = string.Empty;
            optMock.Object.AdditionalParameters = new List<string>();
            optMock.Object.TrustedFolders = new HashSet<string>();
            optMock.Object.DeviceId = "load-gate-device";
            optMock.Object.ApiToken = new AuthenticationToken(AuthenticationType.OAuth, string.Empty);
            return optMock;
        }

        // A manager that, on the first Load() read region, launches a concurrent Save() on a DEDICATED
        // thread (standing in for an LS-push Save on a StreamJsonRpc dispatch thread) and asserts the
        // writer is BLOCKED on persistGate while Load()'s read region is executing — using a DETERMINISTIC
        // state transition, not a wall-clock timeout.
        //
        // Why a dedicated Thread (not Task.Run) and why observe ThreadState: the decision must not depend
        // on how quickly the writer is scheduled or on how long File.WriteAllText takes. While this read
        // region holds persistGate, the writer's Save() will, in a CORRECT build, block on lock(persistGate)
        // and park in ThreadState.WaitSleepJoin; in a REGRESSION build (read region not under the gate) the
        // writer instead acquires the free lock and fires OnSaveCriticalSectionEntered, setting
        // saveEnteredCriticalSection. We spin until ONE of those two definite states is observed, so the
        // outcome is a true state transition rather than "did the seam fire within 200ms". An earlier
        // version waited a fixed 200ms on the event; on a thread-pool-starved runner a REGRESSION build
        // could keep the writer off-CPU for >200ms, the wait would return false, and the test would
        // false-green against the very torn-read it guards. The safety-net timeout below is deliberately
        // huge (10s) so it never decides pass/fail under any plausible scheduling delay — it only prevents
        // an infinite hang if something is genuinely broken.
        //
        // Thread-state key: production Save()'s first action after entry is lock(persistGate); the only
        // thing the writer does before that is the non-blocking writerThreadRunning.Set(). So the FIRST
        // time the writer thread enters WaitSleepJoin it is parked on the persistGate monitor — safe to
        // treat WaitSleepJoin as "correctly blocked".
        private sealed class ConcurrentSaveDuringLoadManager : SnykOptionsManager
        {
            private readonly Mock<ISnykOptions> writerOptions;
            private int hookFired; // guard: only interleave on the first read region

            // Set by the writer thread once it is scheduled and about to call Save().
            private readonly ManualResetEventSlim writerThreadRunning = new ManualResetEventSlim(false);
            // Set from Save()'s OnSaveCriticalSectionEntered seam — the deterministic signal that the
            // writer has acquired persistGate and entered Save's critical section.
            private readonly ManualResetEventSlim saveEnteredCriticalSection = new ManualResetEventSlim(false);
            private Thread writerThread;

            // Observed by the test after Load() returns: did the concurrent Save() enter its persistGate
            // critical section WHILE the Load() read region was still executing? True == invariant
            // violated (Load() did not hold the gate across its read).
            public bool WriterEnteredCriticalSectionDuringRead { get; private set; }

            public ConcurrentSaveDuringLoadManager(
                string settingsFilePath, ISnykServiceProvider serviceProvider, Mock<ISnykOptions> writerOptions)
                : base(settingsFilePath, serviceProvider)
            {
                this.writerOptions = writerOptions;
            }

            protected override void OnSaveCriticalSectionEntered()
            {
                // The writer has acquired persistGate and entered Save's critical section.
                this.saveEnteredCriticalSection.Set();
            }

            protected override void OnLoadReadRegionEntered()
            {
                // Only drive the interleave once (the very first read region). Later Load() calls, and
                // any re-entrant read, are left untouched.
                if (Interlocked.Exchange(ref this.hookFired, 1) != 0)
                    return;

                // Launch a writer that mutates the SAME snykSettings via a real Save(). Save is not
                // overridden for behaviour here, so this resolves to the base persistGate-guarded
                // implementation; its OnSaveCriticalSectionEntered override sets the handshake event.
                // A dedicated Thread (not Task.Run) so we can observe the writer's ThreadState.
                this.writerThread = new Thread(() =>
                {
                    this.writerThreadRunning.Set();
                    // System/LS-driven save: updateOverrideTracker:false, no settings-changed event.
                    this.Save(this.writerOptions.Object, triggerSettingsChangedEvent: false,
                        updateOverrideTracker: false);
                })
                {
                    IsBackground = true,
                    Name = "concurrent-save-during-load-writer",
                };
                this.writerThread.Start();

                // Ensure the writer thread is actually scheduled and about to (or trying to) Save().
                Assert.True(this.writerThreadRunning.Wait(TimeSpan.FromSeconds(5)),
                    "writer thread never started");

                // Deterministic decision by STATE TRANSITION (not a fixed timeout): spin until EITHER
                //   (a) the writer entered Save's critical section  -> event set  -> REGRESSION, or
                //   (b) the writer is parked acquiring the monitor  -> WaitSleepJoin -> correctly BLOCKED.
                // The safety net is far larger than any plausible scheduling delay, so it never decides
                // the outcome under normal or starved load; it only guards against a genuine hang.
                var safetyNet = TimeSpan.FromSeconds(10);
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed < safetyNet)
                {
                    if (this.saveEnteredCriticalSection.IsSet)
                    {
                        this.WriterEnteredCriticalSectionDuringRead = true;
                        return;
                    }

                    if ((this.writerThread.ThreadState & System.Threading.ThreadState.WaitSleepJoin) != 0)
                    {
                        // Writer is blocked on lock(persistGate) (see thread-state note above): the fix
                        // is holding and the writer cannot enter Save's critical section mid-read.
                        this.WriterEnteredCriticalSectionDuringRead = false;
                        return;
                    }

                    Thread.Sleep(1);
                }

                // Safety net hit without a definite transition (should not happen). Fall back to the
                // event's current state so we never claim "blocked" if the writer had in fact entered.
                this.WriterEnteredCriticalSectionDuringRead = this.saveEnteredCriticalSection.IsSet;
            }

            public void WaitForWriter()
            {
                this.writerThread?.Join(TimeSpan.FromSeconds(10));
            }
        }

        private static ISnykServiceProvider BuildServiceProvider(ISnykOptions options)
        {
            var spMock = new Mock<ISnykServiceProvider>();
            spMock.Setup(x => x.Options).Returns(options);
            return spMock.Object;
        }

        // GATE-001: A Save() running concurrently with Load()'s read of snykSettings must be serialized
        // by persistGate — it must NOT be able to mutate the shared object while Load() is mid-read.
        // Without the lock over the read region, Load() can return an options object that mixes fields
        // written before and after a concurrent Save() (the torn read PR #515 flagged).
        [Fact]
        public void Load_ReadOfSnykSettings_IsSerializedWithConcurrentSave()
        {
            var path = Path.GetTempFileName();
            ConcurrentSaveDuringLoadManager manager = null;
            try
            {
                var options = BuildOptions();
                var writerOptions = BuildOptions();
                var sp = BuildServiceProvider(options.Object);

                manager = new ConcurrentSaveDuringLoadManager(path, sp, writerOptions);

                // First Load(): enters the read region, which drives the concurrent Save() interleave.
                manager.Load();

                Assert.False(manager.WriterEnteredCriticalSectionDuringRead,
                    "A concurrent Save() entered its persistGate critical section while Load() was " +
                    "mid-read — Load()'s read region is not serialized under persistGate, so Load() can " +
                    "return a torn/mixed options object (PR #515 finding). Guard the read with persistGate.");
            }
            finally
            {
                // Always join the writer BEFORE deleting the temp file, even if the assert above threw.
                // The read region has exited and released persistGate, so the previously-blocked writer
                // completes its Save(); joining here proves the gate was released (not deadlocked) AND
                // guarantees the writer can no longer File.WriteAllText to `path` after File.Delete
                // (which would otherwise leak cross-test filesystem noise).
                manager?.WaitForWriter();
                File.Delete(path);
            }
        }
    }
}
