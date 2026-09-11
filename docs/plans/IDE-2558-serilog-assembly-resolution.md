<!-- sub-plan: linked from docs/plans/PLAN.md -->
# IDE-2558 — SnykVSPackage fails to load ("not loaded correctly", FileNotFoundException: Serilog)

## Problem (customer outcome)
With the Snyk extension and GitLab for Visual Studio both installed, Visual Studio shows "The
SnykVSPackage package was not loaded correctly" at startup when it restores a persisted Snyk tool
window. Snyk is then unusable for that session. Nothing lands in `snyk-extension.log`, because
Serilog is the thing that failed to load, so `ActivityLog.xml` is the only record.

A second customer (ZED Technologies) hit the same failure shape for
`System.Diagnostics.DiagnosticSource` instead of Serilog.

## Root cause (verified 2026-09-10)
Three separate facts have to line up, and all three are now confirmed rather than inferred.

**1. GitLab for Visual Studio rewrites everyone's Serilog version.** Its shipped
`GitLab.Extension.pkgdef` (extracted from the 0.80.0 VSIX on the marketplace) contains:

```
[$RootKey$\RuntimeConfiguration\dependentAssembly\bindingRedirection\{FC7FB13E-EEDB-17DC-259B-05B3A4B57B01}]
"name"="Serilog"
"publicKeyToken"="24c2f752a8e58a10"
"culture"="neutral"
"oldVersion"="0.0.0.0-4.3.0.0"
"newVersion"="4.3.0.0"
```

It comes from `[assembly: ProvideBindingRedirection(... GenerateCodeBase = false ...)]` in their
`GitLab.Extension/Properties/AssemblyInfo.cs`, identical at tag `v0.80.0` and on current `main`.
They register the same kind of redirect for `System.Diagnostics.DiagnosticSource` up to `9.0.0.0`,
which is the other customer's variant.

Visual Studio merges every installed extension's `$RootKey$\RuntimeConfiguration` entries into one
generated `devenv.exe.config`. One process, one AppDomain, one flat list. These keys are not
scoped to the extension that declares them, so the redirect applies to every Serilog bind in
`devenv.exe`, ours included. We reference Serilog 2.12.0, whose AssemblyVersion is `2.0.0.0`, and
that falls inside their `0.0.0.0-4.3.0.0` range. `GenerateCodeBase = false` means they registered
no codeBase either, so the redirect says "you must use 4.3.0.0" without saying where it lives.

Serilog computes `<AssemblyVersion>$(VersionPrefix.Substring(0,3)).0.0</AssemblyVersion>`, so its
binding identity is `major.minor.0.0`. Every Serilog minor release is a new identity, and the
redirect ceiling moves with each GitLab dependency bump.

**2. Our install folder is not on the binder's probe list.** VS-MEF loads a MefComponent assembly
with `Assembly.Load` plus a codebase hint, which puts it in the default load context. Probing there
covers the GAC, `Common7\IDE`, and PrivateBinPath. It does not cover our extension directory.
Shipping `Serilog.dll` next to `Snyk.VisualStudio.Extension.dll` buys nothing on its own. We
declare no `ProvideBindingPath` and no `ProvideCodeBase`, and the generated pkgdef carries exactly
one codeBase entry, for `Community.VisualStudio.Toolkit`, which the toolkit's own build targets
inject.

**3. Our resolver cannot run in time, by construction.** `source.extension.vsixmanifest` declares
`Snyk.VisualStudio.Extension.dll` as both a `VsPackage` asset and a `Microsoft.VisualStudio.MefComponent`
asset. VS-MEF discovery therefore calls `assembly.GetTypes()` on it. ECMA-335 says a module
initializer runs "at, or sometime before, first access to any static field or first invocation of
any method defined in the module". Reflecting over an assembly is neither, so `[ModuleInitializer]`
does not fire during discovery. The binder asks for `Serilog 4.3.0.0`, fails, and the failed bind
is cached for the life of the process.

Put together: MEF discovery scans our DLL before any of our code has run, the redirect turns our
Serilog 2.0.0.0 reference into a demand for 4.3.0.0, nothing on the probe path has that identity,
and `VsShellComponentModelHost` logs

```
Still unable to load MEF component DLL: Could not load file or assembly 'Serilog, Version=4.3.0.0,
Culture=neutral, PublicKeyToken=24c2f752a8e58a10' or one of its dependencies.
path: ...\Snyk.VisualStudio.Extension.dll
```

followed by `SetSite failed for package [SnykVSPackage] hr: 0x80070002`.

This is documented prior art, not a novel failure. SLaks described the same `GetTypes()` trap in
2014 and recommended a module initializer as the fix, which C# 9 later provided. It does not
actually help, because the ordering guarantee he assumed does not exist.

## What already shipped, and why it was not enough
PRs #559 and #560 removed eager static Serilog fields and added `ExtensionAssemblyResolver`, an
`AppDomain.AssemblyResolve` fallback registered from a module initializer.

That work is correct and stays. It fixes the package-activation path, where our own code does run
first, and the resolver matches on simple name only, so it hands back our Serilog whatever version
the caller asked for. An `AssemblyResolve` handler may return a mismatched identity and the CLR
accepts it, which is exactly what defeats a foreign redirect.

It cannot fix the discovery path, because no code of ours has run at that point. Ben's manual
Windows repro on 2026-09-10 confirmed this: the failure signature moved from a `.cctor`
`TypeInitializationException` to a `VsShellComponentModelHost` MEF load error, and the demanded
version moved from 2.0.0.0 to 4.3.0.0.

## Decision: a stopgap, chosen deliberately
Line our Serilog identity up with GitLab's redirect target, and put our install folder on the
binder's probe list.

