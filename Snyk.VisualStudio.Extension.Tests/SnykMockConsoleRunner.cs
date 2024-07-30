using Snyk.Common.Settings;
using System.Collections.Specialized;
using Snyk.VisualStudio.Extension.CLI;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class SnykMockConsoleRunner : SnykConsoleRunner
    {
        private string consoleResult;

        public SnykMockConsoleRunner(ISnykOptions options, string result) : base(options)
        {
            this.consoleResult = result;
        }

        public override string Run(string fileName, string arguments, StringDictionary environmentVariables = null)
        {
            return this.consoleResult;
        }

        public override string Execute()
        {
            return this.consoleResult;
        }
    }
}