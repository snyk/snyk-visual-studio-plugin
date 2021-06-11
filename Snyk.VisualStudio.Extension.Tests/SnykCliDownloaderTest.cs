namespace Snyk.VisualStudio.Extension.Tests
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Snyk.VisualStudio.Extension.CLI;    

    [TestClass]
    public class SnykCliDownloaderTest
    {
        [TestMethod]
        public void SnykCliDownloader_CorrectInformationProvided_LatestReleaseInfoCorrect()
        {
            var cliDownloader = new SnykCliDownloader(new SnykMockActivityLogger());

            LatestReleaseInfo latestReleaseInfo = cliDownloader.GetLatestReleaseInfo();

            Assert.IsFalse(string.IsNullOrWhiteSpace(latestReleaseInfo.TagName));
        }

        [TestMethod]
        public void SnykCliDownloader_CorrectInformationProvided_DownloadSuccessful()
        {
            var cliDownloader = new SnykCliDownloader(new SnykMockActivityLogger());

            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.IsFalse(File.Exists(tempCliPath));

            cliDownloader.Download(tempCliPath);

            Assert.IsTrue(File.Exists(tempCliPath));

            File.Delete(tempCliPath);
        }

        [TestMethod]
        public void SnykCliDownloader_WrongNewVersionProvided_ChecksFail()
        {
            var cliDownloader = new SnykCliDownloader(new SnykMockActivityLogger());
            
            Assert.IsFalse(cliDownloader.IsNewVersionAvailable("1.342.1", ""));           
            Assert.IsFalse(cliDownloader.IsNewVersionAvailable("", "test"));
            Assert.IsFalse(cliDownloader.IsNewVersionAvailable("2.342.2", "1.342.1"));
            Assert.IsFalse(cliDownloader.IsNewVersionAvailable("1.345.2", "1.342.9"));
            Assert.IsFalse(cliDownloader.IsNewVersionAvailable("1.345.2", "1.345.1"));

            Assert.IsTrue(cliDownloader.IsNewVersionAvailable("", "1.342.1"));
            Assert.IsTrue(cliDownloader.IsNewVersionAvailable("1.342.2", "1.345.1"));
            Assert.IsTrue(cliDownloader.IsNewVersionAvailable("1.342.2", "2.345.1"));
            Assert.IsTrue(cliDownloader.IsNewVersionAvailable("1.345.2", "1.345.9"));            
        }

        [TestMethod]
        public void SnykCliDownloader_CorrectNewVersionProvided_ChecksPass()
        {
            var cliDownloader = new SnykCliDownloader(new SnykMockActivityLogger());            

            Assert.IsTrue(cliDownloader.IsNewVersionAvailable("", "1.342.1"));
            Assert.IsTrue(cliDownloader.IsNewVersionAvailable("1.342.2", "1.345.1"));
            Assert.IsTrue(cliDownloader.IsNewVersionAvailable("1.342.2", "2.345.1"));
            Assert.IsTrue(cliDownloader.IsNewVersionAvailable("1.345.2", "1.345.9"));
        }

        [TestMethod]
        public void SnykCliDownloader_CorrectLastCheckDatePassed_CheckPass()
        {
            var cliDownloader = new SnykCliDownloader(new SnykMockActivityLogger());

            Assert.IsTrue(cliDownloader.IsFourDaysPassedAfterLastCheck(DateTime.Now.AddDays(-5)));
        }

        [TestMethod]
        public void SnykCliDownloader_WrongLastCheckDatePassed_CheckFail()
        {
            var cliDownloader = new SnykCliDownloader(new SnykMockActivityLogger());

            Assert.IsFalse(cliDownloader.IsFourDaysPassedAfterLastCheck(DateTime.Now.AddDays(-3)));
        }

        [TestMethod]
        public void SnykCliDownloader_CliFileNotExists_CliDownloadSuccessful()
        {
            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.IsFalse(File.Exists(tempCliPath));

            var cliDownloader = new SnykCliDownloader(new SnykMockActivityLogger());
            
            cliDownloader.AutoUpdateCli(DateTime.Now.AddDays(-5), tempCliPath);

            string newCliVersion = cliDownloader.GetLatestReleaseInfo().CliVersion;

            Assert.IsTrue(File.Exists(tempCliPath));

            File.Delete(tempCliPath);
        }

        [TestMethod]
        public void SnykCliDownloader_WrongPreviousVersionProvided_CliDownloadSuccessful()
        {
            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.IsFalse(File.Exists(tempCliPath));

            var cliDownloader = new SnykCliDownloader(new SnykMockActivityLogger());

            cliDownloader.AutoUpdateCli(DateTime.Now.AddDays(-5), tempCliPath);

            string newCliVersion = cliDownloader.GetLatestReleaseInfo().CliVersion;

            Assert.IsTrue(File.Exists(tempCliPath));

            File.Delete(tempCliPath);

            Assert.IsFalse(string.IsNullOrEmpty(newCliVersion));
            Assert.IsTrue(cliDownloader.IsNewVersionAvailable("", newCliVersion));
        }

        [TestMethod]
        public void SnykCliDownloader_PreviousVersionOlderProvided_CliDownloadSuccessful()
        {
            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.IsFalse(File.Exists(tempCliPath));

            string currentCliVersion = "1.234.2";

            var cliDownloader = new SnykCliDownloader(currentCliVersion, new SnykMockActivityLogger());            

            cliDownloader.AutoUpdateCli(DateTime.Now.AddDays(-5), tempCliPath);

            string newCliVersion = cliDownloader.GetLatestReleaseInfo().CliVersion;

            Assert.IsTrue(File.Exists(tempCliPath));

            File.Delete(tempCliPath);

            Assert.IsTrue(cliDownloader.IsNewVersionAvailable(currentCliVersion, newCliVersion));
        }

        [TestMethod]
        public void SnykCliDownloader_PreviousVersionOlderAndFourDaysPassedProvided_CliDownloadSuccessful()
        {
            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.IsFalse(File.Exists(tempCliPath));

            string currentCliVersion = "1.234.2";

            var cliDownloader = new SnykCliDownloader(currentCliVersion, new SnykMockActivityLogger());
            
            cliDownloader.AutoUpdateCli(DateTime.Now.AddDays(-5), tempCliPath);

            string newCliVersion = cliDownloader.GetLatestReleaseInfo().CliVersion;

            Assert.IsTrue(File.Exists(tempCliPath));

            File.Delete(tempCliPath);

            Assert.IsTrue(cliDownloader.IsNewVersionAvailable(currentCliVersion, newCliVersion));            
        }        
    }
}
