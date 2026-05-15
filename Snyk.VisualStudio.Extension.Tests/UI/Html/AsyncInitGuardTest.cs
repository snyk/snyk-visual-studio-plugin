using System;
using System.Threading.Tasks;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    /// <summary>
    /// Behavioural tests for the single-flight init gate that backs <see cref="WebView2Host.InitializeAsync"/>.
    /// The gate prevents the host from being permanently poisoned by a failed first init —
    /// a follow-up call must be allowed to retry rather than silently returning against a
    /// faulted Ready task.
    /// </summary>
    public class AsyncInitGuardTest
    {
        [Fact]
        public async Task RunOnceAsync_FailedInit_AllowsRetry()
        {
            var guard = new AsyncInitGuard();
            var attempts = 0;

            Func<Task> failing = () => throw new InvalidOperationException("first");
            Func<Task> failingAgain = () => throw new InvalidOperationException("second");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                guard.RunOnceAsync(() => { attempts++; return failing(); }));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                guard.RunOnceAsync(() => { attempts++; return failingAgain(); }));

            Assert.Equal(2, attempts);
        }

        [Fact]
        public async Task RunOnceAsync_SuccessfulInit_IsNotReinvoked()
        {
            var guard = new AsyncInitGuard();
            var attempts = 0;

            await guard.RunOnceAsync(() => { attempts++; return Task.CompletedTask; });
            await guard.RunOnceAsync(() => { attempts++; return Task.CompletedTask; });

            Assert.Equal(1, attempts);
        }

        [Fact]
        public async Task Ready_FaultsAfterFailedInit_AndSwapsToFreshTaskOnRetry()
        {
            var guard = new AsyncInitGuard();
            var preFailureReady = guard.Ready;

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                guard.RunOnceAsync(() => throw new InvalidOperationException("boom")));

            // The Ready task captured before failure must surface the init exception.
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await preFailureReady);

            // A fresh Ready task is in place for the retry — not the faulted one.
            var postFailureReady = guard.Ready;
            Assert.NotSame(preFailureReady, postFailureReady);

            await guard.RunOnceAsync(() => Task.CompletedTask);
            await postFailureReady;
        }
    }
}
