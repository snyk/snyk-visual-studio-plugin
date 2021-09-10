namespace Snyk.VisualStudio.Extension.Tests
{
    using System;
    using System.IO;
    using Snyk.VisualStudio.Extension.CLI;
    using Xunit;

    public class SnykCliDownloaderTest
    {
        [Fact]
        public void SnykCliDownloader_CorrectInformationProvided_LatestReleaseInfoCorrect()
        {
            var cliDownloader = new SnykCliDownloader(null);

            LatestReleaseInfo latestReleaseInfo = cliDownloader.GetLatestReleaseInfo();

            Assert.False(string.IsNullOrWhiteSpace(latestReleaseInfo.TagName));
        }

        [Fact]
        public void SnykCliDownloader_CorrectInformationProvided_DownloadSuccessful()
        {
            var cliDownloader = new SnykCliDownloader(null);

            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            cliDownloader.Download(tempCliPath);

            Assert.True(File.Exists(tempCliPath));

            File.Delete(tempCliPath);
        }

        [Fact]
        public void SnykCliDownloader_WrongNewVersionProvided_ChecksFail()
        {
            var cliDownloader = new SnykCliDownloader(null);

            Assert.False(cliDownloader.IsNewVersionAvailable("1.342.1", string.Empty));
            Assert.False(cliDownloader.IsNewVersionAvailable(string.Empty, "test"));
            Assert.False(cliDownloader.IsNewVersionAvailable("2.342.2", "1.342.1"));
            Assert.False(cliDownloader.IsNewVersionAvailable("1.345.2", "1.342.9"));
            Assert.False(cliDownloader.IsNewVersionAvailable("1.345.2", "1.345.1"));

            Assert.True(cliDownloader.IsNewVersionAvailable(string.Empty, "1.342.1"));
            Assert.True(cliDownloader.IsNewVersionAvailable("1.342.2", "1.345.1"));
            Assert.True(cliDownloader.IsNewVersionAvailable("1.342.2", "2.345.1"));
            Assert.True(cliDownloader.IsNewVersionAvailable("1.345.2", "1.345.9"));
        }

        [Fact]
        public void SnykCliDownloader_CorrectNewVersionProvided_ChecksPass()
        {
            var cliDownloader = new SnykCliDownloader(null);

            Assert.True(cliDownloader.IsNewVersionAvailable(string.Empty, "1.342.1"));
            Assert.True(cliDownloader.IsNewVersionAvailable("1.342.2", "1.345.1"));
            Assert.True(cliDownloader.IsNewVersionAvailable("1.342.2", "2.345.1"));
            Assert.True(cliDownloader.IsNewVersionAvailable("1.345.2", "1.345.9"));
        }

        [Fact]
        public void SnykCliDownloader_CorrectLastCheckDatePassed_CheckPass()
        {
            var cliDownloader = new SnykCliDownloader(null);

            Assert.True(cliDownloader.IsFourDaysPassedAfterLastCheck(DateTime.Now.AddDays(-5)));
        }

        [Fact]
        public void SnykCliDownloader_WrongLastCheckDatePassed_CheckFail()
        {
            var cliDownloader = new SnykCliDownloader(null);

            Assert.False(cliDownloader.IsFourDaysPassedAfterLastCheck(DateTime.Now.AddDays(-3)));
        }

        [Fact]
        public void SnykCliDownloader_CliFileNotExists_CliDownloadSuccessful()
        {
            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            var cliDownloader = new SnykCliDownloader(null);

            cliDownloader.AutoUpdateCli(DateTime.Now.AddDays(-5), tempCliPath);

            string newCliVersion = cliDownloader.GetLatestReleaseInfo().CliVersion;

            Assert.True(File.Exists(tempCliPath));

            File.Delete(tempCliPath);
        }

        [Fact]
        public void SnykCliDownloader_WrongPreviousVersionProvided_CliDownloadSuccessful()
        {
            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            var cliDownloader = new SnykCliDownloader(null);

            cliDownloader.AutoUpdateCli(DateTime.Now.AddDays(-5), tempCliPath);

            string newCliVersion = cliDownloader.GetLatestReleaseInfo().CliVersion;

            Assert.True(File.Exists(tempCliPath));

            File.Delete(tempCliPath);

            Assert.False(string.IsNullOrEmpty(newCliVersion));
            Assert.True(cliDownloader.IsNewVersionAvailable(string.Empty, newCliVersion));
        }

        [Fact]
        public void SnykCliDownloader_PreviousVersionOlderProvided_CliDownloadSuccessful()
        {
            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            string currentCliVersion = "1.234.2";

            var cliDownloader = new SnykCliDownloader(currentCliVersion, null);

            cliDownloader.AutoUpdateCli(DateTime.Now.AddDays(-5), tempCliPath);

            string newCliVersion = cliDownloader.GetLatestReleaseInfo().CliVersion;

            Assert.True(File.Exists(tempCliPath));

            File.Delete(tempCliPath);

            Assert.True(cliDownloader.IsNewVersionAvailable(currentCliVersion, newCliVersion));
        }

        [Fact]
        public void SnykCliDownloader_PreviousVersionOlderAndFourDaysPassedProvided_CliDownloadSuccessful()
        {
            string tempCliPath = Path.Combine(Path.GetTempPath(), SnykCli.CliFileName);

            File.Delete(tempCliPath);

            Assert.False(File.Exists(tempCliPath));

            string currentCliVersion = "1.234.2";

            var cliDownloader = new SnykCliDownloader(currentCliVersion, null);

            cliDownloader.AutoUpdateCli(DateTime.Now.AddDays(-5), tempCliPath);

            string newCliVersion = cliDownloader.GetLatestReleaseInfo().CliVersion;

            Assert.True(File.Exists(tempCliPath));

            File.Delete(tempCliPath);

            Assert.True(cliDownloader.IsNewVersionAvailable(currentCliVersion, newCliVersion));
        }
    }
}
