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

## Repeated attributes are dropped by ILRepack

ILRepack drops repeated attributes whose `AllowMultiple` comes from a base class. `[ContentType]`
is one (`MultipleBaseMetadataAttribute` declares it). After the merge `SnykLanguageClient` kept one
of its 56 content types, so VS never loaded the language client for `.cs` files and the tool window
stayed on "waiting for Visual Studio to initialize". Checked with ILRepack 2.0.18, 2.0.34 and 2.0.48.

`SnykContentTypeAttribute` is a metadata attribute that declares `AllowMultiple` itself, with the
same `ContentTypes` property, and replaces `[ContentType]` on `SnykLanguageClient`. ILRepack keeps
every repeat of it.

## Tests

`SerilogMergeTests`: the extension assembly has no reference to any `Serilog*` assembly,
`Serilog.ILogger` is defined in the extension assembly, and `SnykLanguageClient` keeps all its
content types.

## Manual check (needs Windows, VS and GitLab for Visual Studio)

1. Build the VSIX. Open it and confirm there is no `Serilog*.dll` file.
2. Install it next to GitLab for Visual Studio.
3. Open and dock the Snyk tool window, close VS, open VS again.
4. Expect no error dialog, and no `CreateInstance failed` or Serilog `FileNotFoundException` in
   `ActivityLog.xml`. `snyk-extension.log` should have new entries.
