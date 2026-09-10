# Master Implementation Plan — snyk-visual-studio-plugin

Single source of truth for in-flight and completed implementation work. Each entry links to a
detailed sub-plan. Update on creation (Pending) and on merge (Completed + commit SHA).

## Pending
- **IDE-1752** — Activate Snyk language server when Visual Studio opens with no solution/folder
  (fixes indefinite "waiting for Visual Studio to initialize" hang). Sub-plan:
  [IDE-1752-vs-init-no-solution.md](./IDE-1752-vs-init-no-solution.md). Branch:
  `fix/IDE-1752-vs-init-no-solution`.
- **IDE-2558** — SnykVSPackage fails to load ("not loaded correctly") due to a Serilog
  `FileNotFoundException` from its `.cctor` when other extensions (e.g. GitLab for Visual
  Studio) interfere with assembly resolution during persisted tool-window reconstruction.
  Sub-plan: [IDE-2558-serilog-assembly-resolution.md](./IDE-2558-serilog-assembly-resolution.md).

## Completed
_(none yet)_
