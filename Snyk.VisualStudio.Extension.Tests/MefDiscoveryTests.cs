// With another extension's process-wide Serilog binding redirect in effect, the extension assembly
// must still survive the type enumeration Visual Studio's MEF catalog discovery runs over it.
//
// Targets the main extension assembly because source.extension.vsixmanifest declares that assembly
// itself as the Microsoft.VisualStudio.MefComponent asset, so it is what a real VS instance
// enumerates.
//
// The probe reproduces what VS-MEF does to trip this bug — Assembly.GetTypes() plus resolving the
// declared types of each type's fields and properties — using nothing but reflection. Running the
// real VS-MEF engine inside the probe domain was tried first and could not load its own dependency
// closure there, which failed the test for a reason that had nothing to do with the bug.
//
// Covers the identity alignment only: our Serilog reference is 4.4.0.0, above the ceiling of the
// GitLab redirect below, so that redirect never applies to it. [ProvideBindingPath] on
// SnykVSPackage is NOT covered, because it takes effect through VS's own probing path when the
// shell loads the package, and a bare AppDomain has no equivalent.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class MefDiscoveryTests
    {
        private const string AsmBindingNamespace = "urn:schemas-microsoft-com:asm.v1";
        private const string SerilogPublicKeyToken = "24c2f752a8e58a10";

        [Fact]
        public void TypeEnumeration_UnderGitLabsSerilogRedirect_Succeeds()
        {
            // GitLab for Visual Studio 0.80.0's exact redirect, as VS merges it into the
            // process-wide devenv.exe.config from that extension's shipped pkgdef. Our old Serilog
            // reference (2.12.0, AssemblyVersion 2.0.0.0) fell inside this range and was rewritten
            // to demand 4.3.0.0, a version nobody shipped.
            var failure = EnumerateExtensionTypesUnderSerilogRedirect("0.0.0.0-4.3.0.0", "4.3.0.0");

            Assert.True(failure == null, "Type enumeration failed under GitLab's Serilog redirect: " + failure);
        }

        // Negative control for the test above. Without it, that test passing proves nothing: it
        // would also pass if this harness never reached the assembly bind the bug lives in. Here
        // the redirect targets a Serilog nobody ships, so enumeration MUST fail.
        [Fact]
        public void TypeEnumeration_UnderRedirectToAnUnshippedSerilog_Fails()
        {
            var failure = EnumerateExtensionTypesUnderSerilogRedirect("0.0.0.0-9.9.0.0", "9.9.0.0");

            Assert.False(
                failure == null,
                "Type enumeration succeeded under a redirect to Serilog 9.9.0.0, which nothing ships. " +
                "This harness is not reaching the assembly bind the fix targets, so the companion " +
                "test proves nothing.");
        }

        private static string EnumerateExtensionTypesUnderSerilogRedirect(string oldVersionRange, string newVersion)
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

                return probe.EnumerateTypesAndReturnFailureOrNull(extensionAssemblyPath);
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

        private static string WriteConfigWithSerilogRedirect(string oldVersionRange, string newVersion)
        {
            var document = new XmlDocument();
            document.LoadXml("<configuration />");

            var runtime = (XmlElement)document.DocumentElement.AppendChild(document.CreateElement("runtime"));
            var assemblyBinding = (XmlElement)runtime.AppendChild(
                document.CreateElement("assemblyBinding", AsmBindingNamespace));
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

        // Runs inside the child AppDomain under the foreign redirect. Deliberately depends on
        // nothing beyond mscorlib: anything else would have to bind inside that domain too, and a
        // failure to do so is indistinguishable from the failure under test. It takes the target
        // assembly's path rather than a static reference for the same reason.
        public class RedirectProbe : MarshalByRefObject
        {
            public string EnumerateTypesAndReturnFailureOrNull(string assemblyPath)
            {
                var failures = new List<string>();
                Type[] types;

                try
                {
                    var assembly = Assembly.LoadFrom(assemblyPath);
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    foreach (var loaderException in ex.LoaderExceptions)
                    {
                        failures.Add(loaderException.Message);
                    }

                    return string.Join("; ", failures.ToArray());
                }
                catch (Exception ex)
                {
                    return ex.ToString();
                }

                if (types.Length == 0)
                {
                    return "No types found in " + assemblyPath;
                }

                // GetTypes() alone does not force every member signature to resolve. VS-MEF reads
                // the declared type of each field and property while building part metadata, and
                // that read is what binds Serilog.
                foreach (var type in types)
                {
                    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic
                        | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

                    try
                    {
                        foreach (var field in type.GetFields(All))
                        {
                            GC.KeepAlive(field.FieldType);
                        }

                        foreach (var property in type.GetProperties(All))
                        {
                            GC.KeepAlive(property.PropertyType);
                        }
                    }
                    catch (Exception ex)
                    {
                        failures.Add(type.FullName + ": " + ex.Message);
                    }
                }

                return failures.Count == 0 ? null : string.Join("; ", failures.ToArray());
            }
        }
    }
}
