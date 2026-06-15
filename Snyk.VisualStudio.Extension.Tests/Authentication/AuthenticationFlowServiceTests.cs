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
    }
}
