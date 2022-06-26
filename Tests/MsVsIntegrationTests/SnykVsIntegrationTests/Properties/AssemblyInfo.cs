using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Harness;

// Configure test framework - taken from https://github.com/microsoft/vs-extension-testing/blob/8d2e84bca8bf076a85081e73ac0209a33050a90d/README.md
[assembly: TestFramework("Xunit.Harness.IdeTestFramework", "Microsoft.VisualStudio.Extensibility.Testing.Xunit")]
[assembly: RequireExtension("../../../../../Snyk.VisualStudio.Extension.2022/bin/Debug/Snyk.VisualStudio.Extension.vsix")]

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("SnykVsIntegrationTests")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("SnykVsIntegrationTests")]
[assembly: AssemblyCopyright("Copyright ©  2022")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("305e3eb9-6f5b-405e-bbcb-39b6059daf65")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
