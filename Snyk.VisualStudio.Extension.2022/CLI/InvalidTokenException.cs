namespace Snyk.VisualStudio.Extension.CLI
{
    using System;

    /// <summary>
    /// Invalid Snyk api token guid exception.
    /// </summary>
    public class InvalidTokenException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidTokenException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public InvalidTokenException(string message)
            : base(message)
        {
        }
    }
}
