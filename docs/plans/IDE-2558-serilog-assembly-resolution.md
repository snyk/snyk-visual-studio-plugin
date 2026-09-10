<!-- sub-plan: linked from docs/plans/PLAN.md -->
# IDE-2558 — SnykVSPackage fails to load ("not loaded correctly", FileNotFoundException: Serilog)

## Problem (customer outcome)
On some machines Visual Studio shows "The SnykVSPackage package was not loaded correctly" at
startup when it reconstructs a persisted Snyk tool window. The ActivityLog records a
`FileNotFoundException` for `Serilog` thrown from `SnykVSPackage`'s static constructor (`.cctor`).
The reporter had **GitLab for Visual Studio 0.80.0** installed alongside Snyk; a second, related
report (ZED Technologies) saw the same failure mode for `System.Diagnostics.DiagnosticSource`.

## Root cause (verified by inspection)
1. `SnykVSPackage` has a `private static readonly ILogger Logger = LogManager.ForContext<SnykVSPackage>();`
   field initializer. Static field initializers run as part of the type's `.cctor`, which the CLR
   guarantees runs to completion (or never runs again — it's poisoned) before the type is first
   used. `LogManager.ForContext<T>()` touches `Serilog`, `Serilog.Core`, `Serilog.Exceptions`, so the
   very first reference to `SnykVSPackage` (VS reconstructing a persisted tool window calls into the
   package machinery before `InitializeAsync`) forces a `Serilog` assembly load right there in the
   `.cctor`.
2. Historically, `ManualAssemblyResolver.Initialize(extensionInstallDir)` was called early enough
   (from the VSIX host bootstrap) to backstop exactly this kind of load — VS extensions are not
   guaranteed to be loaded from a probing path that includes every extension's private dependency
   folder side by side, and when a second extension's `AssemblyResolve` handler (or a shared
   probing path collision) intercepts the request first, or answers first with an incompatible
   version, the default binder can fail with `FileNotFoundException` even though a copy of
   `Serilog.dll` sits right next to `Snyk.VisualStudio.Extension.dll`. `ManualAssemblyResolver`'s
   call site was dropped in `aa422b1` (PR #74) — well before PR #287, which only renamed/relocated
   files — leaving the class itself behind, dead, never called from anywhere.
3. With no backstop and the eager static dependency, any hiccup in resolving `Serilog` (or its
   transitive `Serilog.Exceptions`) during `SnykVSPackage..cctor` crashes package construction, and
   VS reports the generic "package was not loaded correctly" dialog instead of surfacing Snyk
   functionality at all — including the tool window itself, since MEF/`ProvideToolWindow`
   reconstruction is what triggers the first touch.

## Fix
Two independent, complementary changes — either alone reduces the blast radius, together they
close the class of bug:

1. **Remove the eager Serilog dependency from `SnykVSPackage`'s type initializer.** `Logger` is a
   property that calls `LogManager.ForContext<SnykVSPackage>()` on every access — there is no
   static field at all, so `SnykVSPackage..cctor` executes no Serilog code. `LogManager.ForContext`
   already does the expensive work once behind its own `Lazy<Logger>`, so the per-call cost here is
   a wrapper allocation. The first `Serilog` touch is deferred to the first actual log call, which
   happens deep inside `InitializeAsync` (already wrapped in try/catch) rather than at bare type
   construction.
2. **Reinstate a narrow, defensive `AppDomain.AssemblyResolve` fallback**, scoped to the Snyk
   extension only, registered as early as physically possible (a C# **module initializer**, which
   the CLR runs before any other code in the module — including any type's `.cctor` — the first
   time any type in the module is touched). Unlike the old `ManualAssemblyResolver`:
   - It resolves **lazily**, one requested assembly at a time, via `Assembly.LoadFrom` against
     `<simple name>.dll` in the extension's own install directory
     (`Path.GetDirectoryName(typeof(SnykVSPackage).Assembly.Location)`) — it never walks the
     directory or preloads anything, so it can't choke on native DLLs (`WebView2Loader.dll`,
     `runtimes/**/*.dll`) the way the old recursive `Directory.GetFileSystemEntries(...,
     AllSearchOption.AllDirectories)` + eager `Assembly.LoadFrom` did.
   - It only answers requests that are unambiguously "ours": `args.RequestingAssembly == null`, or
     a `RequestingAssembly` whose `Location` is inside the Snyk install directory. A
     `RequestingAssembly` that exists but whose location can't be determined (an in-memory or
     dynamic assembly, or an empty `Location`) is explicitly declined rather than treated as ours —
     `Assembly.Load(byte[])` and dynamic assemblies both make "no requesting assembly" and "unknown
     requesting assembly" look the same unless they're tracked separately. Matching is on simple
     name only, with no version/PublicKeyToken check (binding redirects mean the requested version
     routinely differs from the on-disk one, so a strict check would decline valid resolutions and
     reinstate this ticket's own bug). This means the handler still cannot rule out handing a
     foreign extension in the Snyk directory's own scope a same-named assembly at the wrong
     version — that residual risk is accepted, not eliminated.
   - It declines (`null`, no probing) satellite-resource requests (`*.resources`) — those are
     expected to fail per-culture and are not ours to answer.
   - It never throws and never touches `LogManager`/`Serilog`/anything outside `mscorlib`/`System`
     — it may be being asked to resolve Serilog itself, so calling into Serilog from inside the
     handler would be use-before-ready at best and unbounded recursion at worst.
   - Registration is idempotent and thread-safe (`Interlocked.CompareExchange` guard) — a module
     initializer runs once per AppDomain per module load, but the guard makes the safety
     independently verifiable and defends against any future direct call.

## Why a module initializer
`[ModuleInitializer]` methods run before any other code in the containing module, including
static field initializers of any type in that module — earlier than putting the resolver as the
very first static field of `SnykVSPackage` would be, since a module initializer isn't gated on
`SnykVSPackage` being the first type touched. `net48`'s C# compiler (LangVersion `latest`) accepts
`[ModuleInitializer]` given a local polyfill for `System.Runtime.CompilerServices.ModuleInitializerAttribute`
(the BCL attribute only ships from `netstandard2.1`/`net5.0`; on `net48` the compiler only needs
the type to exist with the right name/namespace, not to ship from a particular assembly).

## Explicitly not done
- No `ProvideBindingRedirection`/`ProvideCodeBase` for Serilog — that registers a **global,
  machine-wide** VS runtime-config entry and risks exactly this ticket's class of bug for some
  other extension.
- No Serilog/Serilog.Sinks.File/Serilog.Exceptions version changes.
- No special-casing of `System.Diagnostics.DiagnosticSource` (the ZED Technologies variant) — the
  generic same-directory-by-simple-name resolver already covers it; it is not itself in scope to
  chase down further.
- `ManualAssemblyResolver.cs` is deleted outright (dead code, unused since its call site was
  dropped in PR #74) rather than kept around deprecated, per repo convention.

## Manual verification (required — this class of bug is load-order/multi-extension dependent and
cannot be deterministically reproduced by a VS test host)
1. Build and install the VSIX locally (or via the Experimental instance).
2. Install **GitLab for Visual Studio 0.80.0** (or the latest available) into the same VS
   instance/hive.
3. Open a solution, open the Snyk tool window, dock/pin it so VS persists its layout, then fully
   close Visual Studio.
4. Reopen Visual Studio (same solution or `devenv` with no solution) with both extensions enabled.
5. **Expected**: no "The SnykVSPackage package was not loaded correctly" dialog; the Snyk tool
   window renders normally on the restored layout; `%LocalAppData%\Snyk\snyk-extension.log` shows
   normal startup log lines (proving `Serilog` did resolve, just later and more robustly than
   before).

## Files
- `Snyk.VisualStudio.Extension.2022/SnykVSPackage.cs` (`Logger` is a property with no backing
  field — see Fix, point 1)
- `Snyk.VisualStudio.Extension.2022/ExtensionAssemblyResolver.cs` (new)
- `Snyk.VisualStudio.Extension.2022/ManualAssemblyResolver.cs` (deleted, dead code)
- `Snyk.VisualStudio.Extension.2022/Snyk.VisualStudio.Extension.2022.csproj` (compile item swap)
- `Snyk.VisualStudio.Extension.Tests/ExtensionAssemblyResolverTests.cs` (unit — pure
  `ResolveCandidatePath` decision logic)
- `Snyk.VisualStudio.Extension.Tests/ExtensionAssemblyResolverWiringTests.cs` (unit — `Initialize()`
  idempotency and `OnAssemblyResolve` in-process, no VS host)
- `Snyk.VisualStudio.Extension.Tests/SnykVSPackageNoSerilogTypedStaticFieldTests.cs` (unit —
  reflection pin that `SnykVSPackage` has no static field typed in the Serilog assembly; see that
  file's header for exactly what this does and does not cover)

## Follow-up hardening (post-review)
- `SafeGetRequestingAssemblyLocation` and the scoping check now distinguish "no requesting
  assembly" from "requesting assembly present but its location is unknown" — only the former is
  treated as ours; see the Fix section's `RequestingAssembly` bullet above.
- The simple name derived from the requested assembly name is validated before it reaches
  `Path.Combine` (no path separators, no rooted path, no invalid file-name characters), closing a
  path-traversal route from a crafted assembly name.
- Every catch block, and the resolve/decline decision itself, leaves a `Trace.WriteLine`
  breadcrumb — still silent with respect to Serilog/LogManager, but no longer silent to a
  `DebugView`/trace listener.
