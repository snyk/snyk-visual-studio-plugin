// ABOUTME: Per-user-data-folder cache of async WebView2 environment creation, extracted from WebView2Host.
// ABOUTME: Generic + factory-injected so the share/evict behaviour is unit-testable without the WebView2 runtime.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Caches the in-flight <see cref="Task{T}"/> that creates a WebView2 environment per user-data
    /// folder, so multiple controls sharing a folder join the same creation rather than racing on the
    /// exclusive folder lock (WebView2 requires one environment per shared folder). Faulted/canceled
    /// entries are dropped on next access so a transient init failure doesn't permanently poison the
    /// slot, and <see cref="Evict"/> drops a slot when the last host using a folder is disposed.
    /// <para>
    /// Generic over the environment type and constructed with the creation factory so the caching and
    /// eviction logic can be exercised without the WebView2 runtime (<see cref="WebView2Host"/> uses
    /// <c>WebView2EnvironmentCache&lt;CoreWebView2Environment&gt;</c>).
    /// </para>
    /// </summary>
    public sealed class WebView2EnvironmentCache<T>
    {
        private readonly object gate = new object();
        private readonly Dictionary<string, Task<T>> cache =
            new Dictionary<string, Task<T>>(StringComparer.OrdinalIgnoreCase);
        private readonly Func<string, Task<T>> factory;

        public WebView2EnvironmentCache(Func<string, Task<T>> factory)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Returns the cached creation task for <paramref name="userDataFolder"/>, or starts (and
        /// caches) a new one. A cached entry that has faulted or been canceled is discarded and
        /// recreated so a transient failure doesn't permanently poison the slot.
        /// </summary>
        public Task<T> GetOrCreate(string userDataFolder)
        {
            lock (this.gate)
            {
                if (this.cache.TryGetValue(userDataFolder, out var existing))
                {
                    if (!existing.IsFaulted && !existing.IsCanceled)
                    {
                        return existing;
                    }

                    this.cache.Remove(userDataFolder);
                }

                var task = this.factory(userDataFolder);
                this.cache[userDataFolder] = task;
                return task;
            }
        }

        /// <summary>Drops the cached entry for the folder so the next <see cref="GetOrCreate"/> recreates it.</summary>
        public void Evict(string userDataFolder)
        {
            if (string.IsNullOrEmpty(userDataFolder)) return;
            lock (this.gate)
            {
                this.cache.Remove(userDataFolder);
            }
        }
    }
}
