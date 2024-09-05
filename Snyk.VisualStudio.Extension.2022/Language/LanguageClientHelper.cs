namespace Snyk.VisualStudio.Extension.Language
{
    public static class LanguageClientHelper
    {
        public static bool IsLanguageServerReady()
        {
            return SnykVSPackage.Instance?.LanguageClientManager != null && SnykVSPackage.Instance.LanguageClientManager.IsReady;
        }
    }
}