using System.IO;
using Moq;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.Utils;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Settings
{
    public class SnykOptionsManagerReadTokenFromFileTests
    {
        private const string ValidOAuth = "{\"access_token\":\"b\",\"token_type\":\"Bearer\",\"refresh_token\":\"r2\",\"expiry\":\"2099-01-01T00:00:00Z\"}";

        private static SnykOptionsManager BuildManager(string path)
        {
            var serviceProvider = new Mock<ISnykServiceProvider>();
            serviceProvider.Setup(x => x.Options).Returns(new Mock<ISnykOptions>().Object);
            return new SnykOptionsManager(path, serviceProvider.Object);
        }

        private static void WriteSettings(string path, string token) =>
            File.WriteAllText(path, Json.Serialize(new SnykSettings { AuthenticationMethod = AuthenticationType.OAuth, Token = token }));

        [Fact]
        public void ReadTokenFromFile_SeesTokenWrittenByAnotherProcess_WithoutReplacingLoadedSettings()
        {
            var path = Path.GetTempFileName();
            WriteSettings(path, string.Empty);
            var manager = BuildManager(path);

            WriteSettings(path, ValidOAuth);

            var onDisk = manager.ReadTokenFromFile();
            Assert.Equal(ValidOAuth, onDisk.ToString());
            Assert.Equal(AuthenticationType.OAuth, onDisk.Type);
            Assert.Equal(string.Empty, manager.Load().ApiToken.ToString());
        }

        [Fact]
        public void ReadTokenFromFile_MissingFile_ReturnsNull()
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var manager = BuildManager(path);
            File.Delete(path);

            Assert.Null(manager.ReadTokenFromFile());
        }
    }
}
