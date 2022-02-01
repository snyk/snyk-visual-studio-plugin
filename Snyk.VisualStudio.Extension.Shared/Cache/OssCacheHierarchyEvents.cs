namespace Snyk.VisualStudio.Extension.Shared.Cache
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.Shell.Interop;
    using Snyk.VisualStudio.Extension.Shared.Service;

    /// <summary>
    /// Impleemnts <see cref="IVsHierarchyEvents"/> for SnykCode cache.
    /// </summary>
    public class OssCacheHierarchyEvents : AbstractCacheHierarchyEvents
    {
        private static readonly string[] SupportedManifestFiles =
            {
                "Gemfile",
                "Gemfile.lock",
                "package-lock.json",
                "pom.xml",
                "build.gradle",
                "build.gradle.kts",
                "build.sbt",
                "yarn.lock",
                "package.json",
                "Pipfile",
                "setup.py",
                "requirements.txt",
                "Gopkg.lock",
                "go.mod",
                "vendor.json",
                "project.assets.json",
                "packages.config",
                "project.json",
                "paket.dependencies",
                "composer.lock",
                "Podfile.lock",
                "CocoaPods.podfile.yaml",
                "CocoaPods.podfile",
                "Podfile",
                "poetry.lock",
                "mix.exs",
            };

        private static readonly string[] SupportedManifestFileExtensions =
            {
                "gemspec",
                "*.jar",
                "*.war",
            };

        private IOssService ossService;

        /// <summary>
        /// Initializes a new instance of the <see cref="OssCacheHierarchyEvents"/> class.
        /// </summary>
        /// /// <param name="vsHierarchy">VS Hierarchy instance.</param>
        /// <param name="ossService">Oss service instance.</param>
        public OssCacheHierarchyEvents(IVsHierarchy vsHierarchy, IOssService ossService)
            : base(vsHierarchy)
        {
            this.ossService = ossService;
        }

        /// <inheritdoc/>
        public override void OnFileAdded(string filePath)
            => this.ClearOssCacheIfNeeded(filePath);

        /// <inheritdoc/>
        public override void OnFileDeleted(string filePath)
            => this.ClearOssCacheIfNeeded(filePath);

        /// <inheritdoc/>
        public override void OnFileChanged(string filePath)
            => this.ClearOssCacheIfNeeded(filePath);

        private void ClearOssCacheIfNeeded(string filePath)
        {
            if (this.IsManifestFile(SupportedManifestFiles, Path.GetFileName(filePath))
                || this.IsManifestFile(SupportedManifestFileExtensions, Path.GetExtension(filePath)))
            {
                this.ossService.ClearCache();
            }
        }

        private bool IsManifestFile(string [] array, string searchString) =>
            Array.Exists(array, fileName => fileName.ToLower() == searchString.ToLower());
    }
}
