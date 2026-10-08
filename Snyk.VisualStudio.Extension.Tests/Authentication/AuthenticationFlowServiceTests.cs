using System.IO;
using Moq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Authentication
{
    public class AuthenticationFlowServiceTests
    {
        [Fact]
        public void Authenticate_Throws_AndDoesNotTouchDialog_WhenCliNotFound()
        {
            var options = new Mock<ISnykOptions>();
            options.SetupGet(o => o.CliCustomPath).Returns(@"Z:\nonexistent\snyk-cli-does-not-exist.exe");

            var serviceProvider = new Mock<ISnykServiceProvider>();
            serviceProvider.SetupGet(p => p.Options).Returns(options.Object);

            var dialog = new Mock<IAuthDialog>();
            var cut = new AuthenticationFlowService(serviceProvider.Object, dialog.Object);

            // The CLI presence check runs before the auth flow starts and throws when the CLI is
            // missing — so the modal dialog is never armed or shown.
            Assert.Throws<FileNotFoundException>(() => cut.Authenticate());

            dialog.Verify(d => d.ArmForShow(), Times.Never);
            dialog.Verify(d => d.ShowDialogForAuth(), Times.Never);
            dialog.Verify(d => d.HideForAuthResult(), Times.Never);
        }

        private const string ExpiredOAuth = "{\"access_token\":\"a\",\"token_type\":\"Bearer\",\"refresh_token\":\"r\",\"expiry\":\"2000-01-01T00:00:00Z\"}";
        private const string ValidOAuth = "{\"access_token\":\"b\",\"token_type\":\"Bearer\",\"refresh_token\":\"r2\",\"expiry\":\"2099-01-01T00:00:00Z\"}";

        private static (AuthenticationFlowService cut, Mock<ISnykOptions> options) SetupWithFileToken(AuthenticationToken onDisk, string inMemory)
        {
            var options = new Mock<ISnykOptions>();
            options.SetupAllProperties();
            options.Object.AuthenticationMethod = AuthenticationType.OAuth;
            options.Object.ApiToken = new AuthenticationToken(AuthenticationType.OAuth, inMemory);

            var optionsManager = new Mock<ISnykOptionsManager>();
            optionsManager.Setup(m => m.ReadTokenFromFile()).Returns(onDisk);

            var serviceProvider = new Mock<ISnykServiceProvider>();
            serviceProvider.SetupGet(p => p.Options).Returns(options.Object);
            serviceProvider.SetupGet(p => p.SnykOptionsManager).Returns(optionsManager.Object);

            return (new AuthenticationFlowService(serviceProvider.Object, new Mock<IAuthDialog>().Object), options);
        }

        [Fact]
        public void TryAdoptTokenFromFile_ValidTokenOnDisk_ReplacesExpiredInMemoryToken()
        {
            var (cut, options) = SetupWithFileToken(new AuthenticationToken(AuthenticationType.OAuth, ValidOAuth), ExpiredOAuth);

            Assert.True(cut.TryAdoptTokenFromFile(options.Object));
            Assert.Equal(ValidOAuth, options.Object.ApiToken.ToString());
            Assert.Equal(AuthenticationType.OAuth, options.Object.AuthenticationMethod);
        }

        [Fact]
        public void TryAdoptTokenFromFile_ExpiredTokenOnDisk_KeepsInMemoryToken()
        {
            var (cut, options) = SetupWithFileToken(new AuthenticationToken(AuthenticationType.OAuth, ExpiredOAuth), ExpiredOAuth);

            Assert.False(cut.TryAdoptTokenFromFile(options.Object));
            Assert.Equal(ExpiredOAuth, options.Object.ApiToken.ToString());
        }

        [Fact]
        public void TryAdoptTokenFromFile_UnreadableFile_ReturnsFalse()
        {
            var (cut, options) = SetupWithFileToken(null, ExpiredOAuth);

            Assert.False(cut.TryAdoptTokenFromFile(options.Object));
        }

        [Fact]
        public void TryAdoptTokenFromFile_SameTokenOnDisk_ReturnsFalse()
        {
            var (cut, options) = SetupWithFileToken(new AuthenticationToken(AuthenticationType.OAuth, ValidOAuth), ValidOAuth);

            Assert.False(cut.TryAdoptTokenFromFile(options.Object));
        }

        [Fact]
        public void Authenticate_ReleasesReentrancyGuard_AfterEachAttempt()
        {
            var options = new Mock<ISnykOptions>();
            options.SetupGet(o => o.CliCustomPath).Returns(@"Z:\nonexistent\snyk-cli-does-not-exist.exe");

            var serviceProvider = new Mock<ISnykServiceProvider>();
            serviceProvider.SetupGet(p => p.Options).Returns(options.Object);

            var cut = new AuthenticationFlowService(serviceProvider.Object, new Mock<IAuthDialog>().Object);

            // The re-entrancy guard is released in the finally even when the attempt throws, so a
            // second call isn't silently swallowed — both attempts reach the CLI check and throw.
            Assert.Throws<FileNotFoundException>(() => cut.Authenticate());
            Assert.Throws<FileNotFoundException>(() => cut.Authenticate());
        }
    }
}
