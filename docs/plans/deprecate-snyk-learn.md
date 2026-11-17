# Deprecate Snyk Learn in Visual Studio

## Goal

Stop surfacing Snyk Learn in the Visual Studio extension without changing `snyk-ls`.
Keep every unrelated issue detail and external link rendered as before.

## Design

1. Send `enable_snyk_learn_code_actions=false` as an always-changed language-server
   setting so Learn code actions are not created for this IDE.
2. Filter HTML at `BaseHtmlProvider`, the shared boundary used before LS-provided HTML
   enters WebView2. Remove the known `.sn-learn` issue-detail component and any remaining
   anchor whose URL targets the exact `learn.snyk.io` host.
3. Cover the current Code and Open Source component shapes, direct Learn links, unrelated
   content, and non-Learn external links with focused unit tests.

## Verification

- Run the focused HTML-provider and language-server-settings unit tests on Windows.
- Build the VSIX solution with Visual Studio MSBuild.
- Manually inspect Code and Open Source issue details in Visual Studio when a Windows
  extension-development environment is available.
