# Master Implementation Plan — snyk-visual-studio-plugin

Single source of truth for in-flight and completed implementation work. Each entry links to a
detailed sub-plan. Update on creation (Pending) and on merge (Completed + commit SHA).

## Pending
- **Snyk Learn deprecation** — Remove Snyk Learn code actions and issue-detail links from
  the Visual Studio extension. Sub-plan:
  [deprecate-snyk-learn.md](./deprecate-snyk-learn.md).

- **IDE-1752** — Activate Snyk language server when Visual Studio opens with no solution/folder
  (fixes indefinite "waiting for Visual Studio to initialize" hang). Sub-plan:
  [IDE-1752-vs-init-no-solution.md](./IDE-1752-vs-init-no-solution.md). Branch:
  `fix/IDE-1752-vs-init-no-solution`.

- **IDE-2558** — Snyk fails to load next to GitLab for Visual Studio (Serilog binding redirect).
  Merge Serilog into the extension assembly. Sub-plan:
  [IDE-2558-merge-serilog.md](./IDE-2558-merge-serilog.md).

## Completed
_(none yet)_
