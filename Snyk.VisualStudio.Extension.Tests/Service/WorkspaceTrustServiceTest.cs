namespace Snyk.VisualStudio.Extension.Tests.Service
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Moq;
    using Snyk.VisualStudio.Extension.Service;
    using Snyk.VisualStudio.Extension.Settings;
    using Xunit;

    public class WorkspaceTrustServiceTest
    {
        [Fact]
        public void WorkspaceTrustServiceTest_IsFolderTrusted_NotTrusted()
        {
            var trustedFolders = new HashSet<string>();
            var settingsServiceMock = new Mock<IUserStorageSettingsService>();
            settingsServiceMock.Setup(s => s.TrustedFolders).Returns(trustedFolders);

            var service = new WorkspaceTrustService(settingsServiceMock.Object);
            var folderPath = "C:\\Users\\Project";

            Assert.False(service.IsFolderTrusted(folderPath));
        }

        [Fact]
        public void WorkspaceTrustServiceTest_IsFolderTrusted_Trusted()
        {
            var trustedFolders = new HashSet<string>();
            trustedFolders.Add("C:\\Users\\Project");
            var settingsServiceMock = new Mock<IUserStorageSettingsService>();
            settingsServiceMock.Setup(s => s.TrustedFolders).Returns(trustedFolders);

            var service = new WorkspaceTrustService(settingsServiceMock.Object);
            var folderPath = "C:\\Users\\Project";

            Assert.True(service.IsFolderTrusted(folderPath));
        }

        [Fact]
        public void WorkspaceTrustServiceTest_IsFolderTrusted_SubfolderTrusted()
        {
            var trustedFolders = new HashSet<string>();
            trustedFolders.Add("C:\\Users\\Project");

            var settingsServiceMock = new Mock<IUserStorageSettingsService>();
            settingsServiceMock.Setup(s => s.TrustedFolders).Returns(trustedFolders);

            var service = new WorkspaceTrustService(settingsServiceMock.Object);
            var folderPath = "C:\\Users\\Project\\subfolder";

            Assert.True(service.IsFolderTrusted(folderPath));
        }

        [Fact]
        public void WorkspaceTrustServiceTest_IsFolderTrusted_ParentFolderNotTrusted()
        {
            var trustedFolders = new HashSet<string>();
            trustedFolders.Add("C:\\Users\\Project\\subfolder");

            var settingsServiceMock = new Mock<IUserStorageSettingsService>();
            settingsServiceMock.Setup(s => s.TrustedFolders).Returns(trustedFolders);

            var service = new WorkspaceTrustService(settingsServiceMock.Object);
            var folderPath = "C:\\Users\\Project";

            Assert.False(service.IsFolderTrusted(folderPath));
        }

        [Fact]
        public void WorkspaceTrustServiceTest_AddFolderToTrusted_NonExistingFolder()
        {
            var settingsServiceMock = new Mock<IUserStorageSettingsService>();

            var service = new WorkspaceTrustService(settingsServiceMock.Object);
            var folderPath = "C:\\Users\\Project";

            Assert.Throws<ArgumentException>(() => service.AddFolderToTrusted(folderPath));
        }

        [Fact]
        public void WorkspaceTrustServiceTest_AddFolderToTrusted_RelativeFolder()
        {
            var settingsServiceMock = new Mock<IUserStorageSettingsService>();

            var service = new WorkspaceTrustService(settingsServiceMock.Object);
            var folderPath = "\\Users\\Project";

            Assert.Throws<ArgumentException>(() => service.AddFolderToTrusted(folderPath));
        }

        [Fact]
        public void WorkspaceTrustServiceTest_AddFolderToTrusted_ExistingFolder()
        {
            var settingsServiceMock = new Mock<IUserStorageSettingsService>();
            settingsServiceMock.Setup(s => s.TrustedFolders).Returns(new HashSet<string>());

            var service = new WorkspaceTrustService(settingsServiceMock.Object);
            var folderPath = Path.GetDirectoryName(Path.GetTempFileName());

            service.AddFolderToTrusted(folderPath);

            settingsServiceMock.VerifySet(s => s.TrustedFolders = new HashSet<string> { folderPath }, Times.Once);
        }

        [Fact]
        public void WorkspaceTrustServiceTest_AddFolderToTrusted_MultipleFolders()
        {
            var settingsServiceMock = new Mock<IUserStorageSettingsService>();
            var presentFolder = "C:\\Users\\Project";
            settingsServiceMock.Setup(s => s.TrustedFolders).Returns(new HashSet<string> { presentFolder });

            var service = new WorkspaceTrustService(settingsServiceMock.Object);

            var newFolderPath = this.CreateTempDirectory();

            service.AddFolderToTrusted(newFolderPath);

            settingsServiceMock.VerifySet(s => s.TrustedFolders = new HashSet<string> { presentFolder, newFolderPath });
        }

        [Fact]
        public void WorkspaceTrustServiceTest_AddFolderToTrusted_SameFolderTwice()
        {
            var settingsServiceMock = new Mock<IUserStorageSettingsService>();
            settingsServiceMock.Setup(s => s.TrustedFolders).Returns(new HashSet<string>());
            var service = new WorkspaceTrustService(settingsServiceMock.Object);

            var folderPath1 = this.CreateTempDirectory();
            var folderPath2 = folderPath1;

            service.AddFolderToTrusted(folderPath1);
            settingsServiceMock.VerifySet(s => s.TrustedFolders = new HashSet<string> { folderPath1 }, Times.Once);

            service.AddFolderToTrusted(folderPath2);

            // Must not append new entry to collection
            settingsServiceMock.VerifySet(s => s.TrustedFolders = new HashSet<string> { folderPath1 }, Times.Exactly(2));
        }

        private string CreateTempDirectory()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);

            return tempDirectory;
        }
    }
}
