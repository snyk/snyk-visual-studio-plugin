namespace Snyk.VisualStudio.Extension.Language
{
    public static class LanguageClientHelper
    {
        public static ILanguageClientManager LanguageClientManager()
        {
            return SnykVSPackage.ServiceProvider.LanguageClientManager;
        }

        public static bool IsLanguageServerReady()
        {
            return LanguageClientManager() != null && LanguageClientManager().IsReady;
        }
    }
}