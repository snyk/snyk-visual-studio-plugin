using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Snyk.VisualStudio.Extension;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    /// <summary>
    /// Unit tests for the testable activation-seam extracted from
    /// SnykVSPackage.LanguageClientManagerOnLanguageClientNotInitializedAsync (IDE-1752).
    ///
    /// The seam consists of two internal members on SnykVSPackage:
    ///   1. DecideNotInitializedActivation(bool isLanguageServerReady)
    ///      → ActivationDecision enum (Activate | NoOp)
    ///   2. InvokeNotInitializedActivationAsync(Func&lt;Task&gt; openTempFileAction)
    ///      → invokes the action, catches any exception, and logs a diagnostic (no rethrow/loop).
    /// </summary>
    [Collection(MockedVS.Collection)]
    public class SnykVSPackageNotInitializedHandlerTests : PackageBaseTest
    {
        public SnykVSPackageNotInitializedHandlerTests(GlobalServiceProvider sp) : base(sp) { }

        // ------------------------------------------------------------------ //
        //  Reflection helpers                                                  //
        // ------------------------------------------------------------------ //

        private ActivationDecision InvokeDecideActivation(bool isReady)
        {
            var method = typeof(SnykVSPackage).GetMethod(
                "DecideNotInitializedActivation",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (method == null)
                throw new InvalidOperationException(
                    "DecideNotInitializedActivation not found on SnykVSPackage. " +
                    "Ensure the testable seam was extracted per IDE-1752 plan step 2.2.");

            return (ActivationDecision)method.Invoke(VsPackage, new object[] { isReady });
        }

        private Task InvokeActivationWrapperAsync(Func<Task> action)
        {
            var method = typeof(SnykVSPackage).GetMethod(
                "InvokeNotInitializedActivationAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (method == null)
                throw new InvalidOperationException(
                    "InvokeNotInitializedActivationAsync not found on SnykVSPackage. " +
                    "Ensure the testable seam was extracted per IDE-1752 plan step 2.2.");

            return (Task)method.Invoke(VsPackage, new object[] { action });
        }

        // ------------------------------------------------------------------ //
        //  Decision tests                                                       //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// LS not ready → activation must be triggered (the IDE-1752 fix: solution state no
        /// longer gates this path).
        ///
        /// Static RED reasoning: before the fix, <c>DecideNotInitializedActivation</c> did not
        /// exist; the handler entered a Task.Delay dead-loop for the no-solution case. Reflection
        /// finds no method → <c>InvalidOperationException</c> → test fails.
        /// Static GREEN reasoning: after the fix the method exists and returns <c>Activate</c>
        /// for any LS-not-ready input, regardless of solution state.
        /// </summary>
        [Fact]
        public void LsNotReady_DecideActivation_ReturnsActivate()
        {
            Assert.Equal(ActivationDecision.Activate, InvokeDecideActivation(isReady: false));
        }

        /// <summary>
        /// LS already ready → no activation (NoOp). Should be green before and after the fix.
        /// </summary>
        [Fact]
        public void LsReady_DecideActivation_ReturnsNoOp()
        {
            Assert.Equal(ActivationDecision.NoOp, InvokeDecideActivation(isReady: true));
        }

        // ------------------------------------------------------------------ //
        //  Activation-wrapper tests                                             //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// When the injected action succeeds, it is called exactly once and the wrapper completes
        /// without throwing.
        ///
        /// Static RED reasoning: before the fix, <c>InvokeNotInitializedActivationAsync</c> did
        /// not exist; reflection fails → test fails.
        /// Static GREEN reasoning: after the fix the wrapper calls the action once and returns.
        /// </summary>
        [Fact]
        public async Task ActivationWrapper_ActionSucceeds_CalledOnce()
        {
            var callCount = 0;
            Func<Task> spyAction = () =>
            {
                callCount++;
                return Task.CompletedTask;
            };

            await InvokeActivationWrapperAsync(spyAction);

            Assert.Equal(1, callCount);
        }

        /// <summary>
        /// When the injected action throws (e.g. DTE unavailable), the wrapper must:
        ///   - call the action exactly once (no retry / infinite loop)
        ///   - not rethrow (the exception must not escape)
        ///
        /// Logger assertion is intentionally absent: <c>LogManager.ForContext&lt;T&gt;()</c>
        /// writes to a <c>Lazy&lt;Logger&gt;</c> file sink, not <c>Serilog.Log.Logger</c>, so
        /// the mock installed in PackageBaseTest cannot intercept it (see brain note
        /// log-manager-not-mockable).
        ///
        /// Static RED reasoning: before the fix the seam does not exist; reflection fails.
        /// Static GREEN reasoning: after the fix the wrapper catches, logs once (unverifiable
        /// here), and returns; callCount == 1 and no exception escapes.
        /// </summary>
        [Fact]
        public async Task ActivationWrapper_ActionThrows_CalledOnce_NoExceptionEscapes()
        {
            var callCount = 0;
            Func<Task> throwingAction = async () =>
            {
                callCount++;
                await Task.CompletedTask;
                throw new InvalidOperationException("Simulated DTE unavailable (IDE-1752 test)");
            };

            // Must not throw
            await InvokeActivationWrapperAsync(throwingAction);

            Assert.Equal(1, callCount);
        }
    }
}