1. Take a direct `PackageReference` on Serilog 4.4.0, the current stable. Serilog's AssemblyVersion
   is `major.minor.0.0`, so this gives us 4.4.0.0, which sits *above* GitLab's `0.0.0.0-4.3.0.0`
   range. Their redirect therefore does not apply to our binds at all.
2. Bump the Serilog packages that have to move with it (`Serilog.Sinks.File`, the two enrichers).
3. Add `ProvideBindingPath` so the binder actually probes our directory. Without it the identity
   could line up and the file still would not be found. This is also a defect on its own, and the
   most likely cause of the customer's original `Serilog 2.0.0.0` not-found.

Pinning above the ceiling beats pinning *at* it. At 4.3.0.0 we would be inside their range and
would break the moment they bump. At 4.4.0.0 we are outside it today, and when they do move to
4.4.0 the redirect retargets to exactly the version we ship, so that bump costs us nothing.

`ExtensionAssemblyResolver` from #559 keeps earning its place here: it matches on simple name only,
so it bridges any Serilog package still referencing 2.0.0.0 once our own code is running.

**This will still break eventually.** Once GitLab goes past 4.4.0, our reference falls back inside
their range and gets rewritten to a version we do not ship. The team accepted that trade knowingly
on 2026-09-10 in exchange for a small diff now. It is a stopgap, not a fix, and this document
should not be read later as claiming otherwise.

### The fix this replaces
The robust option was to take the main DLL off the MEF discovery path. A small satellite assembly,
`Snyk.VisualStudio.Extension.Mef.dll`, becomes the only `Microsoft.VisualStudio.MefComponent`
asset, holds the MEF exports, and installs `ExtensionAssemblyResolver` from a `[ModuleInitializer]`.
VS-MEF enumerates only assemblies declared as MefComponent assets, so it would never call
`GetTypes()` on the main DLL. When MEF instantiates a part, a method in the satellite runs, which
is a documented module initializer trigger, so the resolver is registered before the main assembly
loads. Because the resolver ignores the requested version, that defeats any foreign redirect for
anything we ship, not just GitLab's current one.

A working implementation is parked at `.parked/` in the working tree (git-excluded): the patch, the
satellite project, and the acceptance test. Pick it up when the stopgap breaks. Two findings from
building it are worth keeping. The satellite cannot be literally third-party-free, because
`ILanguageClientCustomMessage2.AttachForCustomMessageAsync` takes a `StreamJsonRpc.JsonRpc`. And
`LanguageClientHelper` is not a usable replacement for the dropped `ILanguageClientManager` export,
because it reads back the same property the caller is trying to populate; the satellite exports a
Snyk-only `ISnykLanguageClientHost` instead, since `ILanguageClient` is exported by every installed
LSP extension and would be ambiguous through `IComponentModel.GetService`.

### Other options rejected
- **Our own `ProvideBindingRedirection`.** Process-wide rewrite of a library we do not own,
  resolved by undocumented merge order between extensions. It is the move that broke us, and doing
  it back makes us the extension that breaks someone else. Microsoft's guidance for the equivalent
  Newtonsoft.Json case is to author no redirects.
- **`ProvideCodeBase` alone.** Right layer, wrong coverage. It runs inside the binder before our
  code, but it is keyed on exact identity, so after the redirect rewrites the request to 4.3.0.0
  our entry for 2.0.0.0 no longer applies.
- **ILRepack Serilog under a private identity.** The only option immune even to a redirect we never
  anticipated, since no `Serilog` identity would remain to redirect. Disproportionate: strong
  naming, sink and enricher resolution, PDBs, and Apache-2.0 attribution all become our problem,
  and it does nothing for shared identities we do not ship, such as `DiagnosticSource`.

## Scope
- Direct `PackageReference` on Serilog 4.4.0 in `Snyk.VisualStudio.Extension.2022.csproj`, plus the
  Serilog satellite packages that have to move with it.
- `[ProvideBindingPath]` on `SnykVSPackage`.
- Acceptance test reproducing GitLab's redirect in a child AppDomain.
- `ExtensionAssemblyResolver`, the vsixmanifest, and the eager-field pin from #559/#560 all stay
  as they are.

## Acceptance criteria (customer-visible)
1. With both the Snyk extension and GitLab for Visual Studio 0.80.0 installed, closing Visual
   Studio with the Snyk tool window docked and reopening it shows no "package was not loaded
   correctly" dialog, and the Snyk tool window renders on the restored layout.
2. Snyk logging works in that session, proving Serilog resolved rather than being skipped.
3. Snyk still works with no other extension installed.
4. The failure does not come back when GitLab ships a new Serilog. Partly bought. Pinning above
   their ceiling survives their move to 4.4.0. Anything past that reopens the bug, and no test can
   pin it.

Criteria 1 to 3 are covered. Criterion 4 is bought for one GitLab release, which is the whole cost
of the stopgap.

The acceptance test is a child AppDomain carrying GitLab's exact redirect, loading the built
extension assembly and running VS-MEF part discovery over it. It fails today with this precise
exception, so it must be red before any production change. It proves the identity alignment only.
It does not exercise `ProvideBindingPath`, which is a VS-level mechanism with no equivalent in a
bare AppDomain, and its child domain has a more forgiving probe path than a real `devenv.exe`.

Manual verification on Windows with the real GitLab extension is therefore still required, and
carries more weight here than it would under the satellite approach, because the test covers less
of the mechanism.

## Follow-up
Draft an upstream issue for `gitlab-org/editor-extensions/gitlab-visual-studio-extension`. Their
two `GenerateCodeBase = false` process-wide redirects break co-installed extensions, and the
Serilog one has already produced two Snyk support tickets. Draft for review before anyone posts it.
