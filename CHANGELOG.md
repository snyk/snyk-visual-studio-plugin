# Snyk Security Changelog

## [2.0.0]
### Changed
- Visual Studio extension is now fully integrated with Snyk Language Server protocol v16.
- Reported issues now are highlighted in directly in your editor.
- Reported issues are now shown in the Visual Studio Error Window.
- Code Actions in the editor to learn about the reported issue.
- Apply AI fixes from the text editor.
- Added IaC Scanning.
- Added Auto Scanning Option.
- Plugin will scan automatically on save or when it starts.
- New Authentication modal dialog with a copy link button in case the browser doesn't automatically open.
- New design for all issue description panels.
- Better support for light and dark themes.
- Added preview release for the extension.
- Added CLI release channels in the settings.
- Added a new LS initialize state.
- Added Html rendering for issues.
- Download CLI depending on Language Server Protocol version.
- Deleted Code Client Library.

### Fixed
- Fixed UI Freeze when changing authentication method.
- Fixed UI Freeze when issues are being rendered.
- Fixed UI Freeze when calling getSastEnabled.
- Fixed UI Freeze after settings window is closed.  
- Fixed Jumpy navigation when issues are rendered in the tree.
- Fixed Disabled Authentication button and token textbox in case of failed authentication.
- High DPI scaling rendering.
- Fixed State transition when Automatic download for CLI is selected.
- Removed TLS enforcement from the extension.

## [1.1.63]

### Fixed
- Change default auth type to OAuth.

## [1.1.62]

### Fixed
- Added OAuth2 authentication Option to settings window.

## [1.1.61]

### Fixed
- Added a deprecation notice for VS 2015,2017 and 2019.

## [1.1.60]

### Fixed
- Fix an issue with extension failing to load in VS 2022 < 17.10.

## [1.1.59]

### Fixed
- Change Default API Endpoint to https://api.snyk.io.

## [1.1.58]

### Fixed
- Rendering issue in Settings page.

## [1.1.57]

### Fixed
- Change analytics scan status from "Succeeded" to "Success".

## [1.1.56]

### Fixed
- Send current solution folder path when sending Analytics.

## [1.1.55]

### Fixed
- Fixed an issue with resource loading in 17.10.

## [1.1.54]

### Fixed
- Fixed an issue affecting old IDEs.

## [1.1.53]

### Fixed
- Use dynamic colors for the suggestion fix textbox.

## [1.1.52]

### Fixed
- Upgraded several dependencies.
- Removed legacy analytics implementation.

## [1.1.51]

### Fixed
- Fixed a UI issue when toggling Code and Quality Scans in Settings page.

## [1.1.50]

### Fixed
- Upgraded the version of the Analytics dependency.

## [1.1.49]

### Fixed
- Upgraded the version of Newtonsoft dependency.

## [1.1.48]

### Fixed
- shortened extension name to just Snyk Security

## [1.1.47]
- Only send Amplitude analytics events when connected to an MT US environment

## [1.1.45]

### Changed
- Streamlined `.dcignore` to avoid accidental exclusion of project files.

## [1.1.44]
- Support for IDE analytics

## [1.1.43]

### Added
- Support for Snyk Code Local Engine

## [1.1.38]

### Fixed
- Custom endpoints validation in the settings page did not work correctly and led to crashes.

## [1.1.37]

### Fixed
- Rendering of code blocks in OSS description panel markdown.

## [1.1.34]

### Fixed
- Improved error handling for Code & Quality scans.

## [1.1.33]

### Added
- OAuth2 support when using OAuth2 endpoints.

## [1.1.32]

### Added
- Arm64 support.

### Changed
- Snyk Code no longer filters configuration files.

### Fixed
- Missing paths in the trust dialog in older VS versions.

## [1.1.31]

### Added
- Adds workspace trust mechanism to ensure scans are run on the trusted projects.

## [1.1.30]

### Changed
- Link to GH issues in the error notification bar is replaced with a link to Snyk support.

## [1.1.29]

### Fixed
- Extension failed to load with VS2022 versions smaller than 17.3.
- Stop button raising an error message.

## [1.1.28]

### Fixed
- Snyk Code scans sometimes skips files unnecessarily, leading to missing results or "No supported code available" errors.

## [1.1.27]

### Fixed
- Bug in VS2022 when loading the extension

## [1.1.26]

### Fixed
- Bug in Snyk Code messages encoding resulting in failed Snyk Code scans.

### Changed
- Update wording in settings menu for collecting analytics.

## [1.1.25]

### Added
- Option to disable CLI auto-update.
- Option to select CLI custom path.
- Improved UI/UX when the CLI is missing.

### Fixed
- Several issues with auto-updating the CLI executable.

## [1.1.24]

### Fixed
- Extension fails to load on VS2017.

## [1.1.23]

### Added
- Organization description information in settings.

### Fixed
- Changing custom endpoint settings leads to authentication errors.

## [1.1.22]

### Fixed
- External example fixes tab control dark theme support.
- Snyk Code results partially lost for WPF projecs.

## [1.1.21]

### Changed
- Replace the word "Remediation" with "Fix" in OSS report.

## [1.1.20]

### Fixed
- Files not detected issue.

## [1.1.19]

### Fixed
- Errors when projects are nested inside solution folders.

## [1.1.18]

### Changed
- Removed manually included DLLs from VSIX package.

## [1.1.17]

### Fixed
- Selection of tree view items only working when clicking on the icon.
- Background color of unfocused selected items might blend with font color on some themes.

## [1.1.16]

### Added
- Base64 encoding for Snyk Code analysis file content payloads.

### Fixed
- The color of the text in the tree view does not match the color from VS theme.
- A problem with partially lost Snyk Code results if a single file contains multiple identical suggestions.
- Error when clicking on issues with unknown severity in the tool window.

## [1.1.15]

### Fixed
- Fixed a bug in Snyk Code where files with an underscore in the path would be ignored.
- Restore all tree items after clear search or filter.

### Changed
- Expand all scan results after completing a scan.

## [1.1.14]

### Fixed
- Run a scan for OSS and for Snyk Code asynchronously.

## [1.1.13]

### Fixed
- Error reporting and Snyk Code configuraton issues.

## [1.1.12]

### Fixed
- Display message on main panel if error occurs.
- Link to Snyk Code settings on app.snyk.io.

### Changed
- Added analysis context information for analysis requests.
- Welcome screen text and added privacy policy, term of service links.

## [1.1.11]

### Fixed
- Fixed CLI download blocking the UI longer than necessary.

### Changed
- Improved message text when Snyk Code is disabled.
- Added Snyk to the "Extensions" menu in VS2019+, and to the top menu bar in older versions.

## [1.1.10]

### Changed
- Snyk Code: add support for Single Tenant setups.

### Fixed
- Scan of solutions in which *.sln file is not in the root directory.

## [1.1.9]

### Fixed
- "Object reference not set to an instance of an object" when launching extension in Visual Studio 2022.

## [1.1.8]

### Fixed
- Clean Open Source and Snyk Code vulnerabilities cache.
- Fixed severity icons for Snyk Code issues.

## [1.1.7]

### Fixed
- Extension crash on Visual Studio 2017.
- Cache invalidation for Open Source vulnerabilities.
