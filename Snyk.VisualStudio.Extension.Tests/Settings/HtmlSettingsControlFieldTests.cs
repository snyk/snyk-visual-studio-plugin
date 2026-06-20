// ABOUTME: Structural / reflection tests for HtmlSettingsControl field declarations.
// ABOUTME: These tests document invariants that CI cannot otherwise enforce (e.g. volatile modifier).
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Settings
{
    public class HtmlSettingsControlFieldTests
    {
        /// <summary>
        /// _pageReady is read on the background LS thread (via RequestReload → null guard) and
        /// written on the UI thread (page-navigation callback). The volatile modifier prevents
        /// the JIT from caching the stale value in a register. This test uses the same
        /// reflection technique as the CLR spec: a volatile field carries
        /// System.Runtime.CompilerServices.IsVolatile as a required custom modifier on its type.
        /// </summary>
        [Fact]
        public void _pageReady_IsVolatile_MatchingInstanceFieldPattern()
        {
            var field = typeof(HtmlSettingsControl).GetField(
                "_pageReady",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(field); // field must exist

            // A volatile field carries IsVolatile as a required custom modifier.
            var requiredMods = field.GetRequiredCustomModifiers();
            Assert.Contains(typeof(IsVolatile), requiredMods);
        }

        /// <summary>
        /// The static instance field was already volatile before this change. Verify it still is,
        /// as a baseline that the reflection technique works correctly.
        /// </summary>
        [Fact]
        public void instance_StaticField_IsVolatile_Baseline()
        {
            var field = typeof(HtmlSettingsControl).GetField(
                "instance",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.NotNull(field); // field must exist

            var requiredMods = field.GetRequiredCustomModifiers();
            Assert.Contains(typeof(IsVolatile), requiredMods);
        }
    }
}
