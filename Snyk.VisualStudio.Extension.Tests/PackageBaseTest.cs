using System.Reflection;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using Serilog;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    [Collection(MockedVS.Collection)]
    public class PackageBaseTest
    {
        protected SnykVSPackage VsPackage;
        protected readonly Mock<ISnykOptions> OptionsMock;
        protected readonly Mock<ISnykOptionsManager> OptionsManagerMock;
        protected readonly Mock<ISnykServiceProvider> ServiceProviderMock;
        protected readonly Mock<ISnykTasksService> TasksServiceMock;

        public PackageBaseTest(GlobalServiceProvider sp)
        {
            sp.Reset();
            OptionsMock = new Mock<ISnykOptions>();
            OptionsManagerMock = new Mock<ISnykOptionsManager>();
            ServiceProviderMock = new Mock<ISnykServiceProvider>();
            TasksServiceMock = new Mock<ISnykTasksService>();
            var loggerMock = new Mock<ILogger>();
            Log.Logger = loggerMock.Object;
            
            ServiceProviderMock.Setup(x => x.SnykOptionsManager).Returns(OptionsManagerMock.Object);
            ServiceProviderMock.Setup(x => x.Options).Returns(OptionsMock.Object);
            ServiceProviderMock.Setup(x => x.TasksService).Returns(TasksServiceMock.Object);
            
            sp.AddService(typeof(ISnykService), ServiceProviderMock.Object);

            VsPackage = new SnykVSPackage();

            var instanceField = typeof(SnykVSPackage).GetField(nameof(SnykVSPackage.Instance), BindingFlags.Static | BindingFlags.Public);
            instanceField.SetValue(null, VsPackage);
            
            typeof(SnykVSPackage).GetProperty(nameof(SnykVSPackage.Options)).SetValue(VsPackage, OptionsMock.Object);
            typeof(SnykVSPackage).GetProperty(nameof(SnykVSPackage.IsInitialized)).SetValue(VsPackage, true, null);
         
            VsPackage.SetServiceProvider(ServiceProviderMock.Object);
        }
    }
}