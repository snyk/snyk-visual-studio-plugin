// With another extension's process-wide Serilog binding redirect in effect, VS-MEF must still be
// able to discover this extension's MEF parts.
//
// Targets the main extension assembly because source.extension.vsixmanifest declares that assembly
// itself as the Microsoft.VisualStudio.MefComponent asset, so it is what a real VS instance runs
// AttributedPartDiscovery over.
//
// Covers the identity alignment only: our Serilog reference is 4.4.0.0, above the ceiling of the
// redirect below, so the redirect never applies to it. [ProvideBindingPath] on SnykVSPackage is
// NOT covered, because it takes effect through VS's own probing path when the shell loads the
// package, and a bare AppDomain has no equivalent.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class MefDiscoveryTests
    {
        // GitLab for Visual Studio 0.80.0's exact redirect, as VS merges it into the process-wide
        // devenv.exe.config from that extension's shipped pkgdef. Our old Serilog reference
        // (2.12.0, AssemblyVersion 2.0.0.0) fell inside this range, so any bind for "Serilog" in
        // that AppDomain was rewritten to demand 4.3.0.0, a version nobody shipped. Our current
        // reference (4.4.0.0) sits above this redirect's ceiling, so no rewrite happens at all.
        private const string GitLabSerilogRedirectConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly>
        <assemblyIdentity name=""Serilog"" publicKeyToken=""24c2f752a8e58a10"" culture=""neutral"" />
        <bindingRedirect oldVersion=""0.0.0.0-4.3.0.0"" newVersion=""4.3.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>";

        [Fact]
        public void MefDiscovery_UnderForeignSerilogRedirect_FindsExtensionPartsWithoutError()
        {
            var extensionAssemblyPath = typeof(SnykVSPackage).Assembly.Location;
            var appBase = Path.GetDirectoryName(typeof(MefDiscoveryTests).Assembly.Location);
            var configPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".config");
            File.WriteAllText(configPath, GitLabSerilogRedirectConfig);

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

                var failure = probe.DiscoverPartsAndReturnFailureOrNull(extensionAssemblyPath);

                Assert.True(failure == null, "MEF discovery of the extension assembly failed under a foreign Serilog redirect: " + failure);
            }
            finally
            {
                File.Delete(configPath);
                if (probeDomain != null)
                {
                    AppDomain.Unload(probeDomain);
                }
            }
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
