using System.Linq;
using Snyk.VisualStudio.Extension.Settings;

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

        /// <summary>
        /// Whether the user has asked for debug logging, via <c>-d</c>/<c>--debug</c> in the global
        /// additional parameters or in ANY folder's additional parameters.
        ///
        /// One signal drives two things: the language server's own <c>-l debug</c>, and the extension's
        /// Serilog level (see <see cref="LogManager.SetDebugLogging"/>). They were separate before, which
        /// meant the extension side of a startup problem could not be turned on at all.
        /// </summary>
        public static bool IsDebugModeRequested(ISnykOptions options)
        {
            if (options == null)
            {
                return false;
            }

            var globalParams = options.AdditionalParameters ?? Enumerable.Empty<string>();
            var folderParams = (options.FolderConfigs ?? Enumerable.Empty<FolderConfig>())
                .Select(fc => fc?.GetStringList(PflagKeys.AdditionalParameters))
                .Where(p => p != null)
                .SelectMany(p => p);

            return globalParams.Concat(folderParams).Any(p => p == "-d" || p == "--debug");
        }
    }
}
