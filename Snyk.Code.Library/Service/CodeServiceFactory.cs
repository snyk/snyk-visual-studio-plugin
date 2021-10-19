namespace Snyk.Code.Library.Service
{
    using Snyk.Code.Library.Api;

    /// <summary>
    /// Factory to create SnykCode services. This class hide intance createtion.
    /// </summary>
    public class CodeServiceFactory
    {





        /// <summary>
        /// TODO:
        /// </summary>
        public static ISnykCodeService CreateSnykCodeService(string apiToken, string endpoint, IFileProvider fileProvider)
        {
            var codeClient = new SnykCodeClient(endpoint, apiToken);

            var filterService = new FiltersService(codeClient);

            var bundleService = new BundleService(codeClient);
            var analysisService = new AnalysisService(codeClient);

            string rootDirectoryPath = fileProvider.GetSolutionPath();

            var codeCacheService = new CodeCacheService(rootDirectoryPath);
            var dcIgnoreService = new DcIgnoreService(rootDirectoryPath);

            return new SnykCodeService(bundleService, analysisService, filterService, codeCacheService, dcIgnoreService);
        }
    }
}
