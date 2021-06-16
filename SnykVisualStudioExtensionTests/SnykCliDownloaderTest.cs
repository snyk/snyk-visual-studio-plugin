using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snyk.VisualStudio.Extension.CLI;
using System.IO;

namespace Snyk.VisualStudio.Extension.Tests
{
    [TestClass]
    public class SnykCliDownloaderTest
    {
        [TestMethod]
        public void GetLatestReleaseInfo()
        {
            var cliDownloader = new SnykCliDownloader(new SnykMockActivityLogger());

            LatestReleaseInfo latestReleaseInfo = cliDownloader.GetLatestReleaseInfo();

            Assert.IsFalse(string.IsNullOrWhiteSpace(latestReleaseInfo.TagName));
        }

        [TestMethod]
        public void Download()
        {
            var cliDownloader = new SnykCliDownloader(new SnykMockActivityLogger());

            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.IsFalse(File.Exists(tempCliPath));

            cliDownloader.Download(tempCliPath);

            Assert.IsTrue(File.Exists(tempCliPath));

            File.Delete(tempCliPath);
        }
    }
}
