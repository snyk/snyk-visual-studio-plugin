namespace Snyk.VisualStudio.Extension.Language
{
    public static class LsConstants
    {
        public const string ProtocolVersion = "23";
        
        // Notifications
        public const string SnykHasAuthenticated = "$/snyk.hasAuthenticated";
        public const string SnykCliPath = "$/snyk.isAvailableCli";
        public const string SnykAddTrustedFolders = "$/snyk.addTrustedFolders";
        public const string SnykScan = "$/snyk.scan";
        public const string SnykFolderConfig = "$/snyk.folderConfigs";
        // This notification is needed because we are sending Issue data in the Diagnostic.Data field and Visual Studio filters it out.
        // We had to send the same notification but with a different to avoid Visual Studio's filtering behavior.
        public const string OnPublishDiagnostics316 = "$/snyk.publishDiagnostics316";
        public const string SnykScanSummary = "$/snyk.scanSummary";
        
        // Commands
        public const string WorkspaceChangeConfiguration = "workspace/didChangeConfiguration";
        public const string WorkspaceExecuteCommand = "workspace/executeCommand";

        public const string SnykWorkspaceScan = "snyk.workspace.scan";
        public const string SnykWorkspaceFolderScan = "snyk.workspaceFolder.scan";
        public const string SnykWorkspaceConfiguration = "snyk.workspace.configuration";
        public const string SnykSastEnabled = "snyk.getSettingsSastEnabled";
        public const string SnykLogin = "snyk.login";
        public const string SnykLogout = "snyk.logout";
        
        public const string SnykCopyAuthLink = "snyk.copyAuthLink";
        public const string SnykGetFeatureFlagStatus = "snyk.getFeatureFlagStatus";
        public const string SnykGenerateIssueDescription = "snyk.generateIssueDescription";
        public const string SnykReportAnalytics = "snyk.reportAnalytics";
        public const string SnykCodeFixDiffs = "snyk.code.fixDiffs";
        public const string SnykCodeFixApplyEdit = "snyk.code.fixApplyEdit";
        public const string SnykSubmitIgnoreRequest = "snyk.submitIgnoreRequest";


        public const string ShowDocument = "window/showDocument";
        // Feature flags
        public const string SnykConsistentIgnoresEnabled = "snykCodeConsistentIgnores";
    }
}
