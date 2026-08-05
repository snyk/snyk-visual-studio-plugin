using System.IO;
using Moq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    /// <summary>
    /// Test case for <see cref="SnykOptionsManagerTest"/>.
    /// </summary>
    public class SnykOptionsManagerTest
    {
        private SnykOptionsManager cut;
        private readonly string settingsFilePath;
        private readonly Mock<ISnykServiceProvider> serviceProviderMock;

        public SnykOptionsManagerTest()
        {
            this.settingsFilePath = Path.GetTempFileName();
            this.serviceProviderMock = new Mock<ISnykServiceProvider>();
            cut = new SnykOptionsManager(settingsFilePath, serviceProviderMock.Object);
            var optionsMock = new Mock<ISnykOptions>();
            optionsMock.Setup(x => x.InvokeSettingsChangedEvent());
            serviceProviderMock.Setup(x => x.Options).Returns(optionsMock.Object);
        }

        [Fact]
        public void LoadAndSaveGeneralOptions_PersistsChanges()
        {
            // Load default
            var options = cut.Load();
            // Set some values
            options.AutoScan = true;
            options.IgnoreUnknownCA = true;
            options.Organization = "my-org";
            options.CustomEndpoint = "https://custom.endpoint";
            options.AuthenticationMethod = AuthenticationType.OAuth;
            options.ApiToken = new AuthenticationToken(AuthenticationType.OAuth, "dummy-token");
            options.BinariesAutoUpdate = true;
            options.CliCustomPath = "C:\\cli\\snyk.exe";
            options.CliBaseDownloadURL = "https://cli.download.url";
            options.CliReleaseChannel = "stable";
            options.CurrentCliVersion = "1.2.3";
            options.IacEnabled = true;
            options.SnykCodeSecurityEnabled = true;
            options.OssEnabled = true;

            cut.Save(options);

            // Reload to confirm persistence
            var reloadedOptions = cut.Load();

            Assert.True(reloadedOptions.AutoScan);
            Assert.True(reloadedOptions.IgnoreUnknownCA);
            Assert.Equal("my-org", reloadedOptions.Organization);
            Assert.Equal("https://custom.endpoint", reloadedOptions.CustomEndpoint);
            Assert.Equal(AuthenticationType.OAuth, reloadedOptions.AuthenticationMethod);
            Assert.Equal("dummy-token", reloadedOptions.ApiToken.ToString());
            Assert.True(reloadedOptions.BinariesAutoUpdate);
            Assert.Equal("C:\\cli\\snyk.exe", reloadedOptions.CliCustomPath);
            Assert.Equal("https://cli.download.url", reloadedOptions.CliBaseDownloadURL);
            Assert.Equal("stable", reloadedOptions.CliReleaseChannel);
            Assert.Equal("1.2.3", reloadedOptions.CurrentCliVersion);
            Assert.True(reloadedOptions.IacEnabled);
            Assert.True(reloadedOptions.SnykCodeSecurityEnabled);
            Assert.True(reloadedOptions.OssEnabled);
        }

        [Fact]
        public void Load_KeepsEmptyCliDownloadSettingsRaw_AndStillComposesAWorkingUrl()
        {
            // Reproduces a settings.json written after a $/snyk.configuration echo landed the LS's
            // empty binary_base_url / cli_release_channel defaults in options. The empties are loaded
            // as-is, so the settings field still shows its placeholder rather than the literal default
            // and Save does not persist the literal. What must never happen is a broken composed URL
            // ("/cli//ls-protocol-version-NN"); resolution at the point of use is what prevents that,
            // which is why the raw empties are safe to keep.
            File.WriteAllText(this.settingsFilePath,
                @"{""cliReleaseChannel"":"""",""cliBaseDownloadURL"":"""",""binariesAutoUpdateEnabled"":true}");
            var manager = new SnykOptionsManager(this.settingsFilePath, this.serviceProviderMock.Object);

            var options = manager.Load();

            Assert.Equal(string.Empty, options.CliBaseDownloadURL);
            Assert.Equal(string.Empty, options.CliReleaseChannel);

            Assert.Equal(
                SnykCliDownloader.DefaultBaseDownloadUrl + "/cli/" + SnykCliDownloader.DefaultReleaseChannel
                    + "/ls-protocol-version-" + LsConstants.ProtocolVersion,
                new SnykCliDownloader(options).BuildLatestReleaseVersionUrl());
        }

        [Fact]
        public void Load_KeepsConfiguredCliDownloadSettings()
        {
            File.WriteAllText(this.settingsFilePath,
                @"{""cliReleaseChannel"":""preview"",""cliBaseDownloadURL"":""https://downloads.snyk.io/fips""}");
            var manager = new SnykOptionsManager(this.settingsFilePath, this.serviceProviderMock.Object);

            var options = manager.Load();

            Assert.Equal("https://downloads.snyk.io/fips", options.CliBaseDownloadURL);
            Assert.Equal("preview", options.CliReleaseChannel);
        }
    }
}
