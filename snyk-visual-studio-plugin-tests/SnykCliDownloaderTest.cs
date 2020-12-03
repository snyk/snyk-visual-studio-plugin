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
            var cliDownloader = new SnykCliDownloader();

            LatestReleaseInfo latestReleaseInfo = cliDownloader.GetLatestReleaseInfo(new SnykWebClient());
            
            Assert.IsFalse(string.IsNullOrWhiteSpace(latestReleaseInfo.TagName));
        }

        [TestMethod]
        public void Download()
        {
            var cliDownloader = new SnykCliDownloader();

            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.IsFalse(File.Exists(tempCliPath));

            cliDownloader.Download(tempCliPath);

            Assert.IsTrue(File.Exists(tempCliPath));

            File.Delete(tempCliPath);
        }
    }
}
