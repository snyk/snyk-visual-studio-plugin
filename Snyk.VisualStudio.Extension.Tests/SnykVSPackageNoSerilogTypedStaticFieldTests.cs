// Regression pin: SnykVSPackage must not declare a static field whose declared FieldType — or,
// for a generic field, whose generic type argument — is a Serilog type. This only catches a field
// declared AS a Serilog type; it cannot catch a field of some other type (e.g. object) whose
// initializer still calls into Serilog at runtime. See
// docs/plans/IDE-2558-serilog-assembly-resolution.md.
using System.Linq;
using System.Reflection;
using Snyk.VisualStudio.Extension;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class SnykVSPackageNoSerilogTypedStaticFieldTests
    {
        [Fact]
        public void SnykVSPackage_DeclaresNoStaticFieldTypedInSerilogAssembly()
        {
            var allStaticFields = typeof(SnykVSPackage).GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            var offending = allStaticFields
                .Where(FieldTouchesSerilogAssembly)
                .Select(f => f.Name)
                .ToList();

            Assert.Empty(offending);
        }

        private static bool FieldTouchesSerilogAssembly(FieldInfo field)
        {
            return TypeIsFromSerilogAssembly(field.FieldType) ||
                   GetGenericArgumentsSafe(field.FieldType).Any(TypeIsFromSerilogAssembly);
        }

        private static System.Type[] GetGenericArgumentsSafe(System.Type type)
        {
            return type.IsGenericType ? type.GetGenericArguments() : System.Type.EmptyTypes;
        }

        private static bool TypeIsFromSerilogAssembly(System.Type type)
        {
            return type.Assembly.GetName().Name.Equals("Serilog", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
