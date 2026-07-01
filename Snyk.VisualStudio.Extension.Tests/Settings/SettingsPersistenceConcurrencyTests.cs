// ABOUTME: Concurrency tests for settings.json persistence (IDE-2152 fix #4).
// ABOUTME: Drives a UI-thread Save concurrently with the background CommitPendingResets on the
// ABOUTME: SAME real temp-file-backed manager, proving the mutate+serialize+write region is
// ABOUTME: mutually exclusive: no throw, no torn/corrupt file, converged deserializable state.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.Utils;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Settings
{
    public class SettingsPersistenceConcurrencyTests
    {
        // Build a real SnykOptionsManager backed by a temp file (mirrors UserOverridePersistenceTests).
        private static (SnykOptionsManager manager, Mock<ISnykOptions> options, string path)
            BuildManager(string existingPath = null)
        {
            var path = existingPath ?? Path.GetTempFileName();
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
            optMock.Object.DeviceId = "concurrency-device";
            optMock.Object.ApiToken = new AuthenticationToken(AuthenticationType.OAuth, string.Empty);

            var spMock = new Mock<ISnykServiceProvider>();
            spMock.Setup(x => x.Options).Returns(optMock.Object);

            var manager = new SnykOptionsManager(path, spMock.Object);
            return (manager, optMock, path);
        }

        // CONCUR-001 (fix #4): Two persisting paths hitting the SAME manager on different threads must
        // not throw and must not corrupt settings.json. In production the concurrent pair is a UI-thread
        // user Save (updateOverrideTracker:true) racing an LS-push Save on a StreamJsonRpc background
        // dispatch thread (SnykLanguageClientCustomTarget.OnSnykConfiguration/OnHasAuthenticated/
        // OnAddTrustedFolders). This test drives Save concurrently with CommitPendingResets — an
        // equivalent second writer that mutates+serializes+writes the same snykSettings — to stress the
        // shared critical section directly. (The DidChangeConfiguration reset-commit itself is now
        // marshaled to the UI thread by IDE-2152 fix #7, so it is no longer a background writer; the gate
        // remains load-bearing for the LS-push background Saves above.) Without the gate, both paths
        // mutate the shared snykSettings object AND call an unsynchronized File.WriteAllText on the same
        // path, so under load this throws (IOException / "collection was modified" during serialization)
        // or leaves a torn, non-deserializable file.
        //
        // The write region (mutate snykSettings + serialize + write) must be a single critical section
        // shared by all persisting callers. After the storm the file must deserialize and reflect a
        // converged state: OssEnabled=false was persisted (user override), and the reset for
        // Organization was committed (drained from the pending queue).
        [Fact]
        public void ConcurrentSaveAndCommit_DoesNotThrowOrCorruptSettingsFile()
        {
            var (manager, optMock, path) = BuildManager();
            try
            {
                manager.Load(); // seed the tracker

                // Pre-enqueue a pending reset so the background CommitPendingResets has something to
                // drain and persist on every iteration.
                manager.Save(optMock.Object, triggerSettingsChangedEvent: false,
                    resetKeys: new List<string> { PflagKeys.Organization });
                Assert.Contains(PflagKeys.Organization, manager.OverrideTracker.PeekPendingResets());

                // The user override we expect to survive the storm.
                optMock.Object.OssEnabled = false;

                const int iterations = 500;
                var exceptions = new System.Collections.Concurrent.ConcurrentQueue<System.Exception>();
                var barrier = new Barrier(2);

                // UI-thread role: repeated user Save with an edit-delta (marks snyk_oss_enabled),
                // mutating snykSettings + persisting on each call.
                var saver = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    try
                    {
                        for (var i = 0; i < iterations; i++)
                            manager.Save(optMock.Object, triggerSettingsChangedEvent: false,
                                editedKeys: new List<string> { PflagKeys.SnykOssEnabled });
                    }
                    catch (System.Exception ex) { exceptions.Enqueue(ex); }
                });

                // Background writer role: repeated CommitPendingResets on a separate thread stands in
                // for any concurrent background persister (e.g. an LS-push Save on a StreamJsonRpc
                // dispatch thread). It mutates+serializes+writes the same snykSettings, stressing the
                // shared critical section from a non-UI thread.
                // Re-enqueue each round so there is always something to commit + persist.
                var committer = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    try
                    {
                        for (var i = 0; i < iterations; i++)
                        {
                            manager.OverrideTracker.ApplyUserResets(new List<string> { PflagKeys.Organization });
                            manager.CommitPendingResets(new List<string> { PflagKeys.Organization });
                        }
                    }
                    catch (System.Exception ex) { exceptions.Enqueue(ex); }
                });

                Task.WaitAll(saver, committer);

                Assert.True(exceptions.IsEmpty,
                    "Concurrent Save + CommitPendingResets must not throw — the mutate+serialize+write " +
                    "region must be one critical section. First exception: " +
                    (exceptions.TryPeek(out var first) ? first.ToString() : "none"));

                // The on-disk file must be intact and deserializable (not torn by two writers).
                var rawJson = File.ReadAllText(path);
                var settings = Json.Deserialize<SnykSettings>(rawJson);
                Assert.NotNull(settings);

                // Converged state: the user's OssEnabled override was recorded.
                Assert.NotNull(settings.ChangedConfigKeys);
                Assert.Contains(PflagKeys.SnykOssEnabled, settings.ChangedConfigKeys);

                // A fresh manager over the same file must load without error (final proof of no torn write).
                var (manager2, _, _) = BuildManager(path);
                var loaded = manager2.Load();
                Assert.NotNull(loaded);
                Assert.Contains(PflagKeys.SnykOssEnabled, loaded.ChangedConfigKeys);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
