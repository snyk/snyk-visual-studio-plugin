using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

// net48's compiler recognizes [ModuleInitializer] purely by type name/namespace — the real
// attribute only ships starting with netstandard2.1/net5.0. The #if guard (rather than a comment
// alone) keeps this polyfill from colliding with the BCL-provided one if the project's target
// framework is ever bumped past that.
#if !NET5_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ModuleInitializerAttribute : Attribute
    {
    }
}
#endif

namespace Snyk.VisualStudio.Extension
{
    /// <summary>
    /// A narrow, extension-scoped <see cref="AppDomain.AssemblyResolve"/> fallback: given a failed
    /// default resolution, looks for <c>&lt;simple name&gt;.dll</c> next to the Snyk extension
    /// assembly and loads it if found. See
    /// docs/plans/IDE-2558-serilog-assembly-resolution.md for the full design rationale.
    ///
    /// Every catch here is silent by necessity: this handler may itself be in the middle of
    /// resolving Serilog, so it must never call into Serilog/LogManager or throw. It traces via
    /// <see cref="Trace.WriteLine(string)"/> instead, which depends only on mscorlib/System and
    /// so cannot recurse back into this resolver.
    /// </summary>
    internal static class ExtensionAssemblyResolver
    {
        private const string ResourceSuffix = ".resources";

        private static int registered;

        /// <summary>
        /// Runs before any other code in this module (including any type's static field
        /// initializers) the first time any type in this assembly is touched — earlier than a
        /// static field on <see cref="SnykVSPackage"/> could guarantee, since a module
        /// initializer isn't gated on that type specifically being the first one touched.
        /// </summary>
        [ModuleInitializer]
        internal static void Initialize()
        {
            // Idempotent + thread-safe: guards against the (currently theoretical, but cheap to
            // defend against) case of this running more than once in the same AppDomain.
            if (Interlocked.CompareExchange(ref registered, 1, 0) != 0)
            {
                return;
            }

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        // Internal (rather than private) so the wiring itself — not just ResolveCandidatePath —
        // is testable in-process without a real VS host.
        internal static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var extensionDirectory = Path.GetDirectoryName(typeof(SnykVSPackage).Assembly.Location);
                var hasRequestingAssembly = SafeGetRequestingAssemblyLocation(args.RequestingAssembly, out var requestingLocation);

                var candidatePath = ResolveCandidatePath(
                    args.Name,
                    hasRequestingAssembly,
                    requestingLocation,
                    extensionDirectory,
                    File.Exists);

                if (candidatePath == null)
                {
                    Trace.WriteLine($"Snyk ExtensionAssemblyResolver: declining '{args.Name}'.");
                    return null;
                }

                Trace.WriteLine($"Snyk ExtensionAssemblyResolver: resolving '{args.Name}' to '{candidatePath}'.");
                return Assembly.LoadFrom(candidatePath);
            }
            catch (Exception ex)
            {
                // Never throw from an AssemblyResolve handler: a throw here surfaces as the
                // original FileNotFoundException anyway.
                Trace.WriteLine($"Snyk ExtensionAssemblyResolver: failed resolving '{args?.Name}': {ex}");
                return null;
            }
        }

        /// <returns>
        /// <c>false</c> when there was no requesting assembly at all (nothing to disambiguate
        /// against); <c>true</c> otherwise, even when <paramref name="location"/> could not be
        /// determined. Callers must not conflate "no requesting assembly" with "requesting
        /// assembly present but unknown location" — only the former is safe to treat as ours.
        /// </returns>
        private static bool SafeGetRequestingAssemblyLocation(Assembly requestingAssembly, out string location)
        {
            location = null;

            if (requestingAssembly == null)
            {
                return false;
            }

            try
            {
                location = requestingAssembly.Location;
            }
            catch (NotSupportedException ex)
            {
                // Dynamic/in-memory assemblies throw on Location access — there IS a requesting
                // assembly, we just can't place it.
                Trace.WriteLine($"Snyk ExtensionAssemblyResolver: requesting assembly location unavailable: {ex.Message}");
            }

            return true;
        }

