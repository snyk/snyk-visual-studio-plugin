// With another extension's process-wide Serilog binding redirect in effect, VS-MEF must still be
// able to discover this extension's MEF parts.
//
// Targets the main extension assembly because source.extension.vsixmanifest declares that assembly
// itself as the Microsoft.VisualStudio.MefComponent asset, so it is what a real VS instance runs
// AttributedPartDiscovery over.
//
// Covers the identity alignment only: our Serilog reference is 4.4.0.0, above the ceiling of the
// GitLab redirect below, so that redirect never applies to it. [ProvideBindingPath] on
// SnykVSPackage is NOT covered, because it takes effect through VS's own probing path when the
// shell loads the package, and a bare AppDomain has no equivalent.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class MefDiscoveryTests
    {
        private const string AsmBindingNamespace = "urn:schemas-microsoft-com:asm.v1";
        private const string SerilogPublicKeyToken = "24c2f752a8e58a10";

        [Fact]
        public void MefDiscovery_UnderGitLabsSerilogRedirect_DiscoversExtensionParts()
        {
            // GitLab for Visual Studio 0.80.0's exact redirect, as VS merges it into the
            // process-wide devenv.exe.config from that extension's shipped pkgdef. Our old Serilog
            // reference (2.12.0, AssemblyVersion 2.0.0.0) fell inside this range and was rewritten
            // to demand 4.3.0.0, a version nobody shipped.
            var failure = DiscoverPartsUnderSerilogRedirect("0.0.0.0-4.3.0.0", "4.3.0.0");

            Assert.True(failure == null, "MEF discovery failed under GitLab's Serilog redirect: " + failure);
        }

        // Negative control for the test above. Without it, that test passing proves nothing: it
        // would also pass if this harness were incapable of reproducing the failure at all. Here
        // the redirect targets a Serilog nobody ships, so discovery MUST fail — if it does not,
        // the harness is not exercising the bind the bug lives in and the positive test is
        // worthless.
        [Fact]
        public void MefDiscovery_UnderRedirectToAnUnshippedSerilog_Fails()
        {
            var failure = DiscoverPartsUnderSerilogRedirect("0.0.0.0-9.9.0.0", "9.9.0.0");

            Assert.False(
                failure == null,
                "MEF discovery succeeded under a redirect to Serilog 9.9.0.0, which nothing ships. " +
                "This harness is not reaching the assembly bind that IDE-2558 turns on, so the " +
                "companion test proves nothing.");
        }

        private static string DiscoverPartsUnderSerilogRedirect(string oldVersionRange, string newVersion)
        {
            var extensionAssemblyPath = typeof(SnykVSPackage).Assembly.Location;
            var appBase = Path.GetDirectoryName(typeof(MefDiscoveryTests).Assembly.Location);
            var configPath = WriteConfigWithSerilogRedirect(oldVersionRange, newVersion);

            AppDomain probeDomain = null;
            try
            {
                var setup = new AppDomainSetup
                {
                    ApplicationBase = appBase,
                    ConfigurationFile = configPath,
                };

                probeDomain = AppDomain.CreateDomain("serilog-redirect-probe", null, setup);

                var probe = (RedirectProbe)probeDomain.CreateInstanceFromAndUnwrap(
                    typeof(RedirectProbe).Assembly.Location,
                    typeof(RedirectProbe).FullName);

                return probe.DiscoverPartsAndReturnFailureOrNull(extensionAssemblyPath);
            }
            finally
            {
                if (probeDomain != null)
                {
                    AppDomain.Unload(probeDomain);
                }

                File.Delete(configPath);
            }
        }

        /// <summary>
        /// Starts from the test assembly's own generated config rather than an empty document. That
        /// config carries the binding redirects the SDK generates for this project, and
        /// Microsoft.VisualStudio.Composition's dependency closure does not bind without them — a
        /// hand-rolled minimal config makes the probe domain fail to load the discovery engine,
        /// which looks exactly like the failure under test but is not it.
        /// </summary>
        private static string WriteConfigWithSerilogRedirect(string oldVersionRange, string newVersion)
        {
            var document = new XmlDocument();
            var hostConfig = typeof(MefDiscoveryTests).Assembly.Location + ".config";

            if (File.Exists(hostConfig))
            {
                document.Load(hostConfig);
            }
            else
            {
                document.LoadXml("<configuration />");
            }

            var configuration = document.DocumentElement
                ?? (XmlElement)document.AppendChild(document.CreateElement("configuration"));

            var runtime = configuration["runtime"]
                ?? (XmlElement)configuration.AppendChild(document.CreateElement("runtime"));

            var assemblyBinding = runtime["assemblyBinding", AsmBindingNamespace]
                ?? (XmlElement)runtime.AppendChild(document.CreateElement("assemblyBinding", AsmBindingNamespace));

            var dependentAssembly = (XmlElement)assemblyBinding.AppendChild(
                document.CreateElement("dependentAssembly", AsmBindingNamespace));

            var identity = (XmlElement)dependentAssembly.AppendChild(
                document.CreateElement("assemblyIdentity", AsmBindingNamespace));
            identity.SetAttribute("name", "Serilog");
            identity.SetAttribute("publicKeyToken", SerilogPublicKeyToken);
            identity.SetAttribute("culture", "neutral");

            var redirect = (XmlElement)dependentAssembly.AppendChild(
                document.CreateElement("bindingRedirect", AsmBindingNamespace));
            redirect.SetAttribute("oldVersion", oldVersionRange);
            redirect.SetAttribute("newVersion", newVersion);

            var configPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".config");
            document.Save(configPath);
            return configPath;
        }

        // Runs inside the child AppDomain under the foreign redirect. Takes the target assembly's
        // path rather than a static reference to it, so this class stays loadable and callable even
        // before the target assembly has bound cleanly.
        public class RedirectProbe : MarshalByRefObject
        {
            public string DiscoverPartsAndReturnFailureOrNull(string assemblyPath)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(assemblyPath);

                    // MEF1 attributes (System.ComponentModel.Composition) are what this codebase's
                    // [Export]/[ImportingConstructor] attributes actually are; MEF2 discovery is
                    // combined in because VsMefHostServices.DefaultAssemblies / VS's real
                    // component-model host does the same combination.
                    var discovery = PartDiscovery.Combine(
                        new AttributedPartDiscoveryV1(Resolver.DefaultInstance),
                        new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true));

                    var discovered = discovery.CreatePartsAsync(assembly).GetAwaiter().GetResult();

                    if (discovered.DiscoveryErrors.Count > 0)
                    {
                        return string.Join("; ", discovered.DiscoveryErrors.Select(e => e.ToString()));
                    }

                    if (discovered.Parts.Count == 0)
                    {
                        return "MEF discovered zero parts in " + assemblyPath;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    return ex.ToString();
                }
            }
        }
    }
}
