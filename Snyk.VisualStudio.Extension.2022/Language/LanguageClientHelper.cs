namespace Snyk.VisualStudio.Extension.Language
{
    public static class LanguageClientHelper
    {
        public static ILanguageClientManager LanguageClientManager()
        {
            return SnykVSPackage.Instance?.LanguageClientManager;
        }

        public static bool IsLanguageServerReady()
        {
            return LanguageClientManager() != null && LanguageClientManager().IsReady;
        }
    }
}