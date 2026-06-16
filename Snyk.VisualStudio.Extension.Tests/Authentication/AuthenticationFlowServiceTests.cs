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
