namespace Snyk.VisualStudio.Extension.Cache
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Community.VisualStudio.Toolkit;
    using Microsoft.VisualStudio.Shell.Interop;
    using Snyk.VisualStudio.Extension.Service;

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
                ".gemspec",
                ".jar",
                ".war",
                ".csproj",
                ".sln",
            };

        private IOssService ossService;

        /// <summary>
        /// Initializes a new instance of the <see cref="OssCacheHierarchyEvents"/> class.
        /// </summary>
        /// <param name="ossService">Oss service instance.</param>
        public OssCacheHierarchyEvents(IOssService ossService)
            : base()
        {
            this.ossService = ossService;
        }

        /// <inheritdoc/>
        protected override void OnFileSaved(string file) => this.ClearOssCacheIfNeeded(file);

        /// <inheritdoc/>
        protected override void OnFileRename(AfterRenameProjectItemEventArgs eventArgs)
        {
            foreach (var item in eventArgs.ProjectItemRenames)
            {
                this.ClearOssCacheIfNeeded(item.SolutionItem.FullPath);
            }
        }

        /// <inheritdoc/>
        protected override void OnFileRemove(AfterRemoveProjectItemEventArgs eventArgs)
        {
            foreach (var item in eventArgs.ProjectItemRemoves)
            {
                this.ClearOssCacheIfNeeded(item.RemovedItemName);
            }
        }

        /// <inheritdoc/>
        protected override void OnFileAdd(IEnumerable<SolutionItem> solutionItems)
        {
            foreach (var solutionItem in solutionItems)
            {
                this.ClearOssCacheIfNeeded(solutionItem.FullPath);
            }
        }

        private void ClearOssCacheIfNeeded(string filePath)
        {
            if (filePath == null || (this.IsManifestFile(SupportedManifestFiles, Path.GetFileName(filePath))
                || this.IsManifestFile(SupportedManifestFileExtensions, Path.GetExtension(filePath))))
            {
                this.ossService.ClearCache();
            }
        }

        private bool IsManifestFile(string [] array, string searchString) =>
            Array.Exists(array, fileName => !string.IsNullOrEmpty(searchString) && fileName.ToLower() == searchString.ToLower());
    }
}
