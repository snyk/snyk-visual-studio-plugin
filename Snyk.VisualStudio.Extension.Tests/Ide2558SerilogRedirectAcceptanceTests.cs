// Acceptance test for IDE-2558: with a foreign extension's process-wide Serilog binding redirect
// in effect, VS-MEF must still be able to discover this extension's MEF parts. See
// docs/plans/IDE-2558-serilog-assembly-resolution.md for the full root-cause analysis; this test
// is the "criterion 4" reproduction called out there.
//
// Targets the main extension assembly (Snyk.VisualStudio.Extension.2022.dll): per
// source.extension.vsixmanifest, that assembly is itself the Microsoft.VisualStudio.MefComponent
// asset VS discovers, so it is the assembly a real VS instance runs AttributedPartDiscovery over.
//
// This proves the identity-alignment half of the fix: our Serilog reference is now 4.4.0.0,
// above the ceiling of GitLab's current redirect (oldVersion 0.0.0.0-4.3.0.0 -> 4.3.0.0), so that
// redirect simply does not apply to a bind for our Serilog identity, and MEF discovery does not
// throw. It does NOT exercise [ProvideBindingPath] on SnykVSPackage: that attribute only takes
// effect through VS's own probing path when the package is loaded by the shell, which a bare
// AppDomain has no equivalent of.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class Ide2558SerilogRedirectAcceptanceTests
    {
        // GitLab for Visual Studio 0.80.0's exact redirect, as VS merges it into the process-wide
        // devenv.exe.config from GitLab's shipped GitLab.Extension.pkgdef. Our old Serilog reference
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
            var appBase = Path.GetDirectoryName(typeof(Ide2558SerilogRedirectAcceptanceTests).Assembly.Location);
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

                probeDomain = AppDomain.CreateDomain("IDE-2558-redirect-probe", null, setup);

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
