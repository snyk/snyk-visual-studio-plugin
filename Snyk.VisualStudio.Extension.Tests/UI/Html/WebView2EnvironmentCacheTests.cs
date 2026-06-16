using System;
using System.Threading.Tasks;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    public class WebView2EnvironmentCacheTests
    {
        [Fact]
        public void GetOrCreate_SameKey_SharesOneInFlightTask()
        {
            var calls = 0;
            var tcs = new TaskCompletionSource<string>();
            var cache = new WebView2EnvironmentCache<string>(_ => { calls++; return tcs.Task; });

            var first = cache.GetOrCreate("folder");
            var second = cache.GetOrCreate("folder");

            Assert.Same(first, second);   // concurrent callers join the same in-flight creation
            Assert.Equal(1, calls);       // factory invoked once
        }

        [Fact]
        public void GetOrCreate_DifferentKeys_CreateSeparateTasks()
        {
            var calls = 0;
            var cache = new WebView2EnvironmentCache<string>(_ => { calls++; return Task.FromResult("env"); });

            cache.GetOrCreate("a");
            cache.GetOrCreate("b");

            Assert.Equal(2, calls);
        }

        [Fact]
        public void GetOrCreate_FaultedEntry_IsRecreatedOnNextAccess()
        {
            var calls = 0;
            var cache = new WebView2EnvironmentCache<string>(_ =>
            {
                calls++;
                return calls == 1
                    ? Task.FromException<string>(new InvalidOperationException("boom"))
                    : Task.FromResult("env");
            });

            var faulted = cache.GetOrCreate("folder");
            Assert.True(faulted.IsFaulted);
            Assert.NotNull(faulted.Exception); // observe so it isn't an unobserved-exception

            var retry = cache.GetOrCreate("folder");

            Assert.NotSame(faulted, retry); // poisoned entry dropped and recreated
            Assert.Equal(2, calls);
        }

        [Fact]
        public void Evict_ThenGetOrCreate_CreatesAFreshTask()
        {
            var calls = 0;
            var cache = new WebView2EnvironmentCache<string>(_ => { calls++; return Task.FromResult("env"); });

            var first = cache.GetOrCreate("folder");
            cache.Evict("folder");
            var second = cache.GetOrCreate("folder");

            Assert.NotSame(first, second);
            Assert.Equal(2, calls);
        }

        [Fact]
        public void Evict_NullOrEmptyKey_IsNoOp()
        {
            var cache = new WebView2EnvironmentCache<string>(_ => Task.FromResult("env"));

            var ex = Record.Exception(() => { cache.Evict(null); cache.Evict(string.Empty); });

            Assert.Null(ex);
        }
    }
}
