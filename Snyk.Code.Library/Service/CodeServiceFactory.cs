﻿namespace Snyk.Code.Library.Service
{
    using Snyk.Code.Library.Api;
    using Snyk.Common;

    /// <summary>
    /// Factory to create SnykCode services. This class hide intance createtion.
    /// </summary>
    public class CodeServiceFactory
    {
        /// <summary>
        /// Factory method for <see cref="ISnykCodeService"/> instance.
        /// </summary>
        /// <param name="apiToken">Snyk ApiToken.</param>
        /// <param name="endpoint">Snyk endpoint.</param>
        /// <param name="fileProvider">VisualStudio file provider.</param>
        /// <returns>ISnykCodeService instance.</returns>
        public static ISnykCodeService CreateSnykCodeService(string apiToken, string endpoint, IFileProvider fileProvider)
        {
            var codeClient = new SnykCodeClient(endpoint, apiToken);

            var filterService = new FiltersService(codeClient);

            var bundleService = new BundleService(codeClient);
            var analysisService = new AnalysisService(codeClient);

            string rootDirectoryPath = fileProvider.GetSolutionPath();

            var codeCacheService = new CodeCacheService(rootDirectoryPath);
            var dcIgnoreService = new DcIgnoreService();

            return new SnykCodeService(bundleService, analysisService, filterService, codeCacheService, dcIgnoreService);
        }
    }
}