        /// <summary>
        /// Pure decision logic, factored out for unit testing without touching the real
        /// <see cref="AppDomain"/>.
        /// </summary>
        /// <param name="requestedFullName">The full assembly name being resolved (<see cref="ResolveEventArgs.Name"/>).</param>
        /// <param name="hasRequestingAssembly">
        /// Whether <see cref="ResolveEventArgs.RequestingAssembly"/> was non-null. A request with
        /// no requesting assembly has nothing to disambiguate against and is treated as ours;
        /// a request that HAS one but whose location is unknown must not be.
        /// </param>
        /// <param name="requestingAssemblyLocation">
        /// The requesting assembly's on-disk location, or null/empty if it could not be
        /// determined. Only meaningful when <paramref name="hasRequestingAssembly"/> is <c>true</c>.
        /// </param>
        /// <param name="extensionDirectory">The Snyk extension's own install directory.</param>
        /// <param name="fileExists">Injectable file-existence check (defaults to <see cref="File.Exists(string)"/> in production).</param>
        /// <returns>The candidate file path to load, or <c>null</c> if this request is not ours to answer.</returns>
        internal static string ResolveCandidatePath(
            string requestedFullName,
            bool hasRequestingAssembly,
            string requestingAssemblyLocation,
            string extensionDirectory,
            Func<string, bool> fileExists)
        {
            if (string.IsNullOrEmpty(requestedFullName) || string.IsNullOrEmpty(extensionDirectory) || fileExists == null)
            {
                return null;
            }

            string simpleName;
            try
            {
                simpleName = new AssemblyName(requestedFullName).Name;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Snyk ExtensionAssemblyResolver: could not parse assembly name '{requestedFullName}': {ex.Message}");
                return null;
            }

            if (string.IsNullOrEmpty(simpleName))
            {
                return null;
            }

            // Satellite resource assemblies are expected to legitimately miss per-culture; not
            // ours to probe for.
            if (simpleName.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!IsSafeSimpleAssemblyName(simpleName))
            {
                Trace.WriteLine($"Snyk ExtensionAssemblyResolver: refusing unsafe simple name '{simpleName}'.");
                return null;
            }

            if (!ShouldTreatRequestAsExtensionOwned(hasRequestingAssembly, requestingAssemblyLocation, extensionDirectory))
            {
                return null;
            }

            var candidatePath = Path.Combine(extensionDirectory, simpleName + ".dll");

            return fileExists(candidatePath) ? candidatePath : null;
        }

        // The simple name flows straight into Path.Combine below; a rooted path or one containing
        // a separator (e.g. "..\..\evil") would let a crafted assembly name escape the extension
        // directory entirely.
        private static bool IsSafeSimpleAssemblyName(string simpleName)
        {
            if (simpleName.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0)
            {
                return false;
            }

            if (Path.IsPathRooted(simpleName))
            {
                return false;
            }

            return simpleName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        private static bool ShouldTreatRequestAsExtensionOwned(
            bool hasRequestingAssembly,
            string requestingAssemblyLocation,
            string extensionDirectory)
        {
            // No requesting assembly (e.g. a probe triggered by the runtime itself) — treat as
            // ours; there is nothing to disambiguate against.
            if (!hasRequestingAssembly)
            {
                return true;
            }

            // There IS a requesting assembly but we could not place it (in-memory/dynamic
            // assembly, or an empty Location) — another extension is a real possibility, so
            // decline rather than risk handing it Snyk's copy of a same-named dependency.
            if (string.IsNullOrEmpty(requestingAssemblyLocation))
            {
                return false;
            }

            try
            {
                var normalizedExtensionDir = NormalizeDirectory(extensionDirectory);
                var normalizedRequestingDir = NormalizeDirectory(Path.GetDirectoryName(requestingAssemblyLocation) ?? string.Empty);

                return string.Equals(normalizedExtensionDir, normalizedRequestingDir, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Snyk ExtensionAssemblyResolver: could not compare requesting location '{requestingAssemblyLocation}': {ex.Message}");
                return false;
            }
        }

        private static string NormalizeDirectory(string path) =>
            Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
