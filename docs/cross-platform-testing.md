# Cross-platform unit testing

Most of this extension's unit tests now run on Linux, macOS and Windows with nothing but the
.NET SDK:

```bash
dotnet test Snyk.VisualStudio.Extension.Core.Tests
```

No Visual Studio, no MSBuild, no VSIX tooling, no `nuget.config` VSSDK feeds. This means you can
develop and verify most changes to the extension's logic without a Windows machine, and the
`Run Cross-Platform Unit-Tests` job in `pr-workflow.yml` gates every pull request on Linux.

## How it works

`Snyk.VisualStudio.Extension.2022` is a legacy net48 VSIX project: referencing it pulls in the
Visual Studio SDK, WPF and WebView2, none of which exist off Windows. A large part of the
extension, though, is ordinary .NET code — settings persistence, language-server configuration,
CLI download logic, HTML/CSS string manipulation, URI handling.

`Snyk.VisualStudio.Extension.Core.Tests` is a `net8.0` xUnit project that has almost no sources of
its own. Instead it **compiles the platform-neutral subset of the existing sources** as linked
files:

| File | Contains |
| --- | --- |
| `Snyk.VisualStudio.Extension.Core.props` | the platform-neutral production sources, from `Snyk.VisualStudio.Extension.2022/` |
| `Snyk.VisualStudio.Extension.Core.Tests.props` | the platform-neutral test sources, from `Snyk.VisualStudio.Extension.Tests/` |

```
Snyk.VisualStudio.Extension.2022        (net48 VSIX, Windows only)  ─┐
                                                                     ├─ same source files
Snyk.VisualStudio.Extension.Core.Tests  (net8.0, any OS)  ──────────┘

Snyk.VisualStudio.Extension.Tests       (net48, Windows only)  ─┐
                                                                ├─ same test files
Snyk.VisualStudio.Extension.Core.Tests  (net8.0, any OS)  ──────┘
```

Nothing was moved or duplicated. Every shared test still runs on Windows against the real net48
VSIX assembly exactly as before; the cross-platform project runs the same test code a second time
against the same source compiled for .NET 8. Two consequences worth knowing:

- Windows coverage never regressed as a side effect of this work.
- Occasionally the two target frameworks disagree (`Snyk.VisualStudio.Extension.Language.Range`
  vs `System.Range`, for instance). That is a real finding about the code, not an artefact.

## The `.Vs.cs` convention

Some types are *mostly* platform-neutral with a small Visual Studio dependency. Rather than
introducing runtime indirection, those types are `partial` and split across two files:

| File | Compiled by | Contains |
| --- | --- | --- |
| `Foo.cs` | both projects | everything that does not need the IDE |
| `Foo.Vs.cs` | the VSIX only | the members that need the VS SDK |

Both `partial class` and `partial interface` are used. The split is resolved entirely at compile
time, so **the Windows build and the shipped VSIX behave exactly as before** — there is no
registration step to forget and no runtime fallback to get wrong.

Current examples:

| Type | What lives in `.Vs.cs` |
| --- | --- |
| `ISnykServiceProvider` | `DTE`, `Package`, `AsyncServiceProvider`, `SettingsManager`, `VsThemeService`, `ToolWindow`, `GetServiceAsync` |
| `ISolutionService` | `SolutionEvents` (`SnykVsSolutionLoadEvents`) |
| `ThreadContextEnricher` | resolving the IDE UI thread via `ThreadHelper.CheckAccess()` |
| `SnykCliDownloader` | raising the VS info bar when a CLI update fails |
| `BaseHtmlProvider` | reading the active VS colour theme into an `HtmlThemePalette` |
| `CodeHtmlProvider`, `OssHtmlProvider`, `SecretsHtmlProvider` | their product-specific themed colour substitutions, and dark / high-contrast detection |
| `StaticHtmlProvider` | `GetInitHtmlAsync`, which marshals through the IDE joinable task factory |

Where the VS half is an implementation detail rather than API, it is expressed as a
[classic partial method](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/partial-method)
(`static partial void Xxx(...)`): if no implementing declaration is compiled, the call is removed
by the compiler. That is why the cross-platform build needs no stub files at all.

## Adding a test to the cross-platform run

1. Add the test file to `Snyk.VisualStudio.Extension.Core.Tests.props`.
2. Add any production file it needs to `Snyk.VisualStudio.Extension.Core.props`, plus that file's
   own dependencies.
3. `dotnet test Snyk.VisualStudio.Extension.Core.Tests`.

If the build fails on a Visual Studio type, you have three options, in order of preference:

1. **Split the type** using the `.Vs.cs` convention above. Best when the VS dependency is a small
   part of an otherwise neutral type. Remember to add the new `.Vs.cs` file to
   `Snyk.VisualStudio.Extension.2022.csproj`.
2. **Narrow the dependency** — for example, depend on an interface the tests already mock instead
   of a concrete VS-bound service.
3. **Leave it Windows-only.** Genuinely IDE-bound code (tool windows, WPF panels, the VS language
   client, the WebView2 control host) belongs in `Snyk.VisualStudio.Extension.Tests`.

## What stays Windows-only

| | Why |
| --- | --- |
| Building the VSIX (`Snyk.VisualStudio.Extension.2022`) | needs MSBuild plus the Visual Studio SDK |
| `Snyk.VisualStudio.Extension.Tests` (net48) | references the VSIX and `Microsoft.VisualStudio.Sdk.TestFramework` / MockedVS |
| `Tests/Integration.Tests` | launches a real Visual Studio instance |

Tests that stay in the Windows-only project are the ones using `[Collection(MockedVS.Collection)]`,
deriving from `PackageBaseTest`, or touching WPF, WebView2 or the VS language client directly.

Do not try to `msbuild` the solution on Linux — the VSSDK targets are Windows-only. Build the
cross-platform project directly, by path, as shown at the top of this document.

## Guard rails

`ProjectFileIntegrityTests` runs as part of the cross-platform suite and fails if:

- a source file exists under `Snyk.VisualStudio.Extension.2022/` but is missing from its legacy
  csproj (which would silently drop it from the VSIX),
- the csproj references a file that has been deleted,
- either `.props` list points at a file that no longer exists,
- a `.Vs.cs` or `.xaml.cs` file leaks into the cross-platform source list.

Because those checks run on Linux, the most common way to break the Windows-only build gets caught
before anyone starts a Windows machine.
