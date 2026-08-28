## Project Overview

Snyk Security (`snyk/snyk-visual-studio-plugin`) is a C#/.NET Framework 4.8 Visual Studio extension (VSIX) that integrates Snyk Open Source, Snyk Code, Snyk Secrets and Snyk IaC scanning into Visual Studio 2022 and 2026. It downloads/launches the Snyk CLI (which bundles the Snyk Language Server) and communicates with it as a standard LSP `ILanguageClient` (`Snyk.VisualStudio.Extension.2022/Language/SnykLanguageClient.cs`), using `Microsoft.VisualStudio.LanguageServer.Client` and `StreamJsonRpc` over stdio.

## Build & Development Commands

This is a .NET Framework 4.8 VSIX project, **not** buildable with the `dotnet` CLI. It requires MSBuild plus the Visual Studio SDK ("Visual Studio extension development" workload). CI runs on Windows:

```bash
nuget restore snyk-visual-studio-plugin.sln
msbuild snyk-visual-studio-plugin.sln /p:configuration=Release /p:DeployExtension=false /p:ZipPackageCompressionLevel=normal /v:m

# Unit tests (xUnit via VSTest, excluding integration tests)
vstest.console.exe **\bin\**\*.Tests.dll /TestCaseFilter:"FullyQualifiedName!=Xunit.Instances.VisualStudio&integration!=true"

# Integration tests (separate project, needs TEST_API_TOKEN)
vstest.console.exe **\bin\**\*Integration.Tests.dll
```

The documented local workflow is opening `snyk-visual-studio-plugin.sln` in Visual Studio 2022 and using Build/Test Explorer directly. Tests use xUnit with Moq for mocking.

## Architecture

Main project: `Snyk.VisualStudio.Extension.2022/` (root namespace `Snyk.VisualStudio.Extension`), organized by feature folder:
- `Language/` is the LSP client: `SnykLanguageClient.cs` (`ILanguageClient`/`ILanguageClientCustomMessage2`/`ILanguageClientManager`), `LsConstants.cs`, `CustomInitializationOptions.cs`; `Language/V25/` holds the newer LS config protocol (`InitializationOptionsV25.cs`, `GlobalSettingsApplier.cs`, `UserOverrideTracker.cs`).
- `CLI/` has `SnykCli.cs`, `ICli.cs` (wraps invoking the Snyk CLI binary).
- `Download/` has `SnykCliDownloader.cs`, `Sha256.cs`, `LatestReleaseInfo.cs` (downloads/verifies the CLI+LS binary).
- `Service/` has the core services: `SnykService.cs`/`ISnykService.cs`, `SnykTasksService.cs`, `SnykSolutionService.cs`, `SnykFeatureFlagService.cs`, `WorkspaceTrustService.cs`, `ApiEndpointResolver.cs`.
- `Commands/` holds VS menu commands, built on `AbstractSnykCommand`/`AbstractTaskCommand` base classes (e.g. `SnykScanCommand.cs`).
- `Authentication/`, `Analytics/`, `Settings/`, `Theme/`, `Model/`, `Extension/` (extension methods), `Utils/`.
- `UI/` has `Toolwindow/`, `Controls/`, `Notifications/`, `Html/` (WPF/XAML tool window UI).

Test projects:
- `Snyk.VisualStudio.Extension.Tests/` has unit tests mirroring the main project's folder structure.
- `Tests/Integration.Tests/` has integration tests run against a real built VSIX/CLI in CI (`ExtensionStartupTests.cs`, `CliProtocolSupportedRealCliTests.cs`).

## Conventions

- Standard C# PascalCase for classes/methods/properties, camelCase for locals/private fields.
- Interfaces prefixed with `I` (e.g. `ICli.cs`, `ISnykService.cs`); shared behavior via `Abstract*` base classes.
- Test framework is xUnit (`[Fact]`) with Moq for mocking; test classes suffixed `Tests`/`Test`, system-under-test field commonly named `sut`. Test method naming: `MethodUnderTest_Scenario_ExpectedResult`.
- Logging via Serilog (`ILogger`, `LogManager.ForContext<T>()`).
- `docs/plans/` and `docs/diagrams/*.mmd` hold design plans/flow diagrams for specific Jira tickets. Write a short design doc there before larger changes.

## Development Workflow

- Before starting non-trivial work, confirm whether the change belongs in this extension or in the shared `snyk-ls`/go-application-framework stack. See `CONTRIBUTING.md`.
- For non-trivial work, write a short design doc under `docs/plans/` before starting and get confirmation.
- This is not a library: delete unused files instead of deprecating them.
- Use Moq for mocking and reuse existing mocks rather than writing new ones.
- Build the solution and run the unit test suite (`vstest.console.exe`, excluding integration tests) before committing. CONTRIBUTING.md requires manually testing every change yourself in Visual Studio, not just relying on automated tests.
- Run Snyk SCA/Code scans against the project's absolute path before committing and after `.csproj` changes; fix real findings, don't touch test fixtures.
- Before each commit, check for and address feedback from the PR review bot (snyk-pr-review-bot) on any open PR.
- Never use `--no-verify` or otherwise skip commit hooks, and never amend commits. Use atomic, conventional-commit-style commits; if a Jira ID (`IDE-XXXX`) appears in the branch name, include it in square brackets in the subject.
- Never push without asking first, and never force-push. Regularly fetch `main` and offer to merge it into the working branch.
- After pushing, offer to open a draft PR using `.github/pull_request_template.md` (or update the existing PR description). Per `CONTRIBUTING.md`, a change applicable to the other Snyk IDE plugins (vscode-extension, snyk-intellij-plugin, snyk-eclipse-plugin) should get matching PRs opened there too, since releases are usually coordinated.
- User-facing changes need documentation updates. Prepare them yourself, or add the wording/screenshots to the PR description if you don't have doc-site access.
