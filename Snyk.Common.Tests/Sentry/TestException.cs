namespace Snyk.Common.Tests.Sentry
{
    using System;

    /// <summary>
    /// Exception for tests with stack trace.
    /// </summary>
    public class TestException : Exception
    {
        private string testStackTrace;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestException"/> class.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="stackTrace">Custom stack trace.</param>
        public TestException(string message, string stackTrace) : base(message) => this.testStackTrace = stackTrace;


        /// <summary>
        /// Override to return custom stack trace.
        /// </summary>
        public override string StackTrace => this.testStackTrace;
    }
}
