# IDE-2558 — Merge Serilog into the extension assembly

## Problem

Visual Studio shows "The 'SnykVSPackage' package did not load correctly" when the Snyk tool window
was left open and VS is restarted, if "GitLab for Visual Studio" is also installed.

GitLab's extension registers a global binding redirect for `Serilog`. VS applies it to every
extension. Snyk asks for `Serilog 2.0.0.0`, VS rewrites that to a version that is not in the Snyk
folder, and the load fails with `FileNotFoundException`. The failure happens in the
`SnykVSPackage` type initializer, and again in VS's MEF scan of `Snyk.VisualStudio.Extension.dll`.
Because Serilog is the logger, Snyk cannot write a log line about it.

## Change

Build step: after compile, merge `Serilog`, `Serilog.Sinks.File`, `Serilog.Enrichers.Process`,
`Serilog.Enrichers.Thread` and `Serilog.Exceptions` into `Snyk.VisualStudio.Extension.dll` with
ILRepack. Nothing asks VS for a separate `Serilog` assembly any more, so no redirect can touch it.

- `ILRepack.targets` (next to the csproj) holds the merge targets. Its presence also turns off the
  package's own default merge target.
- The five Serilog packages are `PrivateAssets="all"`, so the test project sees Serilog types only
  from the merged assembly (no duplicate type).
- The merged DLLs are removed from `ReferenceCopyLocalPaths`, so they are not copied to `bin` or
  into the VSIX.
- `Serilog.Extensions.Logging` is removed. Nothing uses it.
- Serilog types stay public in the merged assembly (`LogManager.ForContext` returns
  `Serilog.ILogger`), so there is no `/internalize`.

## Tests

`SerilogMergeTests`: the extension assembly has no reference to any `Serilog*` assembly, and
`Serilog.ILogger` is defined in the extension assembly.

## Manual check (needs Windows, VS and GitLab for Visual Studio)

1. Build the VSIX. Open it and confirm there is no `Serilog*.dll` file.
2. Install it next to GitLab for Visual Studio.
3. Open and dock the Snyk tool window, close VS, open VS again.
4. Expect no error dialog, and no `CreateInstance failed` or Serilog `FileNotFoundException` in
   `ActivityLog.xml`. `snyk-extension.log` should have new entries.

