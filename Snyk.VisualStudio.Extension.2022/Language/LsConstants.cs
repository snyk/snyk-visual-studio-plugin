namespace Snyk.VisualStudio.Extension.Language
{
    public static class LsConstants
    {
        public const int ProtocolVersion = 14;
        
        // Notifications
        public const string SnykHasAuthenticated = "$/snyk.hasAuthenticated";
        public const string SnykCliPath = "$/snyk.isAvailableCli";
        public const string SnykAddTrustedFolders = "$/snyk.addTrustedFolders";
        public const string SnykScan = "$/snyk.scan";
        public const string SnykFolderConfig = "$/snyk.folderConfigs";
        // This notification is needed because we are sending Issue data in the Diagnostic.Data field and Visual Studio filters it out.
        // We had to send the same notification but with a different to avoid Visual Studio's filtering behavior.
        public const string OnPublishDiagnostics316 = "$/snyk.publishDiagnostics316";
        
        // Requests
        public const string SnykWorkspaceScan = "snyk.workspace.scan";
        public const string SnykSastEnabled = "snyk.getSettingsSastEnabled";
    }
}
