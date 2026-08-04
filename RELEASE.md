## Release version steps


**Protocol Version Verification**

- Ensure the Snyk Language Server Protocol version is correct in the plugin.
  - `ProtocolVersion` in `LsConstants`


**Preview Version Verification**

- Trigger or wait for the preview release workflow to build a preview version on the commit that will be used for the release.
  - The preview release workflow runs automatically on pushes to main.
- Install the preview version from the marketplace and verify that the changes for this release are present and working correctly.
- Before triggering the release workflow, review merged PR titles on main — the release workflow auto-generates release notes from them. Confirm any Early Access features are labeled correctly; fix PR titles on main before releasing if needed.


**Initiate Release**

- If you want to do a hotfix with a subset of commits from main, create a hotfix branch off the previous release tag.
  - For the hotfix release, cherry pick the commits you want to go into the hotfix release.

- Trigger the release workflow in GitHub Actions.
  - Select the appropriate version type (major, minor, patch).
  - If this is a hotfix not off main, select the hotfix branch.
- After the workflow completes, review the GitHub Release on the [releases page](https://github.com/snyk/snyk-visual-studio-plugin/releases) and edit the release notes if Early Access labeling or other wording needs correction.


**Marketplace Availability**

- Check that the new release appears on all relevant Marketplaces.


**Installation and Version Verification**

- Install the plugin or extension in the target IDE.

- Confirm that the installed version matches the intended release.


**CLI Configuration and Verification**

- Ensure the Snyk CLI release channel is set to  `stable`  and automatic update is enabled. 

- Execute the CLI binary in the terminal and verify that the version matches the intended release.
  - The correct version can be found in the  `#hammerhead-releases`  channel in Slack or in the github cli repo.
     https://github.com/snyk/cli/releases


**Manual End-to-End Test**

-   Manually run a scan using the latest version of the plugin to confirm end-to-end functionality.
