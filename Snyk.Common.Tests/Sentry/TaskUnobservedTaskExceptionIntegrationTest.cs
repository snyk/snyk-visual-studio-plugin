namespace Snyk.Common.Tests.Sentry
{
    using System;
    using System.Collections.Generic;
    using Snyk.Common.Sentry;
    using Xunit;

    /// <summary>
    /// Test for <see cref="TaskUnobservedTaskExceptionIntegration"/>
    /// </summary>
    public class TaskUnobservedTaskExceptionIntegrationTest
    {
        [Fact]
        public void TaskUnobservedTaskExceptionIntegration_ProvidedExceptionWithoutSnyk_IsNeedToHandleExceptionReturnFalse()
            => Assert.False(TaskUnobservedTaskExceptionIntegration.IsNeedToHandleException(new Exception()));

        [Fact]
        public void TaskUnobservedTaskExceptionIntegration_ProvidedExceptionWithSnyk_IsNeedToHandleExceptionReturnTrue()
            => Assert.True(TaskUnobservedTaskExceptionIntegration.IsNeedToHandleException(new TestException("Snyk", "Snyk.Common.Tests.Sentry")));

        [Fact]
        public void TaskUnobservedTaskExceptionIntegration_ProvidedAggregateExceptionWithoutSnyk_IsNeedToHandleExceptionReturnFalse()
        {
            var exceptions = new List<Exception>();
            exceptions.Add(new Exception());
            exceptions.Add(new Exception());

            Assert.False(TaskUnobservedTaskExceptionIntegration.IsNeedToHandleException(new AggregateException("Errors.", exceptions)));
        }

        [Fact]
        public void TaskUnobservedTaskExceptionIntegration_ProvidedAggregateExceptionWithSnyk_IsNeedToHandleExceptionReturnTrue()
        {
            var exceptions = new List<Exception>();
            exceptions.Add(new Exception());
            exceptions.Add(new TestException("Snyk", "Snyk.Common.Tests.Sentry"));

            Assert.True(TaskUnobservedTaskExceptionIntegration.IsNeedToHandleException(new AggregateException("Errors.", exceptions)));
        }
    }
}
