// In-process coverage for the wiring around ExtensionAssemblyResolver.ResolveCandidatePath:
// Initialize()'s idempotency guard and OnAssemblyResolve's own try/catch. No real VS host is
// needed — both are exercised directly via InternalsVisibleTo.
using System;
using System.Reflection;
using System.Reflection.Emit;
using Snyk.VisualStudio.Extension;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class ExtensionAssemblyResolverWiringTests
    {
        [Fact]
        public void Initialize_CalledMultipleTimes_RegisteredGuardStaysSet()
        {
            ExtensionAssemblyResolver.Initialize();
            ExtensionAssemblyResolver.Initialize();
            ExtensionAssemblyResolver.Initialize();

            var registeredField = typeof(ExtensionAssemblyResolver)
                .GetField("registered", BindingFlags.NonPublic | BindingFlags.Static);

            // The AssemblyResolve subscription in Initialize() only runs inside the
            // Interlocked.CompareExchange guard, so pinning the guard's value pins the
            // subscribe-at-most-once guarantee it implements.
            Assert.Equal(1, (int)registeredField.GetValue(null));
        }

        [Fact]
        public void OnAssemblyResolve_UnknownAssembly_ReturnsNullWithoutThrowing()
        {
            var args = new ResolveEventArgs("Snyk.Definitely.Not.A.Real.Assembly, Version=1.0.0.0");

            Assembly result = null;
            var ex = Record.Exception(() => result = ExtensionAssemblyResolver.OnAssemblyResolve(null, args));

            Assert.Null(ex);
            Assert.Null(result);
        }

        [Fact]
        public void OnAssemblyResolve_RequestingAssemblyLocationUnavailable_DeclinesWithoutThrowing()
        {
            // A dynamic, run-only assembly throws NotSupportedException on .Location — the
            // "requesting assembly exists but we can't place it" case from item 1, exercised
            // through the full handler rather than just ResolveCandidatePath.
            var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("Snyk.Tests.DynamicProbe"), AssemblyBuilderAccess.Run);

            var args = new ResolveEventArgs("Serilog, Version=2.10.0.0, Culture=neutral, PublicKeyToken=24c2f752a8e58a10", dynamicAssembly);

            Assembly result = null;
            var ex = Record.Exception(() => result = ExtensionAssemblyResolver.OnAssemblyResolve(null, args));

            Assert.Null(ex);
            Assert.Null(result);
        }
    }
}
