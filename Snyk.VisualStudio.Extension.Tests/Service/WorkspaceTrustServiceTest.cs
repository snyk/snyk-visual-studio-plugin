using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Service
{
    public class WorkspaceTrustServiceTest
    {
        private readonly Mock<ISnykOptions> optionsMock;
        private readonly Mock<ISnykServiceProvider> serviceProviderMock;
        private readonly WorkspaceTrustService cut;

        public WorkspaceTrustServiceTest()
        {
            optionsMock = new Mock<ISnykOptions>();
            optionsMock.Setup(x => x.TrustedFolders).Returns(new HashSet<string>());
            serviceProviderMock = new Mock<ISnykServiceProvider>();
            serviceProviderMock.Setup(x => x.Options).Returns(optionsMock.Object);

            cut = new WorkspaceTrustService(serviceProviderMock.Object);
        }

        [Fact]
        public void WorkspaceTrustServiceTest_IsFolderTrusted_NotTrusted()
        {
            var folderPath = "C:\\Users\\Project";
            Assert.False(cut.IsFolderTrusted(folderPath));
        }

        [Fact]
        public void WorkspaceTrustServiceTest_IsFolderTrusted_Trusted()
        {
            var trustedFolders = new HashSet<string>();
            trustedFolders.Add("C:\\Users\\Project");
            optionsMock.Setup(s => s.TrustedFolders).Returns(trustedFolders);

            var folderPath = "C:\\Users\\Project";

            Assert.True(cut.IsFolderTrusted(folderPath));
        }

        [Fact]
        public void WorkspaceTrustServiceTest_IsFolderTrusted_SubfolderTrusted()
        {
            var trustedFolders = new HashSet<string> { "C:\\Users\\Project" };

            optionsMock.Setup(s => s.TrustedFolders).Returns(trustedFolders);

            var folderPath = "C:\\Users\\Project\\subfolder";

            Assert.True(cut.IsFolderTrusted(folderPath));
        }

        [Fact]
        public void WorkspaceTrustServiceTest_IsFolderTrusted_ParentFolderNotTrusted()
        {
            var trustedFolders = new HashSet<string>();
            trustedFolders.Add("C:\\Users\\Project\\subfolder");

            optionsMock.Setup(s => s.TrustedFolders).Returns(trustedFolders);

            var folderPath = "C:\\Users\\Project";

            Assert.False(cut.IsFolderTrusted(folderPath));
        }

        [Fact]
        public void WorkspaceTrustServiceTest_AddFolderToTrusted_NonExistingFolder()
        {
            var folderPath = "C:\\Users\\Project";

            Assert.Throws<ArgumentException>(() => cut.AddFolderToTrusted(folderPath));
        }

        [Fact]
        public void WorkspaceTrustServiceTest_AddFolderToTrusted_RelativeFolder()
        {
            var folderPath = "\\Users\\Project";

            Assert.Throws<ArgumentException>(() => cut.AddFolderToTrusted(folderPath));
        }

        [Fact]
        public void WorkspaceTrustServiceTest_AddFolderToTrusted_ExistingFolder()
        {
            optionsMock.Setup(s => s.TrustedFolders).Returns(new HashSet<string>());

            var folderPath = Path.GetDirectoryName(Path.GetTempFileName());

            cut.AddFolderToTrusted(folderPath);

            optionsMock.VerifySet(s => s.TrustedFolders = new HashSet<string> { folderPath }, Times.Once);
        }

        [Fact]
        public void WorkspaceTrustServiceTest_AddFolderToTrusted_MultipleFolders()
        {
            var presentFolder = "C:\\Users\\Project";
            optionsMock.Setup(s => s.TrustedFolders).Returns(new HashSet<string> { presentFolder });


            var newFolderPath = this.CreateTempDirectory();

            cut.AddFolderToTrusted(newFolderPath);

            optionsMock.VerifySet(s => s.TrustedFolders = new HashSet<string> { presentFolder, newFolderPath });
        }

        [Fact]
        public void WorkspaceTrustServiceTest_AddFolderToTrusted_SameFolderTwice()
        {
            optionsMock.Setup(s => s.TrustedFolders).Returns(new HashSet<string>());

            var folderPath1 = this.CreateTempDirectory();
            var folderPath2 = folderPath1;

            cut.AddFolderToTrusted(folderPath1);
            optionsMock.VerifySet(s => s.TrustedFolders = new HashSet<string> { folderPath1 }, Times.Once);

            cut.AddFolderToTrusted(folderPath2);

            // Must not append new entry to collection
            optionsMock.VerifySet(s => s.TrustedFolders = new HashSet<string> { folderPath1 }, Times.Exactly(2));
        }

        private string CreateTempDirectory()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);

            return tempDirectory;
        }
    }
}
