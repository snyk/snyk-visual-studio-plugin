<!-- sub-plan: linked from docs/plans/PLAN.md -->
# IDE-1752 — Activate Snyk language server when Visual Studio opens with no solution

## Problem (customer outcome)
A VS 2022 developer who opens the IDE **with no solution or folder open** has the Snyk
extension hang forever on "Snyk Security is waiting for Visual Studio to initialize", and the
IDE sometimes becomes unstable. With a solution/folder open it works. Goal: the extension must
finish initializing on its own with no solution open and become usable, while the solution-open
path stays exactly as it is today.

## Root cause (verified)
`SnykVSPackage.LanguageClientManagerOnLanguageClientNotInitializedAsync` only triggers the VS
`ILanguageClient` activation (by force-opening the bundled temp file `Resources/SnykLsInit.cs`)
when a solution/folder is open. With no solution open it falls into an unbounded
`await Task.Delay(3000)` loop and never activates the client, so the language server never
starts. Present and unchanged from plugin 2.7.0 through 2.9.0 — upgrading does not fix it.
Equivalent "init with no project" case was fixed for Eclipse in IDE-845.

## Approach (TDD, outside-in)
1. **Acceptance** (`[IdeFact]`, `Tests/Integration.Tests`): boot VS with no solution open and
   assert the package reaches initialized/ready state within a bounded timeout (no hang).
2. **Seam**: extract a testable decision from the private UI-thread handler — a pure
   `internal` decision method plus an injectable "open temp init file" action (per the repo's
   established testable-seam pattern; `InternalsVisibleTo` is already set).
3. **Unit** (`Snyk.VisualStudio.Extension.Tests`, xUnit + Moq, `[Collection(MockedVS.Collection)]`):
   no-solution + LS-not-ready → activation invoked; solution-open → activation invoked
   (regression); LS-ready → no-op; activation failure → handled + diagnostic logged, no loop.
4. **Fix**: in the no-solution branch, perform the same temp-file activation used for the
   solution-open branch instead of the infinite delay loop; remove the dead-loop; keep the
   existing on-ready handler that closes the temp window. Ensure **Initialize Now** re-enters
   this path.
5. **Verify**: full unit + integration suites green; solution-open startup test unchanged (M2);
   no-solution reaches ready well within timeout (M3 responsiveness).

## Acceptance criteria
The four Gherkin scenarios in Jira IDE-1752 `customfield_10890` (no-solution init; solution-open
regression; Initialize Now recovery; IDE stays responsive), implemented as `[IdeFact]`
acceptance tests plus supporting unit tests.

## Out of scope
- F2 `unsupported Tool: Visual Studio 2022` MCP/CLI message (expected fixed in bundled CLI; track separately).
- F3 `git not on PATH` net-new-scan limitation (customer environment).
- Customer-clarification follow-ups from triage (crash-vs-hang detail, Initialize-Now confirmation).

## Files
- `Snyk.VisualStudio.Extension.2022/SnykVSPackage.cs` (fix + seam)
- `Snyk.VisualStudio.Extension.Tests/SnykVSPackageNotInitializedHandlerTests.cs` (new, unit)
- `Tests/Integration.Tests/ExtensionStartupNoSolutionTests.cs` (new, acceptance)
