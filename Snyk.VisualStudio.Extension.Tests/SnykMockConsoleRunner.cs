using Segment.Model;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Tests
{
    using System.Collections.Specialized;
    using Snyk.VisualStudio.Extension.CLI;

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