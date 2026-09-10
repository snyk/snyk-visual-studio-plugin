// Regression pin: no type in the extension assembly may declare a static field whose declared
// FieldType — or, for a generic field, whose generic type argument — is a Serilog type. This only
// catches a field declared AS a Serilog type; it cannot catch a field of some other type (e.g.
// object) whose initializer still calls into Serilog at runtime. See
// docs/plans/IDE-2558-serilog-assembly-resolution.md.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Snyk.VisualStudio.Extension;
using Xunit;

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

        [Fact]
        public void ExtensionAssembly_DeclaresNoStaticFieldTypedInSerilogAssembly()
        {
            var offending = GetLoadableTypes(typeof(SnykVSPackage).Assembly)
                .Where(t => !AllowedTypes.Contains(t.FullName))
                .SelectMany(t => t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(FieldTouchesSerilogAssembly)
                    .Select(f => $"{t.FullName}.{f.Name}"))
                .ToList();

            Assert.Empty(offending);
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
