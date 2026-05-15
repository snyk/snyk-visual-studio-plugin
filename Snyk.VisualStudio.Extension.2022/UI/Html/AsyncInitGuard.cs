using System;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Single-flight async init gate. <see cref="RunOnceAsync"/> executes its action at most
    /// once on success; a failed attempt resets the gate and the <see cref="Ready"/> task so a
    /// subsequent caller can retry rather than silently early-returning against a faulted
    /// completion source. Designed for UI-thread callers — no locking.
    /// </summary>
    internal sealed class AsyncInitGuard
    {
        private bool _started;
        private TaskCompletionSource<bool> _readyTcs =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Ready => _readyTcs.Task;

        public async Task RunOnceAsync(Func<Task> initAction)
        {
            if (initAction == null) throw new ArgumentNullException(nameof(initAction));
            if (_started) return;
            _started = true;

            var currentTcs = _readyTcs;
            try
            {
                await initAction().ConfigureAwait(true);
                currentTcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                currentTcs.TrySetException(ex);
                _readyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _started = false;
                throw;
            }
        }
    }
}
