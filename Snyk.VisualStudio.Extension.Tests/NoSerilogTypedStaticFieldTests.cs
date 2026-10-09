// Regression pin: no type in the extension assembly may declare a static field whose declared
// FieldType — or, for a generic field, whose generic type argument — is a Serilog type. This only
// catches a field declared AS a Serilog type; it cannot catch a field of some other type (e.g.
// object) whose initializer still calls into Serilog at runtime.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Snyk.VisualStudio.Extension;
using Xunit;
using Xunit.Abstractions;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class NoSerilogTypedStaticFieldTests
    {
        // LogManager is the Serilog bridge itself: touching any of its members loads Serilog by
        // design, and its Lazy<Logger>/LoggingLevelSwitch fields are the guarded implementation
        // everything else routes through. The hazard this test guards against is an unrelated
        // type forcing that same load open via its own eager field before that type's own
        // initialization/try-catch has run — that does not apply to LogManager itself.
        private static readonly HashSet<string> AllowedTypes = new HashSet<string>
        {
            typeof(LogManager).FullName,
        };

        private readonly ITestOutputHelper output;

        public NoSerilogTypedStaticFieldTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void ExtensionAssembly_DeclaresNoStaticFieldTypedInSerilogAssembly()
        {
            var offending = new List<string>();
            var unresolvable = new List<string>();

            foreach (var type in GetLoadableTypes(typeof(SnykVSPackage).Assembly))
            {
                if (AllowedTypes.Contains(type.FullName))
                {
                    continue;
                }

                var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    bool touchesSerilog;
                    try
                    {
                        touchesSerilog = FieldTouchesSerilogAssembly(field);
                    }
                    catch (Exception ex) when (ex is TypeLoadException || ex is FileNotFoundException || ex is FileLoadException || ex is BadImageFormatException)
                    {
                        // The field's declared type couldn't be resolved (e.g. its assembly isn't
                        // present in the test host). That is exactly the unknown state this pin
                        // must not wave through, so it goes in a separate, always-reported bucket
                        // instead of being counted as clean.
                        unresolvable.Add($"{type.FullName}.{field.Name} ({ex.GetType().Name})");
                        continue;
                    }

                    if (touchesSerilog)
                    {
                        offending.Add($"{type.FullName}.{field.Name}");
                    }
                }
            }

            // Reported on every run, pass or fail, since an unrelated VS SDK/host type landing
            // here is invisible to a human otherwise: a passing test never shows its message.
            if (unresolvable.Count > 0)
            {
                output.WriteLine("Fields that could not be inspected (declared type failed to load; verify manually): " + Describe(unresolvable));
            }

            var message = "Serilog-typed static fields: " + Describe(offending) +
                          ". Fields that could not be inspected (declared type failed to load; verify manually): " + Describe(unresolvable);

            Assert.True(offending.Count == 0, message);
        }

        private static string Describe(List<string> names)
        {
            return names.Count == 0 ? "(none)" : string.Join(", ", names);
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }

        private static bool FieldTouchesSerilogAssembly(FieldInfo field)
        {
            return TypeIsFromSerilogAssembly(field.FieldType) ||
                   GetGenericArgumentsSafe(field.FieldType).Any(TypeIsFromSerilogAssembly);
        }

        private static Type[] GetGenericArgumentsSafe(Type type)
        {
            return type.IsGenericType ? type.GetGenericArguments() : Type.EmptyTypes;
        }

        private static bool TypeIsFromSerilogAssembly(Type type)
        {
            return type.Assembly.GetName().Name.Equals("Serilog", StringComparison.OrdinalIgnoreCase);
        }
    }
}
